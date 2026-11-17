# Implementation Plan: Datos y consulta de habilidades (F001)

**Branch**: `002-consulta-habilidades` | **Date**: 2026-11-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-consulta-habilidades/spec.md`

## Summary

Convertir la hoja de cálculo del usuario en datos validados y consultarlos desde la aplicación.
`tools/DataImport` lee la hoja con ClosedXML, interpreta y valida las filas (falla ante cualquier
referencia que no resuelve, marca lo dudoso como "pendiente de verificar") y escribe cuatro
archivos JSON en `data/` más `docs/dudas.md`. El modelo y la lógica de búsqueda, filtros y orden
viven en `HackMaster.Domain`. `HackMaster.Web` incrusta los JSON, los carga al arrancar y ofrece
la lista de habilidades con búsqueda y filtros, las fichas de habilidad, conjunto y estilo, y la
página de dudas, con CSS propio pensado para 390 px. Playwright entra para comprobar los flujos
críticos y el ancho de pantalla.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS), SDK fijado en `global.json`

**Primary Dependencies**: Blazor WebAssembly (autónoma); ClosedXML 0.105.1 solo en
`tools/DataImport`; `System.Text.Json` (incluido en .NET). `Domain` sigue sin paquetes.

**Storage**: archivos JSON generados en `data/`, versionados e incrustados en la aplicación. Sin
almacenamiento del usuario (IndexedDB sigue en F004).

**Testing**: xUnit (`Domain.Tests`, `DataImport.Tests`), bUnit (`Web.Tests`), Playwright 1.64.1
(`Web.Tests/E2E`, a 390 px y 820 px)

**Target Platform**: navegadores modernos (WebAssembly), sitio estático en GitHub Pages; la
herramienta de importación se ejecuta en el equipo del mantenedor (Windows)

**Project Type**: aplicación web de cliente + biblioteca de dominio + herramienta de consola

**Performance Goals**: resultados de búsqueda y filtros en menos de 0,5 s con 343 elementos
(SC-003); importación completa en menos de 1 min (SC-008); CI en menos de 10 min

**Constraints**: 0 avisos de construcción; sin desplazamiento horizontal a 390 px y 820 px; el
texto de manual no entra en el repositorio ni en el sitio; importación determinista; la hoja de
cálculo no está disponible en CI

**Scale/Scope**: 343 habilidades, 59 conjuntos, 5 estilos, unas 15 dudas; 8 direcciones; 1
proyecto de pruebas nuevo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F001 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Todos los datos salen de la hoja. Lo ausente o no interpretable queda nulo y marcado "pendiente de verificar" (`pending`, `raw`). La columna de texto de manual no se lee; una prueba lo comprueba sobre los datos versionados. | PASA |
| II. Prioridad de fuentes | F001 no decide entre fuentes: muestra lo que hay en la hoja y registra las discrepancias como dudas en `docs/dudas.md`, generado desde la pestaña "Dudas". | PASA |
| III. Dominio puro y arquitectura mínima | `Domain` sin paquetes: tipos del modelo, `TextNormalizer` y `SkillQuery`. Sin interfaces ni capas nuevas; `GameData` es una clase concreta. Se añade un proyecto de pruebas (ver Complexity Tracking). | PASA con adición justificada |
| IV. Test primero | Pruebas antes del código en normalización, requisitos, validación del importador, búsqueda y filtros; nombre = regla + fuente (`FR-xxx` o libro). | PASA |
| V. Local primero | Sin backend. La aplicación lee JSON validado e incrustado, nunca la hoja. Los JSON llevan `schemaVersion`. | PASA |
| VI. Entrega verificable | Cada historia tiene verificación con comandos (`quickstart.md`); bucles de 8 iteraciones; rama, commits pequeños, pull request y revisión independiente. | PASA |
| VII. Coste y seguridad | Sin Azure. La ruta de la hoja está en una variable de entorno, no en el repositorio. Sin secretos. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright (entra ahora), GitHub Pages, castellano. La PWA sigue aplazada (como tarde F004). | PASA con aplazamiento ya justificado en F000 |

Re-evaluación tras el diseño (Phase 1): sin cambios. El modelo no introduce abstracciones y el
contrato de la herramienta refuerza los principios I y V.

## Project Structure

### Documentation (this feature)

```text
specs/002-consulta-habilidades/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── data-import-cli.md
│   └── rutas.md
├── checklists/requirements.md
└── tasks.md                      (lo crea /speckit-tasks)
```

### Source Code (repository root)

```text
data/                                   JSON generado; no se edita a mano
├── skills.json
├── suites.json
├── styles.json
└── doubts.json
docs/
└── dudas.md                            generado
src/
├── HackMaster.Domain/
│   ├── Skill.cs, SkillSuite.cs, CombatStyle.cs, Doubt.cs
│   ├── Prerequisite.cs, BookRef.cs, Ability.cs
│   ├── TextNormalizer.cs               sin acentos ni mayúsculas; slugs
│   └── SkillQuery.cs                   SkillCriteria, SkillMatch, SkillFacets
└── HackMaster.Web/
    ├── GameData.cs                     carga de los JSON incrustados e índices
    ├── Pages/
    │   ├── Home.razor                  enlaces a las secciones
    │   ├── Skills.razor, SkillDetail.razor
    │   ├── Suites.razor, SuiteDetail.razor
    │   ├── Styles.razor, StyleDetail.razor
    │   └── Doubts.razor
    ├── Components/
    │   ├── SkillFilters.razor
    │   ├── PrerequisiteView.razor      segmentos con enlaces
    │   ├── PendingMark.razor
    │   └── DoubtNotice.razor
    ├── Layout/MainLayout.razor         navegación principal
    └── wwwroot/css/app.css
tools/
└── DataImport/
    ├── Program.cs                      argumentos, códigos de salida, resumen
    ├── WorkbookReader.cs               ClosedXML → RawRow
    ├── Importer.cs                     RawRow → modelo + errores + avisos
    ├── PrerequisiteParser.cs
    └── DataWriter.cs                   JSON y dudas.md, escritura atómica
tests/
├── HackMaster.Domain.Tests/
│   ├── TextNormalizerTests.cs, SkillQueryTests.cs
│   └── DataIntegrityTests.cs           recuentos y referencias de data/
├── HackMaster.DataImport.Tests/        NUEVO
│   ├── ImporterTests.cs, PrerequisiteParserTests.cs
│   ├── WorkbookReaderTests.cs          libros creados en memoria
│   └── DataWriterTests.cs
├── HackMaster.Web.Tests/
│   ├── SkillsPageTests.cs, SkillDetailTests.cs, …   bUnit
│   └── E2E/                            Playwright; se omiten sin HACKMASTER_E2E_URL
└── README.md                           cómo ejecutar las pruebas de interfaz
.github/workflows/ci.yml                trabajo nuevo para las pruebas de interfaz
```

**Structure Decision**: se mantiene la estructura de la parte B2 de `docs/PRODUCTO.md`. Aparecen
`data/` y `docs/dudas.md`, previstos desde F000, y un proyecto de pruebas para el importador.

## Orden de trabajo

1. **Dominio** (sin dependencias): modelo, `TextNormalizer`, `SkillQuery`.
2. **Importador**: analizador de requisitos, interpretación y validación, lectura y escritura.
3. **Primera importación real**: el usuario ejecuta la herramienta, corrige en la hoja los nombres
   que no resuelven y pasa a la pestaña "Dudas" las dudas de `docs/PRODUCTO.md`; se versiona
   `data/`. Hasta entonces la interfaz se desarrolla contra datos de prueba pequeños.
4. **Aplicación**: `GameData`, lista con búsqueda y filtros (historia 1), fichas (2), conjuntos y
   estilos (3), dudas (4).
5. **Playwright y CI**, revisión independiente y pull request.

Los pasos 1 y 2 no dependen de la hoja real; el paso 3 depende del usuario.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| La hoja real tiene más irregularidades que las observadas | El importador informa de todos los errores a la vez; cada patrón nuevo se añade con su prueba |
| La normalización de texto se comporta distinto en WebAssembly | Prueba Playwright de la búsqueda sin tildes (research §7) |
| Playwright alarga CI por encima de 10 min | Trabajo separado, solo Chromium; se mide en el pull request |
| Los JSON incrustados engordan la descarga inicial | Se mide el tamaño; límite de revisión en 1 MB (research §5) |
| El texto de manual acaba en el repositorio | El lector no accede a esa columna; prueba de integridad; `*.xlsx` en `.gitignore` |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Proyecto nuevo `tests/HackMaster.DataImport.Tests` (F000 dejó cuatro proyectos más la herramienta) | El importador concentra las reglas de F001 y el principio IV exige probarlas antes de escribirlas | Probarlo desde `Domain.Tests` haría depender las pruebas del dominio de ClosedXML y de la herramienta; no probarlo incumple el principio IV. No es un proyecto de producción |
| Paquete nuevo ClosedXML | Leer `.xlsx` no es viable a mano con garantías | ExcelDataReader no escribe, y las pruebas necesitan crear libros en memoria porque los `.xlsx` no se versionan |
| Paquete nuevo Microsoft.Playwright | La constitución lo exige para los flujos críticos y es la única forma de comprobar el ancho de pantalla | bUnit no dibuja la página ni mide desbordamientos |
