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
