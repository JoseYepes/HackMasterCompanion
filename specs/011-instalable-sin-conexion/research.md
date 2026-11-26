# Research: Aplicación instalable y sin conexión (F013)

## R1. Trabajador de servicio: plantilla de Blazor, sin paquetes

**Decisión**: usar el mecanismo del SDK (`<ServiceWorkerAssetsManifest>` y el elemento
`<ServiceWorker … PublishedContent=…>`, como la plantilla `blazorwasm --pwa` de .NET 10). El SDK
genera al publicar `service-worker-assets.js` con cada archivo, su resumen SHA-256 y una versión.

**Por qué**: es la base que indica `PRODUCTO.md` B1 («Blazor WebAssembly PWA»); Workbox u otros
serían una dependencia nueva sin necesidad.

**Cambios sobre la plantilla**:
- Ruta base: la plantilla fija `const base = "/"`. Aquí sale de `self.registration.scope`, así sirve en
  local y en `/HackMasterCompanion/` sin reescribir el trabajador.
- Qué se guarda: todos los archivos del manifiesto de recursos (FR-003, aclaración: 3D y sonidos
  incluidos) salvo el propio `service-worker.js` y las copias comprimidas (`.br`, `.gz`). La lista de
  extensiones de la plantilla dejaba fuera `.woff2` y `.ogg`.
- Qué se responde: solo GET del mismo origen y dentro del ámbito. Las peticiones a la API de F006 y a
  Entra son de otro origen: el trabajador no las toca. `authentication/…` es una navegación más y
  recibe el `index.html` guardado, como en la plantilla: la vuelta del inicio de sesión la procesa la
  aplicación en el dispositivo y no se guarda nada. Al principio iba a la red, pero la revisión
  independiente vio que, con una versión nueva publicada, traía el `index.html` nuevo bajo el
  trabajador viejo y mezclaba versiones (FR-005, FR-007).
- Navegación: cualquier dirección interna que no sea un archivo se responde con `index.html` de la
  caché (FR-004), igual que hace `404.html` en GitHub Pages.
- Instalación atómica: `cache.addAll` falla entero si falla un archivo; una caché a medias se borra y
  se reintenta en la próxima visita (caso límite de la spec).

## R2. `index.html` cambia después de publicar

`pages.yml` reescribe `<base href>` tras `dotnet publish`, así que el resumen de `index.html` en
`service-worker-assets.js` deja de coincidir y la instalación fallaría por integridad.

**Decisión**: un script `tools/pages/apply-base-path.sh` hace todo lo que hacía `pages.yml` (ruta
base, borrar `index.html.br/.gz`, copiar a `404.html`) y además recalcula el resumen de `index.html`
en `service-worker-assets.js`. Lo usan `pages.yml` y el trabajo de CI del sitio publicado, así la
prueba ve exactamente lo que se publica. **Descartado**: saltar la comprobación de integridad de
`index.html` (deja pasar una copia rota).

## R3. Versiones nuevas (aclaración 1)

El trabajador nuevo se instala solo (el navegador lo comprueba al abrir la página, y `pwa.js` también
al volver a la pestaña) y se queda **esperando**: no se mezcla con la página abierta. `pwa.js` avisa
a `UpdateNotice` cuando hay uno esperando y ya había otro al mando. «Actualizar» le manda
`SKIP_WAITING`; al cambiar de trabajador (`controllerchange`) cada pestaña se recarga, así ninguna se
queda con archivos de dos versiones. Sin pulsar, el navegador activa el nuevo cuando se cierran todas
las ventanas (la próxima vez que se abre). Al activarse se borran las cachés de versiones viejas. Los
datos del usuario están en localStorage e IndexedDB, que el trabajador no toca (FR-009).

## R4. Instalar (aclaración 2)

Chrome y Edge lanzan `beforeinstallprompt` si la aplicación es instalable; `pwa.js` lo guarda
(`preventDefault`) y el botón «Instalar» llama a `prompt()`. Safari de iOS no tiene ese evento: se
detecta iPhone/iPad (incluido el iPad que se presenta como Mac) y se muestra la nota. Abierta como
aplicación (`display-mode: standalone` o `navigator.standalone`) no se muestra nada.

## R5. Icono (aclaración 4)

Dibujo propio: silueta de un d20 (hexágono con sus caras triangulares) dorado `#c9a24a` con el número
20, sobre fondo granate `#5a1f1b` a sangre, con el dibujo dentro del 80 % central para que sirva
también como icono «maskable» (Android lo recorta en círculo). Se genera con
`tools/Icons/New-AppIcons.ps1` (System.Drawing de Windows PowerShell, sin descargas) a 192 y 512 px.

## R6. Pruebas del sitio publicado

Las pruebas de navegador actuales usan el servidor de desarrollo, donde el trabajador no guarda nada
(FR-011). Para F013 hace falta el sitio publicado con su ruta base. **Decisión**: un servidor de
archivos mínimo en las pruebas (`HttpListener`, incluido en .NET) que sirve la carpeta publicada en
`/HackMasterCompanion/`, responde `404.html` con estado 404 como GitHub Pages, pone los tipos de
contenido correctos y apunta cada petición recibida (para comprobar qué llega a la red). El
navegador se pone sin conexión con Playwright (`SetOfflineAsync`). La instalabilidad se comprueba con
`Page.getInstallabilityErrors` del protocolo de depuración de Chromium. Para «dos versiones» la prueba
copia el sitio a una carpeta temporal y cambia la versión de `service-worker-assets.js`.
**Descartado**: `python -m http.server` (sin 404 como Pages, tipos de contenido dudosos).

## R7. Iniciar sesión sin conexión (FR-006)

`LoginDisplay` pregunta a `pwa.js` si no hay conexión (`isOffline`, `navigator.onLine`) antes de ir a
`authentication/login`; sin conexión muestra «Sin conexión: para iniciar sesión hace falta conexión.»
Si el script no está (pruebas), se considera que hay conexión y se sigue como hasta ahora.
