// Minimal service worker for PWA installability.
// Blazor Server needs a live SignalR connection, so nothing is cached -
// every request goes straight to the network.
self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', event => event.waitUntil(self.clients.claim()));
self.addEventListener('fetch', () => { /* network passthrough */ });
