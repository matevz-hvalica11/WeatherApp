// Name for the cache version
var CACHE_NAME = 'weather-app-cache-v1';

// List of URLs to cache (static assets only)
var urlsToCache = [
    '/',
    '/css/site.css',
    '/js/site.js',
    // Add static images, icons, or fonts here
];

// Install event
self.addEventListener('install', function (event) {
    console.log("Service Worker Install Event Triggered");
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(function (cache) {
                console.log("Opened cache, adding files:", urlsToCache);
                return cache.addAll(urlsToCache);
            })
            .catch(error => console.error("Cache addition failed:", error))
    );
});

// Fetch Event
self.addEventListener('fetch', function (event) {
    event.respondWith(
        caches.match(event.request)
            .then(function (response) {
                // Serve from cache if available
                if (response) {
                    return response;
                }
                // Otherwise, fetch from the network
                return fetch(event.request);
            })
    );
});

// Activate event: clear out old caches
self.addEventListener('activate', function (event) {
    var cacheWhitelist = [CACHE_NAME];

    event.waitUntil(
        caches.keys().then(function (cacheNames) {
            return Promise.all(
                cacheNames.map(function (cacheName) {
                    if (!cacheWhitelist.includes(cacheName)) {
                        console.log(`Deleting old cache: ${cacheName}`);
                        return caches.delete(cacheName);
                    }
                })
            );
        })
    );
});
