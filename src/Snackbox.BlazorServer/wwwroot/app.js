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
