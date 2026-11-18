# Implementation Plan: Creador de conjuntos y tirador 2D (F002)

**Branch**: `003-creador-conjuntos-dados` | **Date**: 2026-11-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-creador-conjuntos-dados/spec.md`

## Summary

Un tirador de fórmulas de dados y un creador de conjuntos de habilidades, los dos guardados en el
dispositivo. En `HackMaster.Domain` viven:

- el analizador de fórmulas (`DiceFormula`), con mensajes de error en castellano;
- el tirador (`DiceRoller`), con dados que penetran opcionales y el azar a través de `IRandom`;
- el calculador de conjuntos (`SuiteCalculator`), con las reglas de la hoja;
- las migraciones de lo guardado.

`HackMaster.Web` añade:

- la página `/dados`, con el historial;
- el creador `/conjuntos/nuevo` y `/conjuntos/{slug}/editar`;
- la lista combinada de conjuntos (los 36 de la hoja y los del jugador, sin distinguirlos);
- `BrowserStore`, que guarda en `localStorage` mediante `IJSRuntime`.

Una prueba recalcula los 36 conjuntos de `data/` y los compara con la hoja.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS), SDK fijado en `global.json`

**Primary Dependencies**: las de F001. Sin paquetes nuevos: `localStorage` por `IJSRuntime` y
JSON con `System.Text.Json`.

**Storage**: `localStorage` del navegador, con dos claves versionadas (`hackmaster.suites`,
`hackmaster.rolls`). IndexedDB sigue reservado para F004.

**Testing**: xUnit (`Domain.Tests`), bUnit con `IJSRuntime` simulado (`Web.Tests`), Playwright
(`Web.Tests/E2E`, a 390 px y 820 px)

**Target Platform**: navegadores modernos (WebAssembly), GitHub Pages

**Project Type**: aplicación web de cliente + biblioteca de dominio

**Performance Goals**: una tirada en menos de 10 s desde que se abre el tirador; crear y guardar
un conjunto de 4 habilidades en menos de 2 min (SC-004); recálculo del conjunto inmediato, en
menos de 100 ms

**Constraints**:

- 0 avisos de construcción.
- Sin desbordamiento horizontal a 390 px ni a 820 px.
- `Domain` determinista y sin dependencias.
- La aplicación funciona aunque `localStorage` falle.
- Fórmulas de hasta 100 dados, de 2 a 1000 caras y 100 caracteres; hasta 100 dados extra por
  penetración.

**Scale/Scope**: 3 direcciones nuevas; 36 conjuntos de referencia; historial de 100 tiradas;
decenas de conjuntos del jugador

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F002 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Las reglas salen de las fórmulas de la hoja y del MJ (penetración). Lo que depende de un dato pendiente queda pendiente. Las clases que no se pueden combinar no se deducen. Las excepciones de la hoja se registran como duda. | PASA |
| II. Prioridad de fuentes | El MJ manda sobre la hoja en la penetración. Las discrepancias internas de la hoja (dado de 4 conjuntos, 6 componentes en 2) se registran en la pestaña "Dudas". | PASA |
| III. Dominio puro y arquitectura mínima | `Domain` sin dependencias; `IRandom` es la abstracción que exige la constitución. `BrowserStore` y `PlayerSuites` son clases concretas, sin interfaces de almacenamiento: bUnit simula `IJSRuntime`. Sin proyectos nuevos. | PASA |
| IV. Test primero | TDD en el analizador, el tirador, el calculador y las migraciones; una prueba por regla, con la fuente en el nombre (`__HJ405`, `__MJ`, `__FR0xx`). | PASA |
| V. Local primero | Todo en el dispositivo, sin red. Lo guardado lleva `schemaVersion` y migraciones con prueba. | PASA |
| VI. Entrega verificable | Comandos en `quickstart.md`; bucles de 8 iteraciones; rama, commits pequeños, pull request y revisión independiente. | PASA |
| VII. Coste y seguridad | Sin Azure ni secretos. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright, GitHub Pages, castellano. | PASA |

Re-evaluación tras el diseño (Phase 1): sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/003-creador-conjuntos-dados/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/rutas.md
├── checklists/requirements.md
└── tasks.md                      (lo crea /speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── HackMaster.Domain/
│   ├── IRandom.cs
│   ├── DiceFormula.cs              DiceTerm, DiceFormulaError, Parse
│   ├── DiceRoller.cs               DieRoll, GroupRoll, RollResult; penetración
│   ├── SuiteCalculator.cs          SuiteComponentValues, SuiteCalculation
│   ├── PlayerSuite.cs              SuiteComponentRef
│   └── StoredData.cs               formatos guardados y migraciones
└── HackMaster.Web/
    ├── SystemRandom.cs             IRandom con Random.Shared
    ├── BrowserStore.cs             localStorage por IJSRuntime; avisa si falla
    ├── PlayerSuites.cs             conjuntos del jugador y lista combinada
    ├── Pages/
    │   ├── Dice.razor              /dados, con el historial
    │   ├── SuiteEditor.razor       /conjuntos/nuevo y /conjuntos/{slug}/editar
    │   ├── Suites.razor            (F001) lista combinada y "Crear conjunto"
    │   └── SuiteDetail.razor       (F001) también para los del jugador
    ├── Components/
    │   ├── RollView.razor          detalle de una tirada
    │   └── SuiteSummary.razor      valores calculados con sus marcas
    └── Layout/MainLayout.razor     enlace "Dados"
tests/
├── HackMaster.Domain.Tests/
│   ├── DiceFormulaTests.cs, DiceRollerTests.cs
│   ├── SuiteCalculatorTests.cs
│   ├── SuiteRulesMatchSheetTests.cs    los 36 de data/ (SC-001)
│   └── StoredDataTests.cs              migraciones
└── HackMaster.Web.Tests/
    ├── DicePageTests.cs, SuiteEditorTests.cs, PlayerSuitesTests.cs   bUnit
    └── E2E/DiceAndSuitesTests.cs       Playwright
```

**Structure Decision**: sin proyectos nuevos; los tipos nuevos van en los dos proyectos de
producción existentes.

## Orden de trabajo

1. **Dominio del tirador**: `IRandom`, `DiceFormula`, `DiceRoller` (historia 1).
2. **Dominio del creador**: `SuiteCalculator` y su prueba contra los 36 conjuntos de la hoja;
   `PlayerSuite` y `StoredData` con migraciones.
3. **Guardado**: `BrowserStore`, con pruebas bUnit del fallo de `localStorage`.
4. **Tirador e historial**: página `/dados` (historias 1 y 3).
5. **Creador**: `PlayerSuites`, `/conjuntos/nuevo`, edición y borrado, y la lista y ficha de F001
   con los conjuntos del jugador (historia 2).
6. **Playwright y CI**, revisión independiente y pull request.

Las partes 1 y 2 no dependen entre sí.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| Más conjuntos de la hoja de los previstos no cumplen las reglas | La prueba SC-001 lista cada diferencia; solo se excluyen las excepciones con nombre de research §1, y cualquier otra se consulta con el usuario |
| `localStorage` no está disponible (modo privado, lleno) | `BrowserStore` captura el error, avisa y sigue en memoria; prueba bUnit |
| Un conjunto guardado deja de cuadrar al reimportar los datos | Se recalcula al mostrar; los componentes que desaparecen se señalan |
| Penetración infinita (dado de 1 cara o mala suerte) | Mínimo 2 caras y como mucho 100 dados extra por dado, avisando |
| El HJ 4.05 dice otra cosa que la hoja | Las reglas están aisladas en `SuiteCalculator`, cada una con su prueba; el usuario lo verificará |

## Complexity Tracking

Sin violaciones de la constitución que justificar.
