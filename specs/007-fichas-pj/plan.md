# Implementation Plan: Fichas de PJ (F004)

**Branch**: `007-fichas-pj` | **Date**: 2026-11-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/007-fichas-pj/spec.md`

## Summary

Fichas de personaje guardadas en el dispositivo, con lista, editor, duplicar, borrar, exportar e
importar:

- **Dominio** (`HackMaster.Domain`): la ficha (`Character`, `CharacterSkill`, `AbilityScore`), la
  copia y la lectura, escritura y migración de los dos formatos con versión: lo guardado
  (`hackmaster.characters`) y el archivo de una ficha. La versión 1 (borrador B3, seis
  características) se migra a la 2 (con Aspecto) (research §2).
- **Guardado en IndexedDB** con un módulo JS propio de unas 40 líneas, sin paquetes, como pide
  `PRODUCTO.md` B1 (research §1).
- **Exportar** generando el archivo en el navegador; **importar** con el `InputFile` de Blazor
  (research §3).
- **Páginas** `/fichas` (lista e importar) y `/fichas/nueva` y `/fichas/{id}` (editor), con el
  estilo de F007.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS); JavaScript ES2022 en `wwwroot/js/characters.js`

**Primary Dependencies**: ninguna nueva (Blazor `InputFile` viene con el SDK).

**Storage**: IndexedDB, base `hackmaster`, almacén `files`, clave `hackmaster.characters`, con
versión de formato 2.

**Testing**: xUnit (`CharacterTests`, `CharacterStorageTests`), bUnit (`CharactersPageTests`,
`CharacterEditorTests`), Playwright (`CharacterTests` E2E: guardar y recargar, exportar e importar,
sin peticiones externas, desbordamiento).

**Target Platform**: navegadores modernos (los mismos que F001–F008).

**Project Type**: aplicación web de cliente + biblioteca de dominio

**Performance Goals**: abrir la lista o el editor en menos de 1 s con 50 fichas.

**Constraints**:
- 0 avisos al compilar; `dotnet format` limpio.
- Sin peticiones fuera del sitio (SC-003).
- Sin desbordamiento a 390 y 820 px; colores solo con las variables de F007.

**Scale/Scope**: 2 páginas, 1 servicio web (`PlayerCharacters`), 1 módulo JS, unos 4 registros de
dominio y 2 formatos con versión.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F004 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Raza, clase y alineamiento son texto libre: no hay listas en los datos y no se inventan. Las habilidades vienen de `data/`. | PASA |
| II. Prioridad de fuentes | No aplica. | PASA |
| III. Dominio puro y arquitectura mínima | Ficha, copia y formatos en `Domain`, sin dependencias. Un solo servicio web como `PlayerSuites`; sin repositorios ni interfaces. El guardado en IndexedDB es una clase concreta (`IndexedStore`), no una abstracción sobre `BrowserStore`. | PASA |
| IV. Test primero | Pruebas de dominio y de componentes antes del código; migración v1→v2 con prueba por campo. | PASA |
| V. Local primero | Todo en el dispositivo, sin servidor. Los dos formatos llevan `schemaVersion` y migración probada. | PASA |
| VI. Entrega verificable | Comandos en `quickstart.md`. | PASA |
| VII. Coste y seguridad | Sin Azure, sin secretos y sin dependencias nuevas. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright, GitHub Pages. | PASA |

Re-evaluación tras el diseño (Phase 1): sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/007-fichas-pj/
├── spec.md
├── plan.md               # este archivo
├── research.md           # IndexedDB, versiones, exportar e importar, identificadores
├── data-model.md         # Character y los dos formatos
├── quickstart.md
├── contracts/
│   └── character-file.md # archivo de ficha (exportar e importar)
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/HackMaster.Domain/
├── Character.cs              # Character, CharacterSkill, AbilityScore, Duplicate
└── StoredData.cs             # + ReadCharacters/ReadCharacterFile, Write, migración 1 → 2
src/HackMaster.Web/
├── IndexedStore.cs           # get/set en IndexedDB, con el mismo aviso que BrowserStore
├── PlayerCharacters.cs       # lista, guardar, duplicar, borrar, importar
├── Pages/Characters.razor    # /fichas
├── Pages/CharacterEditor.razor  # /fichas/nueva y /fichas/{id}
├── Layout/MainLayout.razor   # enlace «Fichas»
└── wwwroot/js/characters.js  # IndexedDB y descarga del archivo
tests/HackMaster.Domain.Tests/CharacterTests.cs, CharacterStorageTests.cs
tests/HackMaster.Web.Tests/CharactersPageTests.cs, CharacterEditorTests.cs, E2E/CharacterTests.cs
```

**Structure Decision**: la de siempre (`Domain` + `Web`); nada nuevo fuera de esos proyectos.

## Complexity Tracking

Sin desviaciones de la constitución.
