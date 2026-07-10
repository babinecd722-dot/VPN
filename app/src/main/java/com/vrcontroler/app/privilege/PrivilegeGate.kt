package com.vrcontroler.app.privilege

import android.content.Context
import android.content.SharedPreferences
import android.os.Build
import android.util.Log
import com.vrcontroler.app.service.FileDaemon
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.sync.Mutex
import kotlinx.coroutines.sync.withLock
import kotlinx.coroutines.withContext
import moe.shizuku.manager.adb.AdbClient
import moe.shizuku.manager.adb.AdbKey
import moe.shizuku.manager.adb.AdbMdns
import moe.shizuku.manager.adb.AdbPairingClient
import moe.shizuku.manager.adb.AdbShellStream
import moe.shizuku.manager.adb.PreferenceAdbKeyStore
import org.json.JSONObject
import java.util.concurrent.atomic.AtomicReference
import kotlin.coroutines.resume
import kotlin.coroutines.suspendCoroutine

enum class PrivilegeState {
    /** Wireless debugging off / port not found */
    NEED_WIRELESS,
    /** Key not paired yet — show pairing UI */
    NEED_PAIRING,
    /** Connecting / starting daemon */
    CONNECTING,
    /** Shell daemon ready — Android/data accessible */
    READY,
    /** Last attempt failed */
    ERROR,
}

/**
 * Portable privilege layer: wireless ADB pairing + embedded FileDaemon.
 * No external Shizuku APK required.
 */
object PrivilegeGate {
    private const val TAG = "PrivilegeGate"
    private const val PREFS = "vr_adb"
    private const val KEY_PAIRED = "paired"

    private val _state = MutableStateFlow(PrivilegeState.NEED_WIRELESS)
    val state: StateFlow<PrivilegeState> = _state

    private val _message = MutableStateFlow<String?>(null)
    val message: StateFlow<String?> = _message

    private var appContext: Context? = null
    private var prefs: SharedPreferences? = null
    private var adbKey: AdbKey? = null

    private val clientRef = AtomicReference<AdbClient?>(null)
    private val shellRef = AtomicReference<AdbShellStream?>(null)
    private val rpcMutex = Mutex()

    val isReady: Boolean get() = _state.value == PrivilegeState.READY

    fun init(context: Context) {
        appContext = context.applicationContext
        prefs = context.applicationContext.getSharedPreferences(PREFS, Context.MODE_PRIVATE)
        adbKey = AdbKey(PreferenceAdbKeyStore(prefs!!), "vrcontroler@quest")
        // Load native pairing lib early
        try {
            System.loadLibrary("adb")
        } catch (e: Throwable) {
            Log.e(TAG, "libadb load failed", e)
        }
    }

    fun hasPairedBefore(): Boolean = prefs?.getBoolean(KEY_PAIRED, false) == true

    suspend fun refresh() = withContext(Dispatchers.IO) {
        if (shellRef.get() != null && clientRef.get() != null) {
            try {
                val pong = rpc(JSONObject().put("op", "ping"))
                if (pong.optBoolean("ok")) {
                    _state.value = PrivilegeState.READY
                    _message.value = null
                    return@withContext
                }
            } catch (_: Exception) {
                teardown()
            }
        }
        connectInternal(autoPairHint = false)
    }

    /** Try connect; if not paired, move to NEED_PAIRING. */
    suspend fun connect() = withContext(Dispatchers.IO) {
        connectInternal(autoPairHint = true)
    }

    /**
     * Pair with the 6-digit wireless debugging code.
     * User must open: Settings → Developer Options → Wireless debugging → Pair with pairing code.
     */
    suspend fun pair(code: String): Boolean = withContext(Dispatchers.IO) {
        if (Build.VERSION.SDK_INT < 30) {
            _state.value = PrivilegeState.ERROR
            _message.value = "Нужен Android 11+ / Horizon OS с Wireless Debugging"
            return@withContext false
        }
        val ctx = appContext ?: return@withContext false
        val key = adbKey ?: return@withContext false
        val trimmed = code.trim()
        if (trimmed.length < 6) {
            _message.value = "Введи 6-значный код pairing"
            return@withContext false
        }

        _state.value = PrivilegeState.CONNECTING
        _message.value = "Ищем сервис pairing…"

        val port = discoverPort(ctx, AdbMdns.TLS_PAIRING, timeoutMs = 45_000)
        if (port == null || port <= 0) {
            _state.value = PrivilegeState.NEED_PAIRING
            _message.value =
                "Не найден pairing-порт. Открой «Pair device with pairing code» в Wireless Debugging и попробуй снова."
            return@withContext false
        }

        _message.value = "Pairing на порту $port…"
        return@withContext try {
            AdbPairingClient("127.0.0.1", port, trimmed, key).use { client ->
                val ok = client.start()
                if (ok) {
                    prefs?.edit()?.putBoolean(KEY_PAIRED, true)?.apply()
                    _message.value = "Pairing OK — подключаемся…"
                    connectInternal(autoPairHint = false)
                    true
                } else {
                    _state.value = PrivilegeState.NEED_PAIRING
                    _message.value = "Pairing не удался. Проверь код."
                    false
                }
            }
        } catch (e: Exception) {
            Log.e(TAG, "pair failed", e)
            _state.value = PrivilegeState.NEED_PAIRING
            _message.value = "Ошибка pairing: ${e.message}"
            false
        }
    }

    private suspend fun connectInternal(autoPairHint: Boolean) {
        if (Build.VERSION.SDK_INT < 30) {
            _state.value = PrivilegeState.ERROR
            _message.value = "Wireless Debugging недоступен на этой ОС"
            return
        }
        val ctx = appContext ?: return
        val key = adbKey ?: return

        _state.value = PrivilegeState.CONNECTING
        _message.value = "Ищем Wireless Debugging…"

        val port = discoverPort(ctx, AdbMdns.TLS_CONNECT, timeoutMs = 12_000)
        if (port == null || port <= 0) {
            _state.value = PrivilegeState.NEED_WIRELESS
            _message.value =
                "Включи Wireless Debugging в параметрах разработчика, затем нажми «Подключить»."
            return
        }

        _message.value = "ADB $port — запуск daemon…"
        teardown()
        try {
            val client = AdbClient("127.0.0.1", port, key)
            client.connect()
            clientRef.set(client)

            val apk = ctx.applicationInfo.sourceDir
            // Launch FileDaemon with shell identity via app_process
            val cmd =
                "CLASSPATH=$apk app_process /system/bin --nice-name=vr_file_daemon " +
                    "com.vrcontroler.app.service.FileDaemon"
            val shell = client.openShell(cmd)
            shellRef.set(shell)

            val banner = shell.readUntil(FileDaemon.READY, maxBytes = 64_000)
            if (!banner.contains(FileDaemon.READY)) {
                throw IllegalStateException("Daemon не прислал ready: $banner")
            }

            val pong = rpc(JSONObject().put("op", "ping"))
            if (!pong.optBoolean("ok")) throw IllegalStateException("ping failed")

            _state.value = PrivilegeState.READY
            _message.value = null
            Log.i(TAG, "FileDaemon ready on adb:$port")
        } catch (e: Exception) {
            Log.e(TAG, "connect failed", e)
            teardown()
            val msg = e.message ?: "connect error"
            if ((autoPairHint && !hasPairedBefore()) ||
                msg.contains("not A_CNXN", true) ||
                msg.contains("AUTH", true)
            ) {
                _state.value = PrivilegeState.NEED_PAIRING
                _message.value =
                    "Нужен pairing (один раз). Открой Wireless Debugging → Pair with pairing code."
            } else {
                _state.value = PrivilegeState.ERROR
                _message.value = "Не удалось подключить: $msg"
            }
        }
    }

    private suspend fun discoverPort(context: Context, type: String, timeoutMs: Long): Int? =
        suspendCoroutine { cont ->
            var resumed = false
            val mdns = AdbMdns(context, type) { port ->
                if (!resumed && port > 0) {
                    resumed = true
                    cont.resume(port)
                }
            }
            mdns.start()
            // Timeout on a background wait
            Thread {
                try {
                    Thread.sleep(timeoutMs)
                } catch (_: InterruptedException) {
                }
                mdns.stop()
                if (!resumed) {
                    resumed = true
                    cont.resume(null)
                }
            }.start()
        }

    private suspend fun rpc(req: JSONObject): JSONObject = rpcMutex.withLock {
        val shell = shellRef.get() ?: error("daemon not connected")
        shell.writeUtf8(req.toString() + "\n")
        val line = shell.readLine()
        JSONObject(line)
    }

    suspend fun listDir(path: String): String = withContext(Dispatchers.IO) {
        rpc(JSONObject().put("op", "listDir").put("path", path)).toString()
    }

    suspend fun deletePath(path: String): Boolean = withContext(Dispatchers.IO) {
        rpc(JSONObject().put("op", "deletePath").put("path", path)).optBoolean("ok")
    }

    suspend fun createDir(path: String): Boolean = withContext(Dispatchers.IO) {
        rpc(JSONObject().put("op", "createDir").put("path", path)).optBoolean("ok")
    }

    suspend fun renamePath(src: String, dst: String): Boolean = withContext(Dispatchers.IO) {
        rpc(JSONObject().put("op", "renamePath").put("src", src).put("dst", dst)).optBoolean("ok")
    }

    suspend fun copyPath(src: String, dstDir: String): String = withContext(Dispatchers.IO) {
        val r = rpc(JSONObject().put("op", "copyPath").put("src", src).put("dstDir", dstDir))
        if (r.optBoolean("ok")) "" else r.optString("error", "Ошибка копирования")
    }

    suspend fun movePath(src: String, dstDir: String): String = withContext(Dispatchers.IO) {
        val r = rpc(JSONObject().put("op", "movePath").put("src", src).put("dstDir", dstDir))
        if (r.optBoolean("ok")) "" else r.optString("error", "Ошибка перемещения")
    }

    suspend fun readTextFile(path: String): String = withContext(Dispatchers.IO) {
        rpc(JSONObject().put("op", "readTextFile").put("path", path)).toString()
    }

    suspend fun writeTextFile(path: String, content: String): String = withContext(Dispatchers.IO) {
        val r = rpc(
            JSONObject().put("op", "writeTextFile").put("path", path).put("content", content)
        )
        if (r.optBoolean("ok")) "" else r.optString("error", "Ошибка записи")
    }

    suspend fun extractZip(zipPath: String, dstDir: String): String = withContext(Dispatchers.IO) {
        val r = rpc(
            JSONObject().put("op", "extractZip").put("zipPath", zipPath).put("dstDir", dstDir)
        )
        if (r.optBoolean("ok")) "" else r.optString("error", "Ошибка распаковки")
    }

    fun teardown() {
        try { shellRef.getAndSet(null)?.close() } catch (_: Exception) {}
        try { clientRef.getAndSet(null)?.close() } catch (_: Exception) {}
    }
}
