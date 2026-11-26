# Tasks: Aplicación instalable y sin conexión (F013)

**Input**: `spec.md`, `plan.md`, `research.md`, `contracts/pwa.md`, `quickstart.md`

Las pruebas van **antes** del código y se ven fallar.

## Fase 1: Preparación

- [x] T001 Script de ruta base `tools/pages/apply-base-path.sh` (base href, quitar `index.html.br/.gz`, recalcular el resumen de `index.html` en `service-worker-assets.js` si existe, copiar a `404.html`) y usarlo en `.github/workflows/pages.yml` en lugar de los pasos sueltos (research R2).
- [x] T002 Iconos: `tools/Icons/New-AppIcons.ps1` y `src/HackMaster.Web/wwwroot/icon-192.png`, `icon-512.png` (research R5).

## Fase 2: Instalable (US2, P1)

- [x] T003 [US2] Pruebas `tests/HackMaster.Web.Tests/PwaFilesTests.cs`: la descripción de la aplicación tiene nombre, nombre corto, `start_url`/`scope`/`id` relativos (`./`), `display: standalone`, `lang: es`, colores del modo normal (`theme_color` igual que `index.html`), iconos de 192 y 512 px que existen y miden eso, uno `maskable`; `index.html` enlaza el manifiesto, el icono de Apple y `js/pwa.js` antes de Blazor; el proyecto declara el trabajador de servicio. Ver fallar.
- [x] T004 [US2] `wwwroot/manifest.webmanifest`, enlaces en `index.html`, `ServiceWorkerAssetsManifest` y `ServiceWorker` en el `.csproj`, `service-worker.js` de desarrollo que no guarda nada (FR-001, FR-002, FR-011). T003 en verde.
- [x] T005 [US2] Pruebas bUnit `PwaComponentsTests.cs` de `InstallPrompt`: botón «Instalar» con `canInstall` (llama a `hackmasterPwa.install`), nota de iPhone con `isIos`, nada si `isStandalone` o sin script. Ver fallar.
- [x] T006 [US2] `js/pwa.js` (registro, estado, instalar; contracts/pwa.md) y `Components/InstallPrompt.razor` en `Pages/Home.razor` (FR-010). T005 en verde.

## Fase 3: Sin conexión (US1, P1)

- [x] T007 [US1] Servidor de pruebas y atributo en `Published/PublishedSite.cs`; pruebas `Published/OfflineTests.cs`: tras la primera visita, a 390 y 820 px, todas las pantallas (SC-001) abren sin conexión y recargando, sin desbordamiento; el módulo 3D, las letras y un sonido están en la caché (FR-003); la caché no tiene `authentication/` ni otros orígenes y `authentication/login` llega al servidor (SC-004); sin errores de instalabilidad (SC-003). Ver fallar.
- [x] T008 [US1] `wwwroot/service-worker.published.js` (contracts/pwa.md, FR-003 a FR-005, FR-013). T007 en verde.
- [x] T009 [US1] Prueba bUnit: `LoginDisplay` sin conexión muestra el aviso y no va al inicio de sesión (FR-006). Ver fallar; implementarlo.

## Fase 4: Versiones nuevas (US3, P2)

- [x] T010 [US3] Pruebas bUnit de `UpdateNotice` (aviso con `updateReady`, «Actualizar» llama a `hackmasterPwa.applyUpdate`) y prueba `Published/OfflineTests.cs` con dos versiones: sale el aviso, «Actualizar» carga la nueva, se borra la caché vieja y lo guardado sigue igual (SC-005), sin desbordamiento a 390 px. Ver fallar.
- [x] T011 [US3] `Components/UpdateNotice.razor` en `Layout/MainLayout.razor`, `SKIP_WAITING` y limpieza en el trabajador, recarga en `pwa.js` (FR-007 a FR-009). T010 en verde.

## Fase 5: CI y cierre

- [x] T012 Trabajo de CI `published` en `.github/workflows/ci.yml`: publicar, `apply-base-path.sh`, instalar Chromium y `--filter Category=Published`.
- [x] T013 Comprobar SC-006 (lo descargado antes de verse crece < 1 %) y anotarlo; actualizar `tests/README.md`.
- [ ] T014 Revisión independiente con un subagente; corregir con prueba cada hallazgo.
- [ ] T015 `docs/ESTADO.md`, pull request y CI en verde.
- [ ] T016 Prueba del usuario en su móvil (SC-002), tras publicar.

## Dependencias

T001–T002 → Fase 2 → Fase 3 (el trabajador necesita el manifiesto y `pwa.js`) → Fase 4 → Fase 5.
