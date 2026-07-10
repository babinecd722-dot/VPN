package com.vrcontroler.app.shizuku

import android.content.ComponentName
import android.content.ServiceConnection
import android.content.pm.PackageManager
import android.os.IBinder
import com.vrcontroler.app.BuildConfig
import com.vrcontroler.app.IFileService
import com.vrcontroler.app.service.FileService
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import rikka.shizuku.Shizuku

enum class ShizukuState { NOT_INSTALLED, NOT_RUNNING, NO_PERMISSION, CONNECTING, READY }

/** Manages the Shizuku connection and the shell-privileged FileService. */
object ShizukuGate {

    private const val REQUEST_CODE = 4242

    private val _state = MutableStateFlow(ShizukuState.NOT_RUNNING)
    val state: StateFlow<ShizukuState> = _state

    @Volatile
    var remote: IFileService? = null
        private set

    private val userServiceArgs = Shizuku.UserServiceArgs(
        ComponentName(BuildConfig.APPLICATION_ID, FileService::class.java.name)
    )
        .daemon(false)
        .processNameSuffix("file_service")
        .debuggable(BuildConfig.DEBUG)
        .version(BuildConfig.VERSION_CODE)

    private val connection = object : ServiceConnection {
        override fun onServiceConnected(name: ComponentName?, binder: IBinder?) {
            if (binder != null && binder.pingBinder()) {
                remote = IFileService.Stub.asInterface(binder)
                _state.value = ShizukuState.READY
            } else {
                _state.value = ShizukuState.NOT_RUNNING
            }
        }

        override fun onServiceDisconnected(name: ComponentName?) {
            remote = null
            refresh()
        }
    }

    private val permissionListener =
        Shizuku.OnRequestPermissionResultListener { code, result ->
            if (code == REQUEST_CODE && result == PackageManager.PERMISSION_GRANTED) {
                bindService()
            } else {
                refresh()
            }
        }

    private val binderReceivedListener = Shizuku.OnBinderReceivedListener { refresh() }
    private val binderDeadListener = Shizuku.OnBinderDeadListener {
        remote = null
        _state.value = ShizukuState.NOT_RUNNING
    }

    fun init() {
        Shizuku.addBinderReceivedListenerSticky(binderReceivedListener)
        Shizuku.addBinderDeadListener(binderDeadListener)
        Shizuku.addRequestPermissionResultListener(permissionListener)
        refresh()
    }

    fun refresh() {
        if (remote != null) {
            _state.value = ShizukuState.READY
            return
        }
        if (!Shizuku.pingBinder()) {
            _state.value = ShizukuState.NOT_RUNNING
            return
        }
        _state.value = if (hasPermission()) {
            bindService()
            ShizukuState.CONNECTING
        } else {
            ShizukuState.NO_PERMISSION
        }
    }

    fun requestAccess() {
        if (!Shizuku.pingBinder()) {
            _state.value = ShizukuState.NOT_RUNNING
            return
        }
        if (hasPermission()) {
            bindService()
            _state.value = ShizukuState.CONNECTING
        } else if (!Shizuku.shouldShowRequestPermissionRationale()) {
            Shizuku.requestPermission(REQUEST_CODE)
        }
    }

    private fun hasPermission(): Boolean = try {
        Shizuku.checkSelfPermission() == PackageManager.PERMISSION_GRANTED
    } catch (e: Exception) {
        false
    }

    private fun bindService() {
        try {
            Shizuku.bindUserService(userServiceArgs, connection)
        } catch (e: Exception) {
            _state.value = ShizukuState.NOT_RUNNING
        }
    }
}
