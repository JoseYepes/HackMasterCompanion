# Implementation Plan: Base del proyecto (F000)

**Branch**: `001-base-proyecto` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-base-proyecto/spec.md`

## Summary

Crear la base mínima del repositorio: una solución .NET con dos proyectos de producción
(`HackMaster.Domain`, `HackMaster.Web`), sus dos proyectos de pruebas y el esqueleto de
`tools/DataImport`; construcción con avisos tratados como error; un flujo de CI que construye,
prueba y comprueba formato en cada pull request; y un flujo que publica la aplicación vacía en
GitHub Pages bajo `/HackMasterCompanion/`. Sin datos de juego, sin persistencia y sin PWA todavía.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS vigente), SDK fijado en `global.json`

**Primary Dependencies**: Blazor WebAssembly (aplicación autónoma, sin servidor). Ninguna
dependencia externa en `Domain`.

**Storage**: N/A en F000 (IndexedDB se decide en F004 con un ADR)

**Testing**: xUnit (`Domain.Tests`), bUnit sobre xUnit (`Web.Tests`). Playwright queda fuera de F000.

**Target Platform**: navegadores modernos (WASM), alojado como sitio estático en GitHub Pages

**Project Type**: aplicación web de cliente (SPA) más biblioteca de dominio y herramienta de consola

**Performance Goals**: CI con veredicto en < 10 min; publicación visible en < 15 min (SC-003, SC-004)

**Constraints**: 0 avisos de construcción; ruta base `/HackMasterCompanion/` definida en un único
lugar; versiones de SDK, paquetes y acciones fijadas; repositorio público sin secretos ni
documentos fuente

**Scale/Scope**: 5 proyectos, 1 página inicial más la de "no encontrada", 3 pruebas, 2 flujos de GitHub Actions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F000 | Resultado |
|---|---|---|
| I. Contenido sin inventar | F000 no incluye datos ni reglas de juego. Las pruebas de ejemplo comprueban el nombre de la aplicación, no reglas. | PASA |
| II. Prioridad de fuentes | No aplica: no se importan datos. | PASA |
| III. Dominio puro y arquitectura mínima | `Domain` sin referencias de paquete; dos proyectos de producción más `tools/DataImport`; sin capas ni interfaces nuevas. | PASA |
| IV. Test primero | Las dos pruebas de ejemplo se escriben antes que el código que comprueban y se ven fallar. | PASA |
| V. Local primero | Sitio estático, sin backend. `DataImport` es solo un esqueleto. | PASA |
| VI. Entrega verificable | Cada grupo de tareas lleva objetivo, verificación y tope de 8 iteraciones; rama, commits pequeños y pull request. Excepción documentada: commit inicial en `main` (un repositorio vacío no admite pull request). | PASA con excepción justificada |
| VII. Coste y seguridad | Sin Azure. GitHub Actions y Pages son gratuitos en repositorios públicos. Sin secretos. | PASA |
| Restricciones técnicas | .NET 10 LTS, C#, Blazor WASM, xUnit + bUnit, GitHub Pages, castellano. PWA y Playwright se difieren (ver Complexity Tracking). | PASA con aplazamiento justificado |

Re-evaluación tras el diseño (Phase 1): sin cambios; no aparecen violaciones nuevas.

## Project Structure

### Documentation (this feature)

```text
specs/001-base-proyecto/
├── plan.md
├── research.md
├── quickstart.md
├── checklists/requirements.md
└── tasks.md
```

No se generan `data-model.md` ni `contracts/`: F000 no tiene entidades ni interfaces externas.

### Source Code (repository root)

```text
HackMaster.slnx
global.json
Directory.Build.props
Directory.Packages.props
.editorconfig
.gitignore
README.md
src/
├── HackMaster.Domain/          biblioteca; AppInfo.cs
└── HackMaster.Web/             Blazor WASM; Pages/Home.razor, wwwroot/index.html
tests/
├── HackMaster.Domain.Tests/    xUnit; AppInfoTests.cs
└── HackMaster.Web.Tests/       bUnit; HomePageTests.cs
tools/
└── DataImport/                 consola; Program.cs (esqueleto)
.github/workflows/
├── ci.yml                      build + test + format + publicación de prueba; en pull request y reutilizable
└── pages.yml                   llama a ci.yml y publica en GitHub Pages desde main
```

**Structure Decision**: la estructura de la parte B2 de `docs/PRODUCTO.md`, sin añadidos. `data/`,
`docs/adr/` y `docs/dudas.md` se crean cuando tengan contenido (F001 en adelante).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| PWA aplazada (las restricciones técnicas dicen "Blazor WebAssembly como PWA") | El service worker cachea recursos y complica verificar la primera publicación y la ruta base; F000 no necesita instalación ni modo sin conexión | Activarla ya obligaría a probar caché y actualización de versiones sin tener todavía nada que usar sin conexión. Se añade en la primera funcionalidad que lo requiera (como tarde F004) |
| Playwright aplazado | No hay flujos de interfaz que probar en una página vacía | Instalar navegadores en CI ahora solo añadiría minutos y mantenimiento |
| Commit inicial directo en `main` | GitHub no permite pull request contra un repositorio sin commits | No existe alternativa; el commit inicial contiene solo documentación y Spec Kit, sin código |
