# Contrato: `js/pwa.js` y el trabajador de servicio (F013)

`js/pwa.js` es un script clásico cargado en `index.html` antes de Blazor. Expone
`window.hackmasterPwa`:

| Función | Qué hace |
|---|---|
| `subscribe(dotnetRef)` → `id` | Llama ya y en cada cambio a `dotnetRef.invokeMethodAsync('OnPwaState', estado)`. |
| `unsubscribe(id)` | Deja de avisar. |
| `install()` → `bool` | Abre el aviso de instalar del navegador; `true` si se aceptó. |
| `applyUpdate()` | Manda `{ type: 'SKIP_WAITING' }` al trabajador que espera; la página se recarga al cambiar. |
| `isOffline()` → `bool` | `true` si el navegador dice que no hay conexión (`navigator.onLine === false`). |

`estado` (se lee en C# como `PwaState`):

```json
{ "updateReady": false, "canInstall": false, "isIos": false, "isStandalone": false }
```

- `updateReady`: hay un trabajador nuevo esperando y la página ya estaba bajo uno anterior.
- `canInstall`: el navegador lanzó `beforeinstallprompt` y aún no se ha instalado.
- `isIos`: iPhone, iPad o iPod (también iPad que se presenta como Mac con pantalla táctil).
- `isStandalone`: abierta como aplicación instalada.

Registro: con la primera suscripción (la aplicación ya se ve, FR-013; la revisión vio que tras `load`
la copia competía con la descarga de Blazor), `navigator.serviceWorker.register('service-worker.js',
{ updateViaCache: 'none' })`; sin `serviceWorker` no hace nada. Al volver a la pestaña
(`visibilitychange`) llama a `registration.update()`. En `controllerchange`, si la página ya estaba
bajo un trabajador, recarga. `applyUpdate()` sin trabajador esperando (otra pestaña ya actualizó) recarga
la página.

## Trabajador publicado (`service-worker.published.js`)

- Caché `hackmaster-offline-{version}`; en `install` guarda todos los recursos del manifiesto salvo
  `service-worker.js`, `*.br` y `*.gz`, con su resumen de integridad.
- En `activate` borra las cachés `hackmaster-offline-*` de otras versiones.
- En `message` con `SKIP_WAITING` llama a `skipWaiting()`.
- En `fetch` solo responde si: método GET, mismo origen y dentro del ámbito.
  Navegación que no es un archivo (también `authentication/…`) → `index.html` de la caché; resto →
  caché o, si no está, la red.
