package com.vrcontroler.app.ui

import android.os.Environment
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.vrcontroler.app.core.Backend
import com.vrcontroler.app.core.FsEntry
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.File

data class Shortcut(val label: String, val path: String, val emoji: String)

enum class ClipMode { COPY, MOVE }
data class Clipboard(val paths: List<String>, val mode: ClipMode)

class FileManagerViewModel : ViewModel() {

    val root: String = Environment.getExternalStorageDirectory().absolutePath

    val shortcuts = listOf(
        Shortcut("Хранилище", root, "💾"),
        Shortcut("Загрузки", "$root/Download", "⬇️"),
        Shortcut("Android/data", "$root/Android/data", "🔓"),
        Shortcut("BONELAB", "$root/Android/data/com.StressLevelZero.BONELAB/files", "🦴"),
    )

    var currentPath by mutableStateOf(root)
        private set
    var entries by mutableStateOf<List<FsEntry>>(emptyList())
        private set
    var error by mutableStateOf<String?>(null)
        private set
    var loading by mutableStateOf(false)
        private set
    var selection by mutableStateOf<Set<String>>(emptySet())
        private set
    var clipboard by mutableStateOf<Clipboard?>(null)
        private set
    var toast by mutableStateOf<String?>(null)

    var editorPath by mutableStateOf<String?>(null)
        private set
    var editorText by mutableStateOf("")

    fun navigate(path: String) {
        currentPath = path
        selection = emptySet()
        refresh()
    }

    fun goUp() {
        val parent = File(currentPath).parent ?: return
        if (currentPath == root) return
        navigate(parent)
    }

    fun refresh() {
        loading = true
        error = null
        val path = currentPath
        viewModelScope.launch {
            val result = withContext(Dispatchers.IO) { Backend.listDir(path) }
            if (path == currentPath) {
                entries = result.entries ?: emptyList()
                error = result.error
                loading = false
            }
        }
    }

    fun toggleSelect(path: String) {
        selection = if (path in selection) selection - path else selection + path
    }

    fun clearSelection() {
        selection = emptySet()
    }

    fun copySelection() {
        clipboard = Clipboard(selection.toList(), ClipMode.COPY)
        toast = "Скопировано в буфер: ${selection.size}"
        clearSelection()
    }

    fun cutSelection() {
        clipboard = Clipboard(selection.toList(), ClipMode.MOVE)
        toast = "Вырезано: ${selection.size}"
        clearSelection()
    }

    fun paste() {
        val clip = clipboard ?: return
        val dst = currentPath
        runOp {
            var failures = 0
            for (src in clip.paths) {
                val err = if (clip.mode == ClipMode.COPY) Backend.copy(src, dst) else Backend.move(src, dst)
                if (err.isNotEmpty()) failures++
            }
            clipboard = null
            if (failures == 0) "Готово" else "Ошибок: $failures"
        }
    }

    fun deleteSelection() {
        val targets = selection.toList()
        clearSelection()
        runOp {
            val failures = targets.count { !Backend.delete(it) }
            if (failures == 0) "Удалено: ${targets.size}" else "Не удалось удалить: $failures"
        }
    }

    fun createFolder(name: String) {
        if (name.isBlank()) return
        runOp {
            if (Backend.createDir("$currentPath/${name.trim()}")) "Папка создана" else "Не удалось создать папку"
        }
    }

    fun rename(entry: FsEntry, newName: String) {
        if (newName.isBlank()) return
        runOp {
            val dst = "${File(entry.path).parent}/${newName.trim()}"
            if (Backend.rename(entry.path, dst)) "Переименовано" else "Не удалось переименовать"
        }
    }

    fun extract(entry: FsEntry) {
        runOp {
            val dst = "${File(entry.path).parent}/${entry.name.removeSuffix(".zip")}"
            val err = Backend.extractZip(entry.path, dst)
            if (err.isEmpty()) "Распаковано в ${File(dst).name}" else "Ошибка: $err"
        }
    }

    fun openEditor(entry: FsEntry) {
        viewModelScope.launch {
            val (text, err) = withContext(Dispatchers.IO) { Backend.readText(entry.path) }
            if (text != null) {
                editorPath = entry.path
                editorText = text
            } else {
                toast = err
            }
        }
    }

    fun saveEditor() {
        val path = editorPath ?: return
        val text = editorText
        viewModelScope.launch {
            val err = withContext(Dispatchers.IO) { Backend.writeText(path, text) }
            toast = if (err.isEmpty()) "Сохранено" else "Ошибка: $err"
        }
    }

    fun closeEditor() {
        editorPath = null
        editorText = ""
    }

    private fun runOp(op: suspend () -> String) {
        loading = true
        viewModelScope.launch {
            val msg = withContext(Dispatchers.IO) { op() }
            toast = msg
            refresh()
        }
    }
}
