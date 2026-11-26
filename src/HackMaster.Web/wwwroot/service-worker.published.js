// Trabajador de servicio publicado (F013, specs/011-instalable-sin-conexion/contracts/pwa.md).
// Parte de la plantilla de Blazor WebAssembly (https://aka.ms/blazor-offline-considerations) con tres
// cambios: la ruta base sale del ámbito (GitHub Pages sirve en /HackMasterCompanion/), solo responde a
// GET del propio sitio (la API de F006 y Entra van directas a la red) y guarda todos los archivos.

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall()));
self.addEventListener('activate', event => event.waitUntil(onActivate()));
self.addEventListener('message', onMessage);
self.addEventListener('fetch', onFetch);

const cacheNamePrefix = 'hackmaster-offline-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
// Se guarda todo lo publicado (aclaración de 2026-11-26: también los dados 3D y los sonidos), salvo
// el propio trabajador y las copias comprimidas, que el navegador nunca pide por su nombre.
const offlineAssetsExclude = [/^service-worker\.js$/, /\.br$/, /\.gz$/];
const scope = self.registration.scope;
const assets = self.assetsManifest.assets
    .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)));
const assetUrls = new Set(assets.map(asset => new URL(asset.url, scope).href));

async function onInstall() {
    // Todo o nada: si un archivo falla, la caché se borra y se reintenta en la próxima visita.
    const requests = assets.map(asset => new Request(new URL(asset.url, scope), { integrity: asset.hash, cache: 'no-cache' }));
    const cache = await caches.open(cacheName);
    try {
        await cache.addAll(requests);
    } catch (error) {
        await caches.delete(cacheName);
        throw error;
    }
}

async function onActivate() {
    // Borra las copias de otras versiones (FR-007).
    const keys = await caches.keys();
    await Promise.all(keys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

function onMessage(event) {
    // «Actualizar» (FR-008): la versión que espera pasa a mandar; cada pestaña se recarga al verlo.
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
}

function onFetch(event) {
    const request = event.request;
    // Solo GET del propio sitio y dentro del ámbito. La API de F006, Entra y cualquier otro sitio son de
    // otro origen y van a la red sin pasar por aquí (FR-005).
    if (request.method !== 'GET' || !request.url.startsWith(scope)) {
        return;
    }

    event.respondWith(fromCache(request));
}

async function fromCache(request) {
    // Una dirección interna (/habilidades/…, /fichas/…) no es un archivo: se responde con index.html,
    // como hace 404.html en GitHub Pages (FR-004). También authentication/…: la vuelta del inicio de
    // sesión la procesa la aplicación en el dispositivo y no se guarda nada; con el index.html guardado
    // es la misma versión que el resto de archivos (FR-005, FR-007).
    const url = new URL(request.url);
    url.hash = '';
    const isPage = request.mode === 'navigate' && !assetUrls.has(url.href);
    const cache = await caches.open(cacheName);
    const cached = await cache.match(isPage ? new URL('index.html', scope).href : request);
    return cached || fetch(request);
}
