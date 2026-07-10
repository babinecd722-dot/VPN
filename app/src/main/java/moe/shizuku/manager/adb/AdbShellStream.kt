package moe.shizuku.manager.adb

import moe.shizuku.manager.adb.AdbProtocol.A_CLSE
import moe.shizuku.manager.adb.AdbProtocol.A_OKAY
import moe.shizuku.manager.adb.AdbProtocol.A_WRTE
import java.io.Closeable

/**
 * Bidirectional ADB shell channel with a small stdout buffer for line-oriented RPC.
 */
class AdbShellStream(
    private val client: AdbClient,
    private val localId: Int,
    private var remoteId: Int,
) : Closeable {
    private var closed = false
    private val stdout = StringBuilder()

    fun writeUtf8(text: String) {
        check(!closed)
        client.write(A_WRTE, localId, remoteId, text.toByteArray(Charsets.UTF_8))
        // Drain until we see OKAY for our write (WRTE from remote may arrive first)
        while (!closed) {
            val msg = client.read()
            when (msg.command) {
                A_OKAY -> {
                    remoteId = msg.arg0
                    return
                }
                A_WRTE -> {
                    if (msg.data_length > 0) {
                        stdout.append(String(msg.data!!, Charsets.UTF_8))
                    }
                    client.write(A_OKAY, localId, msg.arg0)
                    remoteId = msg.arg0
                }
                A_CLSE -> {
                    closed = true
                    client.write(A_CLSE, localId, msg.arg0)
                    error("shell closed while writing")
                }
                else -> error("unexpected shell message ${msg.command}")
            }
        }
    }

    /** Block until a full line is available on stdout. */
    fun readLine(maxBytes: Int = 8_000_000): String {
        while (!closed) {
            val idx = stdout.indexOf("\n")
            if (idx >= 0) {
                val line = stdout.substring(0, idx)
                stdout.delete(0, idx + 1)
                return line.trimEnd('\r')
            }
            if (stdout.length > maxBytes) error("shell line too large")

            val msg = client.read()
            when (msg.command) {
                A_WRTE -> {
                    if (msg.data_length > 0) {
                        stdout.append(String(msg.data!!, Charsets.UTF_8))
                    }
                    client.write(A_OKAY, localId, msg.arg0)
                    remoteId = msg.arg0
                }
                A_OKAY -> remoteId = msg.arg0
                A_CLSE -> {
                    closed = true
                    client.write(A_CLSE, localId, msg.arg0)
                    break
                }
                else -> error("unexpected shell message")
            }
        }
        val leftover = stdout.toString()
        stdout.clear()
        return leftover.trimEnd('\r', '\n')
    }

    /** Read until [marker] appears anywhere in the buffer (kept for leftover). */
    fun readUntil(marker: String, maxBytes: Int = 64_000): String {
        while (!closed) {
            if (stdout.contains(marker)) {
                val all = stdout.toString()
                // Keep everything after the marker line for subsequent RPC
                val markerIdx = all.indexOf(marker)
                val after = all.indexOf('\n', markerIdx).let { if (it < 0) all.length else it + 1 }
                val consumed = all.substring(0, after)
                stdout.delete(0, after)
                return consumed
            }
            if (stdout.length > maxBytes) error("shell output too large waiting for $marker")

            val msg = client.read()
            when (msg.command) {
                A_WRTE -> {
                    if (msg.data_length > 0) {
                        stdout.append(String(msg.data!!, Charsets.UTF_8))
                    }
                    client.write(A_OKAY, localId, msg.arg0)
                    remoteId = msg.arg0
                }
                A_OKAY -> remoteId = msg.arg0
                A_CLSE -> {
                    closed = true
                    client.write(A_CLSE, localId, msg.arg0)
                    break
                }
                else -> error("unexpected shell message")
            }
        }
        return stdout.toString()
    }

    override fun close() {
        if (closed) return
        try {
            client.write(A_CLSE, localId, remoteId)
        } catch (_: Exception) {
        }
        closed = true
    }
}
