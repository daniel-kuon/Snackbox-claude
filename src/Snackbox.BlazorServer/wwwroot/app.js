// Register the service worker so the site can be installed as an app (PWA)
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.register('service-worker.js').catch(() => { /* not fatal */ });
}

// Helper function to download files from base64 data
window.downloadFile = function (filename, base64Content) {
    const link = document.createElement('a');
    link.href = 'data:application/octet-stream;base64,' + base64Content;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// The tablet page runs full screen: browsers only allow that after a touch, so the first touch
// asks for it (again after the screen was off, which ends full screen in some browsers). An app
// installed from the tablet manifest starts full screen anyway.
// Firefox on Android grants it on a completed tap (click / touchend), not on the first touch.
const snackboxTabletFullscreen = () => {
    if (!location.pathname.startsWith('/tablet') || document.fullscreenElement) return;
    const el = document.documentElement;
    if (el.requestFullscreen) el.requestFullscreen({ navigationUI: 'hide' }).catch(() => { /* not allowed here - fine */ });
};
document.addEventListener('click', snackboxTabletFullscreen, true);
document.addEventListener('touchend', snackboxTabletFullscreen, true);

// The tablet keeps its screen on while a purchase is open (Screen Wake Lock API - needs a secure
// context, which http://127.0.0.1 is); released afterwards, so Android's timeout dims it again.
let snackboxWakeLock = null;
window.snackboxKeepScreenOn = async (on) => {
    try {
        if (on && !snackboxWakeLock && navigator.wakeLock) {
            snackboxWakeLock = await navigator.wakeLock.request('screen');
            snackboxWakeLock.addEventListener('release', () => { snackboxWakeLock = null; });
        } else if (!on && snackboxWakeLock) {
            await snackboxWakeLock.release();
            snackboxWakeLock = null;
        }
    } catch { /* not supported or not allowed - the system timeout applies */ }
};
