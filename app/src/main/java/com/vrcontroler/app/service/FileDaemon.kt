package com.vrcontroler.app.service

import com.vrcontroler.app.core.FileCore
import org.json.JSONObject
import java.io.BufferedReader
import java.io.InputStreamReader
import java.io.PrintStream

/**
 * Shell-privileged JSON-RPC daemon started via `app_process` over wireless ADB.
 *
 * Protocol (one JSON object per line):
 *   {"op":"listDir","path":"..."}
 *   {"op":"ping"}
 *   {"op":"exit"}
 *
 * Responses are a single JSON line. Ready signal: "__VR_DAEMON_READY__"
 */
object FileDaemon {
    const val READY = "__VR_DAEMON_READY__"

    @JvmStatic
    fun main(args: Array<String>) {
        val out = PrintStream(System.out, true, Charsets.UTF_8.name())
        val err = PrintStream(System.err, true, Charsets.UTF_8.name())
        out.println(READY)
        out.flush()

        val reader = BufferedReader(InputStreamReader(System.`in`, Charsets.UTF_8))
        while (true) {
            val line = try {
                reader.readLine() ?: break
            } catch (e: Exception) {
                break
            }
            if (line.isBlank()) continue
            try {
                val req = JSONObject(line)
                when (req.getString("op")) {
                    "ping" -> out.println(JSONObject().put("ok", true).put("pong", true))
                    "exit" -> {
                        out.println(JSONObject().put("ok", true))
                        break
                    }
                    "listDir" -> out.println(FileCore.listDir(req.getString("path")))
                    "statPath" -> out.println(FileCore.statPath(req.getString("path")))
                    "deletePath" -> out.println(
                        JSONObject().put("ok", FileCore.deletePath(req.getString("path")))
                    )
                    "createDir" -> out.println(
                        JSONObject().put("ok", FileCore.createDir(req.getString("path")))
                    )
                    "renamePath" -> out.println(
                        JSONObject().put(
                            "ok",
                            FileCore.renamePath(req.getString("src"), req.getString("dst"))
                        )
                    )
                    "copyPath" -> {
                        val errMsg = FileCore.copyPath(req.getString("src"), req.getString("dstDir"))
                        out.println(
                            JSONObject().put("ok", errMsg.isEmpty()).put("error", errMsg)
                        )
                    }
                    "movePath" -> {
                        val errMsg = FileCore.movePath(req.getString("src"), req.getString("dstDir"))
                        out.println(
                            JSONObject().put("ok", errMsg.isEmpty()).put("error", errMsg)
                        )
                    }
                    "readTextFile" -> out.println(FileCore.readTextFile(req.getString("path")))
                    "writeTextFile" -> {
                        val errMsg = FileCore.writeTextFile(
                            req.getString("path"),
                            req.getString("content")
                        )
                        out.println(
                            JSONObject().put("ok", errMsg.isEmpty()).put("error", errMsg)
                        )
                    }
                    "extractZip" -> {
                        val errMsg = FileCore.extractZip(
                            req.getString("zipPath"),
                            req.getString("dstDir")
                        )
                        out.println(
                            JSONObject().put("ok", errMsg.isEmpty()).put("error", errMsg)
                        )
                    }
                    else -> out.println(
                        JSONObject().put("ok", false).put("error", "unknown op")
                    )
                }
                out.flush()
            } catch (e: Exception) {
                err.println("daemon error: ${e.message}")
                out.println(
                    JSONObject().put("ok", false).put("error", e.message ?: "error")
                )
                out.flush()
            }
        }
    }
}
