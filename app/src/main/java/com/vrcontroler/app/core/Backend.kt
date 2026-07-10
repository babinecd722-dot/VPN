package com.vrcontroler.app.core

import com.vrcontroler.app.shizuku.ShizukuGate
import org.json.JSONObject

data class FsEntry(
    val name: String,
    val path: String,
    val isDir: Boolean,
    val size: Long,
    val mtime: Long,
)

data class ListResult(val entries: List<FsEntry>? = null, val error: String? = null)

/**
 * Routes file operations: paths under Android/data and Android/obb go through
 * the shell-privileged Shizuku service, everything else uses direct file access.
 */
object Backend {

    private val restrictedPrefixes = listOf(
        "/storage/emulated/0/Android/data",
        "/storage/emulated/0/Android/obb",
        "/sdcard/Android/data",
        "/sdcard/Android/obb",
    )

    fun isRestricted(path: String): Boolean =
        restrictedPrefixes.any { path == it || path.startsWith("$it/") }

    private fun useShell(vararg paths: String): Boolean =
        paths.any { isRestricted(it) } && ShizukuGate.remote != null

    fun listDir(path: String): ListResult {
        val raw = if (useShell(path)) {
            ShizukuGate.remote!!.listDir(path)
        } else {
            FileCore.listDir(path)
        }
        val json = JSONObject(raw)
        if (!json.optBoolean("ok")) {
            return ListResult(error = json.optString("error", "Ошибка"))
        }
        val arr = json.getJSONArray("entries")
        val list = ArrayList<FsEntry>(arr.length())
        for (i in 0 until arr.length()) {
            val o = arr.getJSONObject(i)
            list.add(
                FsEntry(
                    name = o.getString("name"),
                    path = o.getString("path"),
                    isDir = o.getBoolean("isDir"),
                    size = o.getLong("size"),
                    mtime = o.getLong("mtime"),
                )
            )
        }
        return ListResult(entries = list.sortedWith(
            compareByDescending<FsEntry> { it.isDir }.thenBy { it.name.lowercase() }
        ))
    }

    fun delete(path: String): Boolean =
        if (useShell(path)) ShizukuGate.remote!!.deletePath(path) else FileCore.deletePath(path)

    fun createDir(path: String): Boolean =
        if (useShell(path)) ShizukuGate.remote!!.createDir(path) else FileCore.createDir(path)

    fun rename(src: String, dst: String): Boolean =
        if (useShell(src, dst)) ShizukuGate.remote!!.renamePath(src, dst) else FileCore.renamePath(src, dst)

    fun copy(src: String, dstDir: String): String =
        if (useShell(src, dstDir)) ShizukuGate.remote!!.copyPath(src, dstDir) else FileCore.copyPath(src, dstDir)

    fun move(src: String, dstDir: String): String =
        if (useShell(src, dstDir)) ShizukuGate.remote!!.movePath(src, dstDir) else FileCore.movePath(src, dstDir)

    fun readText(path: String): Pair<String?, String?> {
        val raw = if (useShell(path)) ShizukuGate.remote!!.readTextFile(path) else FileCore.readTextFile(path)
        val json = JSONObject(raw)
        return if (json.optBoolean("ok")) json.getString("text") to null
        else null to json.optString("error", "Ошибка чтения")
    }

    fun writeText(path: String, content: String): String =
        if (useShell(path)) ShizukuGate.remote!!.writeTextFile(path, content) else FileCore.writeTextFile(path, content)

    fun extractZip(zipPath: String, dstDir: String): String =
        if (useShell(zipPath, dstDir)) ShizukuGate.remote!!.extractZip(zipPath, dstDir) else FileCore.extractZip(zipPath, dstDir)
}
