export async function fullscreen() {
    try {
        if (document.fullscreenElement) await document.exitFullscreen();
        else await document.documentElement.requestFullscreen();
    } catch { /* Browser shortcut F11 remains available if fullscreen is denied. */ }
}
