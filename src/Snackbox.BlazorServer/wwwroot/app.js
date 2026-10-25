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

// Until the tablet page is full screen it shows a welcome screen over everything (Tablet.razor):
// the first tap only starts full screen, and the page's jump when Firefox's bars go away happens
// behind it. Its text is pinned to the middle of the physical screen - mozInnerScreenY is where the
// page starts on it - so nothing moves; the welcome screen goes once the page has its full-screen size.
const snackboxTabletWelcome = () => {
    // Screen off: whatever is up stays up for the wake-up
    if (document.hidden) return;
    const root = document.documentElement;
    // Screen pixels to page pixels: the page may be scaled to the screen (Firefox's desktop mode)
    const scale = innerWidth / screen.width;
    root.style.setProperty('--sb-screen-mid', (screen.height / 2 - (window.mozInnerScreenY || 0)) * scale + 'px');
    const welcome = () => location.pathname.startsWith('/tablet') && document.fullscreenEnabled && !document.fullscreenElement;
    if (welcome()) root.classList.add('sb-welcome');
    else setTimeout(() => { if (!welcome()) root.classList.remove('sb-welcome'); }, 150);
};
document.addEventListener('fullscreenchange', snackboxTabletWelcome);
window.addEventListener('resize', snackboxTabletWelcome);
document.addEventListener('visibilitychange', snackboxTabletWelcome);
snackboxTabletWelcome();

// Before the screen goes dark the welcome screen goes up (resolves once it is painted), so the
// screen wakes up with it rather than with the last purchase. Still on 3 seconds later (a scan came
// in, the server kept the screen on): back to whatever full screen says
window.snackboxTabletDark = () => {
    document.documentElement.classList.add('sb-welcome');
    setTimeout(snackboxTabletWelcome, 3000);
    return new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
};

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
