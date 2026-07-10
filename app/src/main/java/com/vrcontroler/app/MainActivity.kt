package com.vrcontroler.app

import android.content.Intent
import android.net.Uri
import android.os.Bundle
import android.os.Environment
import android.provider.Settings
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.viewModels
import androidx.compose.foundation.ExperimentalFoundationApi
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.horizontalScroll
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material.icons.automirrored.filled.InsertDriveFile
import androidx.compose.material.icons.filled.*
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.vrcontroler.app.core.FsEntry
import com.vrcontroler.app.shizuku.ShizukuGate
import com.vrcontroler.app.shizuku.ShizukuState
import com.vrcontroler.app.ui.FileManagerViewModel
import com.vrcontroler.app.ui.VRControlerTheme
import java.text.SimpleDateFormat
import java.util.Date
import java.util.Locale

class MainActivity : ComponentActivity() {

    private val vm: FileManagerViewModel by viewModels()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        ShizukuGate.init()
        setContent {
            VRControlerTheme {
                AppScreen(vm,
                    hasAllFiles = { Environment.isExternalStorageManager() },
                    requestAllFiles = { requestAllFilesAccess() })
            }
        }
    }

    override fun onResume() {
        super.onResume()
        ShizukuGate.refresh()
        vm.refresh()
    }

    private fun requestAllFilesAccess() {
        try {
            startActivity(
                Intent(
                    Settings.ACTION_MANAGE_APP_ALL_FILES_ACCESS_PERMISSION,
                    Uri.parse("package:$packageName")
                )
            )
        } catch (e: Exception) {
            startActivity(Intent(Settings.ACTION_MANAGE_ALL_FILES_ACCESS_PERMISSION))
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AppScreen(
    vm: FileManagerViewModel,
    hasAllFiles: () -> Boolean,
    requestAllFiles: () -> Unit,
) {
    val shizukuState by ShizukuGate.state.collectAsState()
    var allFiles by remember { mutableStateOf(hasAllFiles()) }
    var showNewFolder by remember { mutableStateOf(false) }
    var renameTarget by remember { mutableStateOf<FsEntry?>(null) }
    var deleteConfirm by remember { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(vm.toast) {
        vm.toast?.let {
            snackbar.showSnackbar(it)
            vm.toast = null
        }
    }
    LaunchedEffect(Unit) {
        allFiles = hasAllFiles()
        if (allFiles) vm.refresh()
    }

    Scaffold(
        snackbarHost = { SnackbarHost(snackbar) },
        topBar = {
            TopAppBar(
                title = {
                    Text(
                        "VR CONTROLER",
                        fontWeight = FontWeight.Black,
                        letterSpacing = 2.sp,
                        color = MaterialTheme.colorScheme.primary
                    )
                },
                actions = {
                    IconButton(onClick = { vm.refresh() }) {
                        Icon(Icons.Default.Refresh, "Обновить")
                    }
                }
            )
        }
    ) { pad ->
        Column(Modifier.padding(pad).fillMaxSize()) {

            if (!allFiles) {
                PermissionCard(
                    title = "Нужен доступ ко всем файлам",
                    body = "Разреши «Управление всеми файлами», чтобы видеть всё хранилище очков.",
                    button = "Разрешить",
                    onClick = requestAllFiles
                )
            }
            if (shizukuState != ShizukuState.READY) {
                PermissionCard(
                    title = "Android/data: нужен Shizuku",
                    body = when (shizukuState) {
                        ShizukuState.NOT_RUNNING -> "Shizuku не запущен. Установи Shizuku (Quest-совместимую сборку), запусти сервис через Wireless Debugging и вернись сюда."
                        ShizukuState.NO_PERMISSION -> "Shizuku запущен — дай разрешение приложению."
                        ShizukuState.CONNECTING -> "Подключение к Shizuku…"
                        else -> "Shizuku недоступен."
                    },
                    button = if (shizukuState == ShizukuState.NO_PERMISSION) "Дать доступ" else "Проверить",
                    onClick = {
                        if (shizukuState == ShizukuState.NO_PERMISSION) ShizukuGate.requestAccess()
                        else ShizukuGate.refresh()
                    }
                )
            }

            ShortcutRow(vm)
            Breadcrumbs(vm)

            vm.error?.let {
                Text(
                    it,
                    color = MaterialTheme.colorScheme.error,
                    modifier = Modifier.padding(16.dp)
                )
            }

            Box(Modifier.weight(1f)) {
                if (vm.loading) {
                    CircularProgressIndicator(Modifier.align(Alignment.Center))
                }
                LazyColumn(Modifier.fillMaxSize()) {
                    items(vm.entries, key = { it.path }) { entry ->
                        FileRow(
                            entry = entry,
                            selected = entry.path in vm.selection,
                            selectionMode = vm.selection.isNotEmpty(),
                            onClick = {
                                when {
                                    vm.selection.isNotEmpty() -> vm.toggleSelect(entry.path)
                                    entry.isDir -> vm.navigate(entry.path)
                                    entry.name.endsWith(".zip", true) -> vm.extract(entry)
                                    isTextFile(entry.name) -> vm.openEditor(entry)
                                    else -> vm.toggleSelect(entry.path)
                                }
                            },
                            onLongClick = { vm.toggleSelect(entry.path) },
                            onRename = { renameTarget = entry }
                        )
                    }
                }
            }

            BottomBar(
                vm = vm,
                onNewFolder = { showNewFolder = true },
                onDelete = { deleteConfirm = true }
            )
        }
    }

    if (showNewFolder) {
        TextDialog(
            title = "Новая папка",
            initial = "",
            confirm = "Создать",
            onDismiss = { showNewFolder = false },
            onConfirm = {
                vm.createFolder(it)
                showNewFolder = false
            }
        )
    }
    renameTarget?.let { target ->
        TextDialog(
            title = "Переименовать",
            initial = target.name,
            confirm = "OK",
            onDismiss = { renameTarget = null },
            onConfirm = {
                vm.rename(target, it)
                renameTarget = null
            }
        )
    }
    if (deleteConfirm) {
        AlertDialog(
            onDismissRequest = { deleteConfirm = false },
            title = { Text("Удалить выбранное?") },
            text = { Text("Будет удалено: ${vm.selection.size}. Это действие необратимо.") },
            confirmButton = {
                TextButton(onClick = {
                    vm.deleteSelection()
                    deleteConfirm = false
                }) { Text("Удалить", color = MaterialTheme.colorScheme.error) }
            },
            dismissButton = {
                TextButton(onClick = { deleteConfirm = false }) { Text("Отмена") }
            }
        )
    }
    vm.editorPath?.let { path ->
        EditorDialog(vm, path)
    }
}

@Composable
private fun PermissionCard(title: String, body: String, button: String, onClick: () -> Unit) {
    Card(
        Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 4.dp),
        colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surfaceVariant)
    ) {
        Column(Modifier.padding(14.dp)) {
            Text(title, fontWeight = FontWeight.Bold, fontSize = 16.sp)
            Spacer(Modifier.height(4.dp))
            Text(body, fontSize = 13.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Spacer(Modifier.height(8.dp))
            Button(onClick = onClick) { Text(button) }
        }
    }
}

@Composable
private fun ShortcutRow(vm: FileManagerViewModel) {
    Row(
        Modifier.fillMaxWidth().horizontalScroll(rememberScrollState()).padding(8.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp)
    ) {
        vm.shortcuts.forEach { s ->
            AssistChip(
                onClick = { vm.navigate(s.path) },
                label = { Text("${s.emoji} ${s.label}") }
            )
        }
    }
}

@Composable
private fun Breadcrumbs(vm: FileManagerViewModel) {
    val rel = vm.currentPath.removePrefix(vm.root).trimStart('/')
    Row(
        Modifier.fillMaxWidth().padding(horizontal = 12.dp, vertical = 4.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        IconButton(onClick = { vm.goUp() }, enabled = vm.currentPath != vm.root) {
            Icon(Icons.AutoMirrored.Filled.ArrowBack, "Вверх")
        }
        Text(
            if (rel.isEmpty()) "/" else "/$rel",
            fontFamily = FontFamily.Monospace,
            fontSize = 14.sp,
            maxLines = 1,
            modifier = Modifier.horizontalScroll(rememberScrollState())
        )
    }
}

@OptIn(ExperimentalFoundationApi::class)
@Composable
private fun FileRow(
    entry: FsEntry,
    selected: Boolean,
    selectionMode: Boolean,
    onClick: () -> Unit,
    onLongClick: () -> Unit,
    onRename: () -> Unit,
) {
    Row(
        Modifier
            .fillMaxWidth()
            .clickable(onClick = onClick)
            .background(
                if (selected) MaterialTheme.colorScheme.primary.copy(alpha = 0.15f)
                else MaterialTheme.colorScheme.background
            )
            .padding(horizontal = 12.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically
    ) {
        Checkbox(checked = selected, onCheckedChange = { onLongClick() })
        Icon(
            if (entry.isDir) Icons.Default.Folder else Icons.AutoMirrored.Filled.InsertDriveFile,
            null,
            tint = if (entry.isDir) MaterialTheme.colorScheme.primary
            else MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.size(28.dp)
        )
        Spacer(Modifier.width(12.dp))
        Column(Modifier.weight(1f)) {
            Text(entry.name, fontSize = 16.sp, maxLines = 1)
            Text(
                buildString {
                    if (!entry.isDir) {
                        append(formatSize(entry.size))
                        append("  ·  ")
                    }
                    append(
                        SimpleDateFormat("dd.MM.yyyy HH:mm", Locale.getDefault())
                            .format(Date(entry.mtime))
                    )
                },
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant
            )
        }
        IconButton(onClick = onRename) {
            Icon(Icons.Default.Edit, "Переименовать", modifier = Modifier.size(20.dp))
        }
    }
    HorizontalDivider(color = MaterialTheme.colorScheme.surfaceVariant, thickness = 0.5.dp)
}

@Composable
private fun BottomBar(vm: FileManagerViewModel, onNewFolder: () -> Unit, onDelete: () -> Unit) {
    Surface(color = MaterialTheme.colorScheme.surface, shadowElevation = 8.dp) {
        Row(
            Modifier.fillMaxWidth().padding(8.dp),
            horizontalArrangement = Arrangement.SpaceEvenly
        ) {
            BarButton(Icons.Default.CreateNewFolder, "Папка", true, onNewFolder)
            BarButton(Icons.Default.ContentCopy, "Копир.", vm.selection.isNotEmpty()) { vm.copySelection() }
            BarButton(Icons.Default.ContentCut, "Вырез.", vm.selection.isNotEmpty()) { vm.cutSelection() }
            BarButton(Icons.Default.ContentPaste, "Встав.", vm.clipboard != null) { vm.paste() }
            BarButton(Icons.Default.Delete, "Удал.", vm.selection.isNotEmpty(), onDelete)
            BarButton(Icons.Default.Close, "Снять", vm.selection.isNotEmpty()) { vm.clearSelection() }
        }
    }
}

@Composable
private fun BarButton(icon: androidx.compose.ui.graphics.vector.ImageVector, label: String, enabled: Boolean, onClick: () -> Unit) {
    Column(
        Modifier
            .clip(RoundedCornerShape(12.dp))
            .clickable(enabled = enabled, onClick = onClick)
            .padding(horizontal = 10.dp, vertical = 6.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        Icon(
            icon, label,
            tint = if (enabled) MaterialTheme.colorScheme.primary
            else MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.4f)
        )
        Text(
            label, fontSize = 11.sp,
            color = if (enabled) MaterialTheme.colorScheme.onSurface
            else MaterialTheme.colorScheme.onSurfaceVariant.copy(alpha = 0.4f)
        )
    }
}

@Composable
private fun TextDialog(
    title: String,
    initial: String,
    confirm: String,
    onDismiss: () -> Unit,
    onConfirm: (String) -> Unit,
) {
    var value by remember { mutableStateOf(initial) }
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = {
            OutlinedTextField(value = value, onValueChange = { value = it }, singleLine = true)
        },
        confirmButton = { TextButton(onClick = { onConfirm(value) }) { Text(confirm) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text("Отмена") } }
    )
}

@Composable
private fun EditorDialog(vm: FileManagerViewModel, path: String) {
    AlertDialog(
        onDismissRequest = { vm.closeEditor() },
        title = { Text(path.substringAfterLast('/'), fontFamily = FontFamily.Monospace, fontSize = 15.sp) },
        text = {
            OutlinedTextField(
                value = vm.editorText,
                onValueChange = { vm.editorText = it },
                modifier = Modifier.fillMaxWidth().height(320.dp),
                textStyle = androidx.compose.ui.text.TextStyle(
                    fontFamily = FontFamily.Monospace,
                    fontSize = 13.sp
                )
            )
        },
        confirmButton = {
            TextButton(onClick = {
                vm.saveEditor()
                vm.closeEditor()
            }) { Text("Сохранить") }
        },
        dismissButton = { TextButton(onClick = { vm.closeEditor() }) { Text("Закрыть") } }
    )
}

private fun isTextFile(name: String): Boolean {
    val n = name.lowercase()
    return listOf(".txt", ".json", ".xml", ".cfg", ".ini", ".log", ".md", ".yaml", ".yml")
        .any { n.endsWith(it) }
}

private fun formatSize(bytes: Long): String {
    if (bytes < 1024) return "$bytes Б"
    val kb = bytes / 1024.0
    if (kb < 1024) return "%.1f КБ".format(kb)
    val mb = kb / 1024.0
    if (mb < 1024) return "%.1f МБ".format(mb)
    return "%.2f ГБ".format(mb / 1024.0)
}
