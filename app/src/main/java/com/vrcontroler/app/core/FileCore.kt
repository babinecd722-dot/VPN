package com.vrcontroler.app.core

import org.json.JSONArray
import org.json.JSONObject
import java.io.File
import java.util.zip.ZipFile

/**
 * Pure java.io file operations, shared by the in-process backend and the
 * shell-privileged FileDaemon (started over wireless ADB).
 */
object FileCore {

    fun listDir(path: String): String {
        val dir = File(path)
        if (!dir.exists()) return err("Путь не существует: $path")
        if (!dir.isDirectory) return err("Не папка: $path")
        val children = dir.listFiles() ?: return err("Нет доступа к: $path")
        val arr = JSONArray()
        for (f in children) {
            arr.put(JSONObject().apply {
                put("name", f.name)
                put("path", f.absolutePath)
                put("isDir", f.isDirectory)
                put("size", if (f.isFile) f.length() else 0L)
                put("mtime", f.lastModified())
            })
        }
        return JSONObject().put("ok", true).put("entries", arr).toString()
    }

    fun statPath(path: String): String {
        val f = File(path)
        return JSONObject().apply {
            put("exists", f.exists())
            put("isDir", f.isDirectory)
            put("size", if (f.isFile) f.length() else 0L)
            put("canRead", f.canRead())
            put("canWrite", f.canWrite())
        }.toString()
    }

    fun deletePath(path: String): Boolean = File(path).deleteRecursively()

    fun createDir(path: String): Boolean = File(path).mkdirs()

    fun renamePath(src: String, dst: String): Boolean = File(src).renameTo(File(dst))

    fun copyPath(src: String, dstDir: String): String {
        return try {
            val s = File(src)
            val d = File(dstDir, s.name)
            if (d.absolutePath == s.absolutePath) return "Источник и назначение совпадают"
            if (s.isDirectory) {
                if (!s.copyRecursively(d, overwrite = true)) return "Не удалось скопировать папку"
            } else {
                s.copyTo(d, overwrite = true)
            }
            ""
        } catch (e: Exception) {
            e.message ?: "Ошибка копирования"
        }
    }

    fun movePath(src: String, dstDir: String): String {
        val s = File(src)
        val d = File(dstDir, s.name)
        if (d.absolutePath == s.absolutePath) return "Источник и назначение совпадают"
        if (s.renameTo(d)) return ""
        val copyErr = copyPath(src, dstDir)
        if (copyErr.isNotEmpty()) return copyErr
        return if (s.deleteRecursively()) "" else "Скопировано, но не удалось удалить оригинал"
    }

    fun readTextFile(path: String): String {
        return try {
            val f = File(path)
            if (f.length() > 2_000_000) return err("Файл слишком большой для редактора")
            JSONObject().put("ok", true).put("text", f.readText()).toString()
        } catch (e: Exception) {
            err(e.message ?: "Ошибка чтения")
        }
    }

    fun writeTextFile(path: String, content: String): String {
        return try {
            File(path).writeText(content)
            ""
        } catch (e: Exception) {
            e.message ?: "Ошибка записи"
        }
    }

    fun extractZip(zipPath: String, dstDir: String): String {
        return try {
            val dst = File(dstDir)
            dst.mkdirs()
            ZipFile(zipPath).use { zip ->
                val entries = zip.entries()
                while (entries.hasMoreElements()) {
                    val e = entries.nextElement()
                    val out = File(dst, e.name)
                    if (!out.canonicalPath.startsWith(dst.canonicalPath + File.separator) &&
                        out.canonicalPath != dst.canonicalPath
                    ) continue
                    if (e.isDirectory) {
                        out.mkdirs()
                    } else {
                        out.parentFile?.mkdirs()
                        zip.getInputStream(e).use { input ->
                            out.outputStream().use { output -> input.copyTo(output) }
                        }
                    }
                }
            }
            ""
        } catch (e: Exception) {
            e.message ?: "Ошибка распаковки"
        }
    }

    private fun err(msg: String): String =
        JSONObject().put("ok", false).put("error", msg).toString()
}
