---

description: "Task list for F008 — Dados personalizables y sonido"
---

# Tasks: Dados personalizables y sonido (F008)

**Input**: Design documents from `/specs/006-dados-personalizables/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dice3d-look.md, quickstart.md

**Tests**: incluidas. Principio IV: cada prueba se escribe y se ve fallar antes de implementar,
también las de navegador (lección de F007). Nombre = regla + fuente.

**Organization**: por historia de usuario; cada fase es un bucle con objetivo, verificación,
restricciones y un límite de 8 iteraciones (`CLAUDE.md`).

**Verificación común**: `dotnet build -warnaserror`, `dotnet test --no-build`,
`dotnet format --verify-no-changes`; en las fases de 3D, las pruebas `E2E` con la aplicación en
marcha, reiniciándola tras cada compilación.

**Restricciones comunes**:

- No borrar ni debilitar pruebas.
- `Domain` sin dependencias.
- El resultado lo decide solo el tirador (FR-001 de F003): ni el aspecto ni el sonido ni los
  efectos cambian qué cara queda arriba.
- Sin dependencias ni descargas nuevas.
- Diseños propios (FR-015).
- Interfaz en castellano.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario de `spec.md` (US1…US5)

---

## Phase 1: Foundational (dominio y guardado)

**Objetivo**: `DiceLook`, `Parent` y el historial versión 3, probados.
**Verificación**: verificación común.
**Restricciones**: tipos inmutables; funciones puras.

### Tests (primero)

- [X] T001 [P] Escribir `tests/HackMaster.Domain.Tests/DiceLookTests.cs` (data-model):
  - `DiceLook.Default` es marfil `#f4efe6`, números `#1f2328`, explosión, sonido activado y calidad alta;
  - `ForMaterial` da el color y los números por defecto de cada uno de los 11 materiales y conserva efecto, sonido y calidad;
  - `InkContrast` con la fórmula de WCAG (blanco sobre negro = 21);
  - `ReadableInk` elige el claro o el oscuro, el que contraste más;
  - los 11 colores por defecto tienen números con contraste ≥ 3 (FR-003)
- [X] T002 [P] Añadir a `tests/HackMaster.Domain.Tests/DiceBoardTests.cs`: `Parent` de cada dado extra es el índice del dado que penetró (también en olas encadenadas y en un d100: el d10 de las unidades); nulo en los demás
- [X] T003 [P] Añadir a `tests/HackMaster.Domain.Tests/StoredDataTests.cs`:
  - un historial versión 2 se lee con todas sus tiradas y `Look = DiceLook.Default` (y uno de la versión 1, encadenando);
  - lo escrito lleva `schemaVersion: 3` y `look`;
  - un `look` con material desconocido o color roto se completa con los valores por defecto sin perder tiradas;
  - una versión 4 se rechaza con motivo

### Implementation

- [X] T004 Crear `src/HackMaster.Domain/DiceLook.cs` hasta pasar T001
- [X] T005 Añadir `Parent` a `BoardDie` en `src/HackMaster.Domain/DiceBoard.cs` hasta pasar T002; revisar que las pruebas de F003 siguen en verde
- [X] T006 `StoredData.RollsVersion` = 3, `Look` en `RollHistoryFile` y migración 2 → 3 en `src/HackMaster.Domain/StoredData.cs` hasta pasar T003

---

## Phase 2: User Story 2 - Aristas redondeadas (Priority: P1)

**Goal**: todas las formas redondeadas, con la cara de arriba garantizada.

**Independent Test**: tirar un d20 y un d6 y verlos redondeados con la cara correcta.

### Tests (primero)

- [X] T007 [US2] Ampliar `tests/HackMaster.Web.Tests/E2E/Dice3DTests.cs`: en las 1 000 tiradas, además, la cara de arriba de la malla redondeada está a la misma distancia del centro que la del poliedro (`plane`, contrato) y la malla es redondeada (más vértices que el poliedro); verla fallar

### Implementation

- [X] T008 [US2] En `src/HackMaster.Web/wwwroot/js/dice3d.js`: malla redondeada por suma de Minkowski con los planos de las caras en su sitio (research §1) y números en láminas sobre cada cara (también los tres del d4), hasta pasar T007 y las pruebas de F003

---

## Phase 3: User Story 1 - Elegir el aspecto (Priority: P1) 🎯 MVP

**Goal**: 11 materiales, colores, aviso de contraste y vista previa; se recuerda.

**Independent Test**: elegir mármol con números claros, verlo en la vista previa y tirar.

### Tests (primero)

- [X] T009 [P] [US1] Escribir `tests/HackMaster.Web.Tests/DiceLookPanelTests.cs` (bUnit, JavaScript simulado):
  - el panel muestra los 11 materiales con su nombre en castellano y el actual marcado;
  - elegir un material llama a `preview` con su aspecto y lo guarda en el historial;
  - cambiar los colores actualiza la vista previa;
  - con contraste < 3, aviso y botón que pone los números legibles (FR-003);
  - sin 3D, el panel no se muestra;
  - los controles tienen nombres accesibles
- [X] T010 [P] [US1] Escribir `tests/HackMaster.Web.Tests/E2E/DiceLookTests.cs`: con un aspecto guardado, la tirada dibuja los dados con ese material (`shownDice`), y tras recargar sigue igual; sin desbordamiento a 390 y 820 px con el panel abierto; botones de 44 px

### Implementation

- [X] T011 [US1] En `dice3d.js`: los 11 materiales generados con código, entorno de reflejos, colores elegidos, `preview`/`stopPreview` (contrato) y `material` en `shownDice`
- [X] T012 [US1] Crear `src/HackMaster.Web/Components/DiceLookPanel.razor` y conectarlo en `src/HackMaster.Web/Pages/Dice.razor` y `DiceBoardView.razor` (pasa `look` y `parent` a `roll`), con estilos en `app.css` usando las variables de F007, hasta pasar T009 y T010

**Checkpoint**: los dados se ven con el aspecto elegido.

---

## Phase 4: User Story 5 - Efecto al penetrar (Priority: P2)

**Goal**: explosión, fuego, sangre o ninguno, en el dado padre; aura en los extra.

### Tests (primero)

- [X] T013 [US5] Añadir a `E2E/DiceLookTests.cs`: con una tirada que penetra, `shownEffects` tiene el efecto elegido con el índice del padre; los dados extra tienen `aura`; con «ninguno» no hay efecto pero sí aura; con movimiento reducido o calidad baja, efecto simplificado; la tirada termina en menos de 4 s (FR-017, FR-018)

### Implementation

- [X] T014 [US5] En `dice3d.js`: aura y sistemas de partículas de explosión, fuego y sangre (research §4), hasta pasar T013; el efecto se elige en el panel (`DiceLookPanel`)

---

## Phase 5: User Story 3 - Sonido (Priority: P2)

### Tests (primero)

- [X] T015 [US3] Añadir a `E2E/DiceLookTests.cs`: con sonido, `heardSounds` tiene golpes con volumen según la fuerza, como mucho 4 cada 50 ms y del tipo del material; con silencio, ninguno; si el audio falla, la tirada termina igual; el efecto de penetración suena (FR-008 a FR-010)

### Implementation

- [X] T016 [US3] En `dice3d.js`: choques grabados en la simulación, síntesis con Web Audio por material y por efecto, límite de golpes, `unlockAudio` y `heardSounds`; botón de silencio junto a la mesa y en el panel, hasta pasar T015 (el silencio está en el panel; no hizo falta otro botón junto a la mesa)

---

## Phase 6: User Story 4 - Calidad baja (Priority: P2)

### Tests (primero)

- [X] T017 [US4] Añadir a `E2E/DiceLookTests.cs`: con calidad baja, los dados no usan texturas, reflejos ni transmisión (`shownDice`) y el resultado y la duración no cambian (FR-011)

### Implementation

- [X] T018 [US4] En `dice3d.js` y el panel: calidad baja con color liso, hasta pasar T017

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T019 Medir que sin 3D la descarga no crece (SC-005) y anotarlo en `research.md` §8
- [X] T020 [P] Actualizar `tests/README.md`, `specs/004-dados-3d/contracts/dice3d-js.md` (enlace al contrato nuevo) y `docs/ESTADO.md`
- [ ] T021 Revisión independiente del diff (subagente con contexto limpio); corregir lo que encuentre
- [ ] T022 Pull request; tras fusionar, comprobar el sitio publicado
- [ ] T023 Comprobación del usuario en su móvil: fluidez en calidad alta y si le gustan los dados (SC-003, SC-006)

---

## Dependencies & Execution Order

- Phase 1 bloquea al resto.
- US2 (redondeo) antes que US1: los materiales se aplican a la malla redondeada.
- US5, US3 y US4 dependen de US1 (el aspecto llega al módulo) y entre ellas solo comparten
  `dice3d.js`, así que van en orden.
- Polish al final; T022 y T023 tras fusionar.

## Implementation Strategy

MVP = Phases 1–3: dados redondeados con el material y los colores elegidos. Después efectos,
sonido y calidad baja, cada uno comprobable por separado.

## Coverage (FR → tareas)

| Requisito | Tareas |
|---|---|
| FR-001, FR-002, FR-005 | T001, T009, T011, T012 |
| FR-003 | T001, T009 |
| FR-004 | T004, T012 (un aspecto para todos) |
| FR-006 | T007, T008 |
| FR-007, FR-017, FR-018 | T002, T013, T014 |
| FR-008, FR-009, FR-010 | T015, T016 |
| FR-011 | T017, T018 |
| FR-012 | T003, T006, T010 |
| FR-013, FR-014 | sin descargas (research §2–§3); T019 |
| FR-015 | restricción común; revisión T021 |
| FR-016 | T010, `ThemeContrastTests` existente |
| SC-001…SC-006 | T007, T010, T013, T015, T019, T023 |
