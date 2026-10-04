// The kiosk's Admin link stays hidden until someone shakes the mouse for a second.
// Customers scan and never see it; a remote admin, who cannot scan a card, finds it.
// Plain DOM on purpose: routing every mousemove through Blazor would re-render the page.
(() => {
    const SHAKE_MS = 1000;       // how long the shaking has to last
    const MAX_GAP_MS = 250;      // a longer pause starts over
    const MIN_REVERSALS = 3;     // left-right changes - a single bump of the mouse is not a shake
    const HIDE_AFTER_MS = 10000; // hidden again once the mouse rests this long
    const CLASS = 'show-admin-login';

    let start = 0, last = 0, lastX = null, lastDir = 0, reversals = 0, hideTimer = 0;

    const keepVisible = () => {
        clearTimeout(hideTimer);
        hideTimer = setTimeout(() => document.body.classList.remove(CLASS), HIDE_AFTER_MS);
    };

    document.addEventListener('mousemove', e => {
        const now = performance.now();

        if (document.body.classList.contains(CLASS)) {
            keepVisible();
            return;
        }

        if (now - last > MAX_GAP_MS) {
            start = now;
            reversals = 0;
            lastDir = 0;
            lastX = e.clientX;
        }
        last = now;

        const dx = e.clientX - lastX;
        lastX = e.clientX;
        if (Math.abs(dx) >= 2) {
            const dir = Math.sign(dx);
            if (lastDir !== 0 && dir !== lastDir) reversals++;
            lastDir = dir;
        }

        if (now - start >= SHAKE_MS && reversals >= MIN_REVERSALS) {
            document.body.classList.add(CLASS);
            keepVisible();
        }
    }, { passive: true });
})();
