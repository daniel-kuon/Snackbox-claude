// Helper function to download files from base64 data
window.downloadFile = function (filename, base64Content) {
    const link = document.createElement('a');
    link.href = 'data:application/octet-stream;base64,' + base64Content;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
