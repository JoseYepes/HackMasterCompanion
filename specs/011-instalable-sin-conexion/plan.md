# Implementation Plan: Aplicación instalable y sin conexión (F013)

**Branch**: `011-instalable-sin-conexion` | **Date**: 2026-11-26 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/011-instalable-sin-conexion/spec.md`

## Summary

Convertir la aplicación publicada en una PWA: una descripción de la aplicación
(`manifest.webmanifest`) con icono propio y un trabajador de servicio que, tras la primera visita,
guarda todos los archivos publicados y responde con ellos sin conexión. Se parte de la plantilla de
Blazor WebAssembly (`service-worker.published.js` y el manifiesto de recursos que genera el SDK), sin
paquetes nuevos, con tres cambios: la ruta base sale del ámbito del trabajador (GitHub Pages sirve en
`/HackMasterCompanion/`), solo responde a peticiones GET del propio sitio y dentro del ámbito, y deja
pasar a la red `authentication/…` (F006). Un script pequeño (`js/pwa.js`) registra el trabajador
tras la carga, detecta la versión nueva y el aviso de instalar del navegador, y se lo cuenta a dos
componentes: `UpdateNotice` (aviso «Hay una versión nueva» con «Actualizar», en el diseño) e
`InstallPrompt` (botón «Instalar» o nota de iPhone, en el inicio). `LoginDisplay` avisa sin conexión
en lugar de ir al inicio de sesión.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (LTS), JavaScript sin compilar

**Primary Dependencies**: las ya fijadas (Blazor WebAssembly, MSAL). Ninguna nueva: el trabajador de
servicio y su manifiesto de recursos los genera el SDK (`ServiceWorkerAssetsManifest`).

**Storage**: Cache Storage del navegador (una caché por versión); los datos del usuario
(localStorage, IndexedDB) no se tocan.

**Testing**: xUnit (archivos publicados: descripción, iconos, `index.html`), bUnit (componentes con
JS simulado) y Playwright contra el **sitio publicado** con su ruta base, servido por un servidor de
archivos de prueba hecho con `HttpListener` (sin dependencias), con el navegador sin conexión.

**Target Platform**: navegadores de móvil y escritorio; Chrome/Edge (instalación con aviso), Safari
de iOS (instalación manual).

**Project Type**: aplicación web (PWA Blazor WebAssembly) en GitHub Pages.

**Performance Goals**: abrir sin conexión en < 3 s en un móvil medio (A2); lo descargado antes de que
se vea la aplicación crece < 1 % (SC-006).

**Constraints**: ruta base `/HackMasterCompanion/` reescrita tras publicar (`pages.yml`); repositorio
público; no tocar `infra/`, `.github/workflows/azure.yml` ni `wwwroot/appsettings.json`.

**Scale/Scope**: unos 15 MB publicados (≈ 5 MB comprimidos), una caché por versión.

## Constitution Check

| Principio | Cumple | Nota |
|---|---|---|
| I. Contenido sin inventar | Sí | No se añade contenido de juego; `data/` no se toca. El icono es un dibujo propio. |
| II. Prioridad de fuentes | N/A | Sin datos de juego. |
| III. Dominio puro, arquitectura mínima | Sí | Nada en `Domain`. Sin interfaces nuevas: los componentes llaman a `IJSRuntime` como el resto. El script de iconos es un archivo suelto en `tools/`, no un proyecto. |
| IV. Test primero | Sí | Pruebas de archivos, bUnit y navegador escritas antes y vistas fallar. |
| V. Local primero | Sí | Es justo lo que falta para «sin conexión». |
| VI. Entrega verificable | Sí | Comandos en `quickstart.md`; trabajo nuevo de CI para el sitio publicado. |
| VII. Coste y seguridad | Sí | Sin Azure. El trabajador nunca guarda inicio de sesión, API ni otros sitios. |

## Project Structure

### Documentation (this feature)

```text
specs/011-instalable-sin-conexion/
├── plan.md
├── research.md
├── contracts/pwa.md      # pwa.js ↔ componentes y trabajador de servicio
├── quickstart.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/HackMaster.Web/
├── HackMaster.Web.csproj            # ServiceWorkerAssetsManifest + ServiceWorker
├── wwwroot/
│   ├── index.html                   # enlace al manifiesto, iconos de Apple, js/pwa.js
│   ├── manifest.webmanifest
│   ├── icon-192.png, icon-512.png   # dado de 20 caras dorado sobre granate
│   ├── service-worker.js            # desarrollo: no guarda nada (FR-011)
│   ├── service-worker.published.js  # publicado: copia por versión
│   └── js/pwa.js                    # registro, versión nueva, instalar
├── Components/UpdateNotice.razor    # en MainLayout
├── Components/InstallPrompt.razor   # en Home
└── Account/LoginDisplay.razor       # aviso sin conexión (FR-006)

tools/
├── Icons/New-AppIcons.ps1           # genera los iconos (System.Drawing de Windows)
└── pages/apply-base-path.sh         # ruta base tras publicar; lo usan pages.yml y ci.yml

tests/HackMaster.Web.Tests/
├── PwaFilesTests.cs                 # descripción, iconos, index.html (sin navegador)
├── PwaComponentsTests.cs            # bUnit: UpdateNotice, InstallPrompt, LoginDisplay
└── Published/                       # Playwright contra el sitio publicado
    ├── PublishedSite.cs             # HttpListener con la ruta base y 404.html como Pages;
    │                                # PublishedFact se omite sin HACKMASTER_PUBLISHED_DIR
    └── OfflineTests.cs              # sin conexión, versiones, instalabilidad, caché
```

**Structure Decision**: todo en `HackMaster.Web` y sus pruebas; la reescritura de la ruta base pasa
de `pages.yml` a un script compartido para que la prueba del sitio publicado use exactamente lo
mismo que la publicación real.

## Complexity Tracking

Sin violaciones.
