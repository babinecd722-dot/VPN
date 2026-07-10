package moe.shizuku.manager.adb

import android.os.Build
import android.util.Log
import androidx.annotation.RequiresApi
import java.io.Closeable
import java.io.DataInputStream
import java.io.DataOutputStream
import java.net.Socket
import java.nio.ByteBuffer
import java.nio.ByteOrder
import javax.net.ssl.SSLSocket

/**
 * Wireless ADB pairing client (SPAKE2 via libadb.so).
 * Adapted from RikkaApps/Shizuku (Apache-2.0).
 *
 * PairingContext must stay in package moe.shizuku.manager.adb — JNI RegisterNatives
 * looks up that exact class name inside libadb.so.
 */
private const val TAG = "AdbPairClient"

private const val kCurrentKeyHeaderVersion: Byte = 1
private const val kMinSupportedKeyHeaderVersion: Byte = 1
private const val kMaxSupportedKeyHeaderVersion: Byte = 1
private const val kMaxPeerInfoSize = 8192
private const val kMaxPayloadSize = kMaxPeerInfoSize * 2
private const val kExportedKeyLabel = "adb-label\u0000"
private const val kExportedKeySize = 64
private const val kPairingPacketHeaderSize = 6

private class PeerInfo(val type: Byte, data: ByteArray) {
    val data = ByteArray(kMaxPeerInfoSize - 1)

    init {
        data.copyInto(this.data, 0, 0, data.size.coerceAtMost(kMaxPeerInfoSize - 1))
    }

    object Type {
        const val ADB_RSA_PUB_KEY: Byte = 0
    }

    fun writeTo(buffer: ByteBuffer) {
        buffer.put(type)
        buffer.put(data)
    }

    companion object {
        fun readFrom(buffer: ByteBuffer): PeerInfo {
            val type = buffer.get()
            val data = ByteArray(kMaxPeerInfoSize - 1)
            buffer.get(data)
            return PeerInfo(type, data)
        }
    }
}

private class PairingPacketHeader(val version: Byte, val type: Byte, val payload: Int) {
    object Type {
        const val SPAKE2_MSG: Byte = 0
        const val PEER_INFO: Byte = 1
    }

    fun writeTo(buffer: ByteBuffer) {
        buffer.put(version)
        buffer.put(type)
        buffer.putInt(payload)
    }

    companion object {
        fun readFrom(buffer: ByteBuffer): PairingPacketHeader? {
            val version = buffer.get()
            val type = buffer.get()
            val payload = buffer.int
            if (version < kMinSupportedKeyHeaderVersion || version > kMaxSupportedKeyHeaderVersion) {
                Log.e(TAG, "PairingPacketHeader version mismatch")
                return null
            }
            if (type != Type.SPAKE2_MSG && type != Type.PEER_INFO) return null
            if (payload <= 0 || payload > kMaxPayloadSize) return null
            return PairingPacketHeader(version, type, payload)
        }
    }
}

/**
 * JNI-backed SPAKE2 context. Class name is required by libadb.so JNI_OnLoad.
 */
class PairingContext private constructor(private val nativePtr: Long) {
    val msg: ByteArray = nativeMsg(nativePtr)

    fun initCipher(theirMsg: ByteArray) = nativeInitCipher(nativePtr, theirMsg)
    fun encrypt(`in`: ByteArray) = nativeEncrypt(nativePtr, `in`)
    fun decrypt(`in`: ByteArray) = nativeDecrypt(nativePtr, `in`)
    fun destroy() = nativeDestroy(nativePtr)

    private external fun nativeMsg(nativePtr: Long): ByteArray
    private external fun nativeInitCipher(nativePtr: Long, theirMsg: ByteArray): Boolean
    private external fun nativeEncrypt(nativePtr: Long, inbuf: ByteArray): ByteArray?
    private external fun nativeDecrypt(nativePtr: Long, inbuf: ByteArray): ByteArray?
    private external fun nativeDestroy(nativePtr: Long)

    companion object {
        fun create(password: ByteArray): PairingContext? {
            val nativePtr = nativeConstructor(true, password)
            return if (nativePtr != 0L) PairingContext(nativePtr) else null
        }

        @JvmStatic
        private external fun nativeConstructor(isClient: Boolean, password: ByteArray): Long
    }
}

@RequiresApi(Build.VERSION_CODES.R)
class AdbPairingClient(
    private val host: String,
    private val port: Int,
    private val pairCode: String,
    private val key: AdbKey,
) : Closeable {

    private enum class State { Ready, ExchangingMsgs, ExchangingPeerInfo, Stopped }

    private lateinit var socket: Socket
    private lateinit var inputStream: DataInputStream
    private lateinit var outputStream: DataOutputStream
    private val peerInfo = PeerInfo(PeerInfo.Type.ADB_RSA_PUB_KEY, key.adbPublicKey)
    private lateinit var pairingContext: PairingContext
    private var state = State.Ready

    fun start(): Boolean {
        setupTlsConnection()
        state = State.ExchangingMsgs
        if (!doExchangeMsgs()) {
            state = State.Stopped
            return false
        }
        state = State.ExchangingPeerInfo
        if (!doExchangePeerInfo()) {
            state = State.Stopped
            return false
        }
        state = State.Stopped
        return true
    }

    private fun setupTlsConnection() {
        socket = Socket(host, port)
        socket.tcpNoDelay = true
        val sslSocket = key.sslContext.socketFactory.createSocket(socket, host, port, true) as SSLSocket
        sslSocket.startHandshake()
        Log.d(TAG, "Handshake succeeded.")
        inputStream = DataInputStream(sslSocket.inputStream)
        outputStream = DataOutputStream(sslSocket.outputStream)

        val pairCodeBytes = pairCode.toByteArray()
        val keyMaterial = exportKeyingMaterial(sslSocket, kExportedKeyLabel, null, kExportedKeySize)
        val passwordBytes = ByteArray(pairCode.length + keyMaterial.size)
        pairCodeBytes.copyInto(passwordBytes)
        keyMaterial.copyInto(passwordBytes, pairCodeBytes.size)

        pairingContext = PairingContext.create(passwordBytes)
            ?: error("Unable to create PairingContext.")
    }

    /** Uses platform Conscrypt (hidden API) via reflection — present on Quest/Horizon OS. */
    private fun exportKeyingMaterial(
        sslSocket: SSLSocket,
        label: String,
        context: ByteArray?,
        length: Int,
    ): ByteArray {
        val conscrypt = Class.forName("com.android.org.conscrypt.Conscrypt")
        val method = conscrypt.getMethod(
            "exportKeyingMaterial",
            SSLSocket::class.java,
            String::class.java,
            ByteArray::class.java,
            Int::class.javaPrimitiveType
        )
        return method.invoke(null, sslSocket, label, context, length) as ByteArray
    }

    private fun createHeader(type: Byte, payloadSize: Int) =
        PairingPacketHeader(kCurrentKeyHeaderVersion, type, payloadSize)

    private fun readHeader(): PairingPacketHeader? {
        val bytes = ByteArray(kPairingPacketHeaderSize)
        inputStream.readFully(bytes)
        return PairingPacketHeader.readFrom(ByteBuffer.wrap(bytes).order(ByteOrder.BIG_ENDIAN))
    }

    private fun writeHeader(header: PairingPacketHeader, payload: ByteArray) {
        val buffer = ByteBuffer.allocate(kPairingPacketHeaderSize).order(ByteOrder.BIG_ENDIAN)
        header.writeTo(buffer)
        outputStream.write(buffer.array())
        outputStream.write(payload)
    }

    private fun doExchangeMsgs(): Boolean {
        val msg = pairingContext.msg
        writeHeader(createHeader(PairingPacketHeader.Type.SPAKE2_MSG, msg.size), msg)
        val theirHeader = readHeader() ?: return false
        if (theirHeader.type != PairingPacketHeader.Type.SPAKE2_MSG) return false
        val theirMessage = ByteArray(theirHeader.payload)
        inputStream.readFully(theirMessage)
        return pairingContext.initCipher(theirMessage)
    }

    private fun doExchangePeerInfo(): Boolean {
        val buf = ByteBuffer.allocate(kMaxPeerInfoSize).order(ByteOrder.BIG_ENDIAN)
        peerInfo.writeTo(buf)
        val outbuf = pairingContext.encrypt(buf.array()) ?: return false
        writeHeader(createHeader(PairingPacketHeader.Type.PEER_INFO, outbuf.size), outbuf)

        val theirHeader = readHeader() ?: return false
        if (theirHeader.type != PairingPacketHeader.Type.PEER_INFO) return false
        val theirMessage = ByteArray(theirHeader.payload)
        inputStream.readFully(theirMessage)
        val decrypted = pairingContext.decrypt(theirMessage) ?: throw AdbInvalidPairingCodeException()
        if (decrypted.size != kMaxPeerInfoSize) return false
        PeerInfo.readFrom(ByteBuffer.wrap(decrypted))
        return true
    }

    override fun close() {
        try { inputStream.close() } catch (_: Throwable) {}
        try { outputStream.close() } catch (_: Throwable) {}
        try { socket.close() } catch (_: Exception) {}
        if (state != State.Ready) {
            try { pairingContext.destroy() } catch (_: Exception) {}
        }
    }

    companion object {
        init {
            System.loadLibrary("adb")
        }
    }
}
