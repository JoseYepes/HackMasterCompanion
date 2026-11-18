---

description: "Task list for F002 — Creador de conjuntos y tirador 2D"
---

# Tasks: Creador de conjuntos y tirador 2D (F002)

**Input**: Design documents from `/specs/003-creador-conjuntos-dados/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/rutas.md, quickstart.md

**Tests**: incluidas. La constitución (principio IV) exige test primero: cada prueba se escribe y
se ve fallar antes de implementar. El nombre de cada prueba es la regla más su fuente
(`Suite_Price_Is_Sum_Times_1_1_Rounded__HJ405`).

**Organization**: por historia de usuario. Cada fase es un bucle con objetivo, verificación,
restricciones y un límite de 8 iteraciones (formato de `CLAUDE.md`).

**Verificación común** de todas las fases: `dotnet build -warnaserror`, `dotnet test --no-build`,
`dotnet format --verify-no-changes`.

**Restricciones comunes**:

- No borrar ni debilitar pruebas existentes.
- No editar `data/` ni `docs/dudas.md` a mano.
- No añadir paquetes ni proyectos.
- `HackMaster.Domain` sin dependencias, y el azar solo por `IRandom`.
- Interfaz y mensajes en castellano; identificadores en inglés.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario de `spec.md` (US1…US3)

---

## Phase 1: Setup

**Objetivo**: la navegación y las direcciones nuevas existen, vacías.
**Verificación**: verificación común; `/dados` y `/conjuntos/nuevo` cargan.
**Restricciones**: sin lógica todavía.

- [X] T001 Añadir el enlace "Dados" a `src/HackMaster.Web/Layout/MainLayout.razor` y crear `src/HackMaster.Web/Pages/Dice.razor` (`@page "/dados"`, título "Dados") y `src/HackMaster.Web/Pages/SuiteEditor.razor` (`@page "/conjuntos/nuevo"` y `@page "/conjuntos/{Slug}/editar"`, título "Crear conjunto"), según `contracts/rutas.md`

---

## Phase 2: Foundational (azar y formato guardado)

**Objetivo**: `IRandom`, el formato guardado con versión y su migración, y el guardado en el
navegador, que todas las historias usan.
**Verificación**: verificación común; pasan `StoredDataTests` y `BrowserStoreTests`.
**Restricciones**: `BrowserStore` es una clase concreta, sin interfaz (research §7).

### Tests (primero; deben fallar antes de implementar)

- [X] T002 [P] Escribir `tests/HackMaster.Domain.Tests/StoredDataTests.cs`:
  - leer y escribir `hackmaster.suites` y `hackmaster.rolls` con `schemaVersion: 1` sin perder datos;
  - un JSON sin `schemaVersion` o con una versión mayor que la conocida se rechaza con motivo;
  - un JSON dañado se rechaza sin excepción;
  - (FR-018, data-model "Guardado")
- [X] T003 [P] Escribir `tests/HackMaster.Web.Tests/BrowserStoreTests.cs` con el `JSInterop` de bUnit:
  - guarda y lee cada clave;
  - si `localStorage.setItem` o `getItem` fallan, no lanza, marca `IsAvailable = false` y conserva los datos en memoria (FR-019)

### Implementation

- [X] T004 [P] Crear `src/HackMaster.Domain/IRandom.cs` (`int Next(int minInclusive, int maxInclusive)`) y `src/HackMaster.Web/SystemRandom.cs` con `Random.Shared`, registrado como instancia única en `src/HackMaster.Web/Program.cs`
- [X] T005 Crear `src/HackMaster.Domain/StoredData.cs` con los formatos de `data-model.md`, `CurrentVersion = 1` y `Migrate` hasta pasar T002
- [X] T006 Crear `src/HackMaster.Web/BrowserStore.cs` (`localStorage` por `IJSRuntime`, de ámbito único) y el aviso `storage-warning` en `MainLayout.razor`, hasta pasar T003

---

## Phase 3: User Story 1 - Tirar dados con una fórmula (Priority: P1) 🎯 MVP

**Goal**: escribir una fórmula, tirar y ver el detalle; con dados que penetran opcionales.
**Independent Test**: paso 2 de `quickstart.md` (puntos 1 a 3).
**Verificación**: verificación común; pasan `DiceFormulaTests`, `DiceRollerTests` y `DicePageTests`.
**Restricciones**: el análisis y la tirada viven en `Domain`; la página solo los muestra.

### Tests (primero; deben fallar antes de implementar)

- [X] T007 [P] [US1] Escribir `tests/HackMaster.Domain.Tests/DiceFormulaTests.cs`:
  - válidas: "2d6+3", "1d20+1d6-2", "d8", "2D6 + 3", "-1+1d4", "1d100", "100d6", "1d1000";
  - no válidas, cada una con posición, trozo y ejemplo: "", "  ", "2d", "d", "3x6", "2d6+", "0d6", "2d0", "2d1", "+3", "5", "101d6", "1d1001", "1d6+10001", fórmula de 101 caracteres, "2d6++3", "2d6 3";
  - al menos 20 de cada clase (FR-001 a FR-004, SC-002)
- [X] T008 [P] [US1] Escribir `tests/HackMaster.Domain.Tests/DiceRollerTests.cs` con un `IRandom` de secuencia fija:
  - total = suma de grupos con signo más constantes (FR-005);
  - cada dado entre 1 y sus caras;
  - con penetración, el dado que saca el máximo añade otro que suma `valor − 1`, en cadena, hasta 100 extra con aviso (FR-006, `__MJ`);
  - sin penetración, nunca hay dados extra;
  - los modificadores no penetran;
  - uniformidad: 10 000 tiradas de 1d6 con `Random(20261118)`, cada cara entre el 15 % y el 18,4 % (SC-003)
- [X] T009 [P] [US1] Escribir `tests/HackMaster.Web.Tests/DicePageTests.cs` (bUnit):
  - tirar "2d6+3" muestra `dice-total` y `dice-detail`;
  - una fórmula mala muestra `dice-error` y no tira;
  - `dice-repeat` repite la fórmula (FR-007);
  - `dice-penetrating` cambia la tirada;
  - `/dados?f=2d6%2B3` rellena la fórmula

### Implementation

- [X] T010 [US1] Crear `src/HackMaster.Domain/DiceFormula.cs` (`DiceTerm`, `DiceFormulaError`, `Parse` escrito a mano, research §5) hasta pasar T007
- [X] T011 [US1] Crear `src/HackMaster.Domain/DiceRoller.cs` (`DieRoll`, `GroupRoll`, `RollResult`) hasta pasar T008
- [X] T012 [US1] Crear `src/HackMaster.Web/Components/RollView.razor` y completar `Dice.razor` (fórmula, opción de penetración, Tirar, Repetir, error y resultado) hasta pasar T009, con CSS en `src/HackMaster.Web/wwwroot/css/app.css` pensado para 390 px

---

## Phase 4: User Story 3 - Historial de tiradas (Priority: P2)

**Goal**: las últimas 100 tiradas se guardan en el dispositivo y se pueden reutilizar o borrar.
**Independent Test**: paso 2 de `quickstart.md` (punto 4).
**Verificación**: verificación común; pasan `RollHistoryTests` y las pruebas de historial de `DicePageTests`.
**Restricciones**: el historial usa `BrowserStore` y la clave `hackmaster.rolls`.

### Tests (primero; deben fallar antes de implementar)

- [X] T013 [P] [US3] Escribir `tests/HackMaster.Domain.Tests/RollHistoryTests.cs`:
  - añadir pone la tirada la primera;
  - la número 101 descarta la más antigua;
  - borrar vacía la lista (FR-016)
- [X] T014 [P] [US3] Añadir a `tests/HackMaster.Web.Tests/DicePageTests.cs`:
  - `roll-history` lista las tiradas de la más reciente a la más antigua, con fórmula, detalle, total y fecha;
  - elegir una fila (`history-row`) copia la fórmula;
  - `history-clear` pide confirmación y vacía;
  - el historial y la opción de penetración se leen de `BrowserStore` al entrar (FR-017)

### Implementation

- [X] T015 [US3] Crear `src/HackMaster.Domain/RollHistory.cs` (lista inmutable con máximo 100) hasta pasar T013
- [X] T016 [US3] Conectar el historial en `Dice.razor` con `BrowserStore` hasta pasar T014

---

## Phase 5: User Story 2 - Crear un conjunto de habilidades (Priority: P1)

**Goal**: elegir de 2 a 5 componentes, ver los valores calculados, guardar y editar; los conjuntos
del jugador aparecen con los 36.
**Independent Test**: paso 3 de `quickstart.md`.
**Verificación**: verificación común; pasan `SuiteCalculatorTests`, `SuiteRulesMatchSheetTests`,
`PlayerSuitesTests` y `SuiteEditorTests`.
**Restricciones**:

- Las reglas viven solo en `SuiteCalculator`.
- Los conjuntos del jugador no guardan valores calculados (research §8).
- `SuiteRulesMatchSheetTests` solo puede excluir las excepciones con nombre de research §1; cualquier otra diferencia se consulta con el usuario.

### Tests (primero; deben fallar antes de implementar)

- [ ] T017 [P] [US2] Escribir `tests/HackMaster.Domain.Tests/SuiteCalculatorTests.cs`, una prueba por regla:
  - PC = suma − 1 (`__HJ405`);
  - precio = redondear(suma × 1,1) con los medios alejándose de cero (`__HJ405`);
  - dificultad = mayor + 5 (`__HJ405`);
  - dado = media, mínimo 4, a la baja a un dado real, ignorando "NA" y "*" (`__HJ405`);
  - pendiente si algún dato del que depende lo está (FR-013);
  - clases: "Todas" neutro, iguales se conservan, distintas quedan pendientes (FR-012b);
  - `CanSave` solo con 2 a 5 componentes, sin repetidos ni ciclos (FR-010, FR-011);
  - un conjunto componente aporta sus valores (FR-011)
- [ ] T018 [P] [US2] Escribir `tests/HackMaster.Domain.Tests/SuiteRulesMatchSheetTests.cs`:
  - lee `data/` como `DataIntegrityTests`;
  - recalcula los 36 conjuntos de habilidades a partir de sus componentes;
  - compara coste, precio, dificultad y dado;
  - excluye solo estas excepciones, con su nombre y motivo, del valor afectado:
    - dado de Zoólogo, Escribano y Hechicero (la hoja suma 1 a la media);
    - dado de Cerrajero (escrito a mano);
    - dado de Capitán de compañía y Líder de escuadrón (Liderazgo básico y Liderazgo de pequeños grupos tienen el dado «5» pendiente, y la aplicación lo deja pendiente, FR-013);
  - cualquier otra diferencia hace fallar la prueba;
  - (SC-001, FR-015)
- [ ] T019 [P] [US2] Escribir `tests/HackMaster.Web.Tests/PlayerSuitesTests.cs` (bUnit con `JSInterop`):
  - guardar, cambiar el nombre, modificar y borrar;
  - la lista combinada incluye los 36 y los del jugador, ordenados por nombre;
  - un nombre cuyo `slug` ya existe, o los reservados "nuevo" y "oficiales", se rechaza;
  - borrar un conjunto que es componente de otro lista los afectados;
  - un componente que ya no existe en los datos se señala;
  - (FR-014)
- [ ] T020 [P] [US2] Escribir `tests/HackMaster.Web.Tests/SuiteEditorTests.cs`:
  - buscar sin acentos añade componentes (`suite-search`, `suite-component`);
  - cada componente muestra su coste, precio, dificultad y dado (US2-1);
  - añadir un componente que ya está no lo duplica y avisa (caso límite);
  - `suite-summary` se recalcula con cada cambio;
  - el requisito del conjunto dice «las de las habilidades que lo componen» y lista el requisito de cada componente (FR-012);
  - `suite-cannot-save` explica el motivo;
  - las características solo se eligen entre las de los componentes (FR-012);
  - guardar lleva a la ficha;
  - `/conjuntos/{slug}/editar` carga el conjunto y `suite-delete` pide confirmación

### Implementation

- [ ] T021 [US2] Crear `src/HackMaster.Domain/SuiteCalculator.cs` (`SuiteComponentValues`, `SuiteCalculation`) hasta pasar T017
- [ ] T022 [US2] Ajustar `SuiteCalculator` hasta pasar T018. Si aparecen diferencias no previstas, parar y consultarlas con el usuario antes de añadir excepciones
- [ ] T023 [US2] Crear `src/HackMaster.Domain/PlayerSuite.cs` (`SuiteComponentRef`) y `src/HackMaster.Web/PlayerSuites.cs` (carga desde `BrowserStore`, lista combinada, validación de nombres y ciclos) hasta pasar T019
- [ ] T024 [US2] Crear `src/HackMaster.Web/Components/SuiteSummary.razor` y completar `SuiteEditor.razor` hasta pasar T020
- [ ] T025 [US2] Mostrar los conjuntos del jugador en `src/HackMaster.Web/Pages/Suites.razor` (mismo aspecto, botón "Crear conjunto") y en `src/HackMaster.Web/Pages/SuiteDetail.razor` (valores calculados; botones Editar y Borrar solo si es del jugador), con pruebas nuevas en `tests/HackMaster.Web.Tests/SuitesPageTests.cs`, escritas primero

---

## Phase 6: Polish & Cross-Cutting Concerns

**Objetivo**: flujos críticos comprobados en navegador, documentación al día y pull request.
**Verificación**: verificación común; pasan las pruebas `E2E` con la aplicación en marcha
(`tests/README.md`); el CI está en verde en el pull request.

- [ ] T026 [P] Escribir `tests/HackMaster.Web.Tests/E2E/DiceAndSuitesTests.cs`:
  - tirar "2d6+3" y recargar conserva el historial;
  - crear con las 5 habilidades de Alquimista da 16 PC, 1403 po, dificultad 15 y d8;
  - guardar y recargar lo conserva;
  - (SC-001, SC-005)
- [ ] T027 [P] Añadir `/dados`, `/conjuntos/nuevo` y la ficha de un conjunto del jugador a `tests/HackMaster.Web.Tests/E2E/NoHorizontalOverflowTests.cs`, a 390 px y 820 px (FR-020, SC-006)
- [ ] T028 Pedir al usuario que añada a la pestaña "Dudas" de la hoja las dos dudas de research §1 (dado de 4 conjuntos; 6 componentes en Forjador de armaduras y Trovador), reimportar y versionar `data/doubts.json` y `docs/dudas.md`
- [ ] T029 Medir SC-004 en el navegador (una tirada y crear un conjunto de 4 habilidades) y anotarlo en `research.md`
- [ ] T030 Revisión independiente del diff por un subagente con contexto limpio y aplicar lo que encuentre
- [ ] T031 Actualizar `docs/ESTADO.md` y `tests/README.md`; abrir el pull request en castellano con qué se hizo, cómo se verificó y qué falta
- [ ] T032 Tras fusionar, comprobar `/dados` y el creador en el sitio publicado

---

## Dependencies & Execution Order

- **Phase 1** no depende de nada. **Phase 2** depende de la 1.
- **US1 (Phase 3)** y **US2 (Phase 5)** dependen solo de la Phase 2 y no dependen entre sí.
- **US3 (Phase 4)** depende de US1, porque el historial guarda sus tiradas.
- **Polish (Phase 6)** depende de todas las historias.

### Tareas que necesitan al usuario

- T022, si aparecen diferencias no previstas con la hoja.
- T028: la hoja solo está en su equipo.
- T031 y T032: pull request y fusión.

### Within Each User Story

Pruebas primero y vistas fallar; después dominio, servicio y página.

### Parallel Opportunities

- Phase 2: T002 y T003; T004 junto con ellas.
- Phase 3: T007, T008 y T009. Phase 5: T017, T018, T019 y T020.
- Phase 6: T026 y T027.

## Parallel Example: User Story 2

```text
T017 SuiteCalculatorTests      T018 SuiteRulesMatchSheetTests
T019 PlayerSuitesTests         T020 SuiteEditorTests
```

## Implementation Strategy

1. **MVP**: Phases 1-3, el tirador. Ya se puede usar en la mesa.
2. **Phase 5**, el creador: aporta la otra mitad de la funcionalidad.
3. **Phase 4**, el historial: mejora del tirador.
4. **Phase 6**: navegador, revisión y entrega.
