package com.vrcontroler.app.service

import com.vrcontroler.app.IFileService
import com.vrcontroler.app.core.FileCore
import kotlin.system.exitProcess

/** Runs inside the Shizuku-managed process with shell (ADB) privileges. */
class FileService : IFileService.Stub() {

    override fun destroy() {
        exitProcess(0)
    }

    override fun exit() {
        destroy()
    }

    override fun listDir(path: String): String = FileCore.listDir(path)
    override fun deletePath(path: String): Boolean = FileCore.deletePath(path)
    override fun createDir(path: String): Boolean = FileCore.createDir(path)
    override fun renamePath(src: String, dst: String): Boolean = FileCore.renamePath(src, dst)
    override fun copyPath(src: String, dstDir: String): String = FileCore.copyPath(src, dstDir)
    override fun movePath(src: String, dstDir: String): String = FileCore.movePath(src, dstDir)
    override fun readTextFile(path: String): String = FileCore.readTextFile(path)
    override fun writeTextFile(path: String, content: String): String = FileCore.writeTextFile(path, content)
    override fun extractZip(zipPath: String, dstDir: String): String = FileCore.extractZip(zipPath, dstDir)
    override fun statPath(path: String): String = FileCore.statPath(path)
}
