const CACHE_NAME = 'novacart-cache-v1';
const ASSETS = [
  '/',
  '/manifest.json',
  '/icon.svg',
  '/en/offline',
  '/zh/offline'
];

self.addEventListener('install', (e) => {
  self.skipWaiting();
  e.waitUntil(
    caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))
  );
});

self.addEventListener('activate', (e) => {
  e.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (e) => {
  // Only handle GET requests and local assets
  if (e.request.method !== 'GET' || !e.request.url.startsWith(self.location.origin)) {
    return;
  }

  // Never cache API calls — always go to network for fresh JSON data
  if (e.request.url.includes('/api/')) {
    return;
  }

  e.respondWith(
    caches.match(e.request).then((cachedResponse) => {
      return cachedResponse || fetch(e.request).then((networkResponse) => {
        // Cache static files
        if (e.request.url.match(/\.(js|css|png|jpg|jpeg|svg|woff2|json)$/)) {
          const clone = networkResponse.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(e.request, clone));
        }
        return networkResponse;
      }).catch(() => {
        if (e.request.mode === 'navigate') {
          return caches.match('/en/offline').then((offline) =>
            offline || caches.match('/zh/offline')
          );
        }
        return cachedResponse;
      });
    })
  );
});
