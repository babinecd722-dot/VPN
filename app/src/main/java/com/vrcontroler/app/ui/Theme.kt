package com.vrcontroler.app.ui

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

private val VrColors = darkColorScheme(
    primary = Color(0xFF66D9EF),
    onPrimary = Color(0xFF00252B),
    secondary = Color(0xFFB388FF),
    onSecondary = Color(0xFF1A0033),
    background = Color(0xFF0D1117),
    onBackground = Color(0xFFE6EDF3),
    surface = Color(0xFF161B22),
    onSurface = Color(0xFFE6EDF3),
    surfaceVariant = Color(0xFF21262D),
    onSurfaceVariant = Color(0xFF9DA7B3),
    error = Color(0xFFFF6B6B),
)

@Composable
fun VRControlerTheme(content: @Composable () -> Unit) {
    isSystemInDarkTheme()
    MaterialTheme(colorScheme = VrColors, content = content)
}
