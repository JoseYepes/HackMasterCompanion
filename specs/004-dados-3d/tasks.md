---

description: "Task list for F003 — Dados 3D"
---

# Tasks: Dados 3D (F003)

**Input**: Design documents from `/specs/004-dados-3d/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dice3d-js.md, quickstart.md

**Tests**: incluidas. La constitución (principio IV) exige test primero: cada prueba se escribe y
se ve fallar antes de implementar. El nombre de cada prueba es la regla más su fuente
(`D100_Is_Drawn_As_Tens_And_Units__FR003`).

**Organization**: por historia de usuario; cada fase es un bucle con objetivo, verificación,
restricciones y un límite de 8 iteraciones (formato de `CLAUDE.md`).

**Verificación común**: `dotnet build -warnaserror`, `dotnet test --no-build`,
`dotnet format --verify-no-changes`; y, en las fases con JavaScript, las pruebas `E2E` con la
aplicación en marcha (`tests/README.md`).

**Restricciones comunes**:

- No borrar ni debilitar pruebas.
- `Domain` sin dependencias.
- El resultado lo decide solo el tirador de F002 (FR-001).
- Sin paquetes NuGet nuevos. Las únicas librerías nuevas son three.js 0.187.0 y cannon-es
  0.20.0, aprobadas por el usuario.
- Interfaz en castellano.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario de `spec.md` (US1…US3)

---

## Phase 1: Setup

**Objetivo**: librerías copiadas con su versión y licencia, sin cargarse en ninguna página.
**Verificación**: verificación común; `/habilidades` no descarga nada de `lib/` (pestaña de red).
**Restricciones**: los archivos de `lib/` no se editan.

- [X] T001 Descargar, con el permiso dado por el usuario, desde jsDelivr (npm) `three@0.187.0/build/three.module.js` (643 KB), `three@0.187.0/build/three.core.js` (1440 KB) y `cannon-es@0.20.0/dist/cannon-es.js` (338 KB), con sus `LICENSE`, en `src/HackMaster.Web/wwwroot/lib/`, más `lib/README.md` con origen, versión y licencia de cada archivo

---

## Phase 2: Foundational (dominio)

**Objetivo**: el plan de dados y el formato guardado versión 2, probados.
**Verificación**: verificación común; pasan `DiceBoardTests` y las pruebas nuevas de `StoredDataTests`.
**Restricciones**: tipos inmutables; funciones puras.

### Tests (primero; deben fallar antes de implementar)

- [X] T002 [P] Escribir `tests/HackMaster.Domain.Tests/DiceBoardTests.cs` (data-model):
  - un dado por d4, d6, d8, d10, d12 y d20, con su cara;
  - d100 como `D10Tens` y `D10`: 100 → "00" y "0", 7 → "00" y "7", 70 → "70" y "0", 10 → "10" y "0";
  - d2, d3 y d1000 van a `Unshaped`;
  - los extra por penetración van en olas sucesivas, detrás de su dado;
  - más de 20 dados al empezar (un d100 cuenta 2) → `TooMany` y nada que dibujar;
  - más de 20 con los extra → se cortan y `Truncated`;
  - los modificadores no se dibujan
- [X] T003 [P] Añadir a `tests/HackMaster.Domain.Tests/StoredDataTests.cs`:
  - un historial de la versión 1 se lee con todas sus tiradas y `dice3d = false`;
  - lo escrito lleva `schemaVersion: 2` y `dice3d`;
  - una versión 3 se rechaza con motivo;
  - (FR-009, principio V)

### Implementation

- [X] T004 Crear `src/HackMaster.Domain/DiceBoard.cs` (`DieShape`, `BoardDie`, `BoardPlan`, `Plan`) hasta pasar T002
- [X] T005 Subir `StoredData.CurrentVersion` a 2, añadir `Dice3D` a `RollHistoryFile` y la migración 1 → 2 en `Migrate` hasta pasar T003; revisar que las pruebas de F002 siguen en verde

---

## Phase 3: User Story 1 - Ver los dados rodar (Priority: P1) 🎯 MVP

**Goal**: con «Dados 3D» activado, los dados ruedan con física y se paran en la cara que manda el tirador.
**Independent Test**: paso 3 de `quickstart.md` (puntos 2 a 4).
**Verificación**: verificación común; pasan `Dice3DTests` (SC-001) y las pruebas nuevas de `DicePageTests`.
**Restricciones**: el JavaScript no decide valores; la cara se fuerza como en research §2.

### Tests (primero; deben fallar antes de implementar)

- [X] T006 [P] [US1] Escribir `tests/HackMaster.Web.Tests/E2E/Dice3DTests.cs`:
  - en la página `/dados`, importar `js/dice3d.js` y llamar 1 000 veces a `simulate(plan, semilla)` con planes de todas las formas (d4 a d20, d100 como dos d10) y caras al azar;
  - comprobar que la cara de arriba coincide siempre (SC-001)
- [X] T007 [P] [US1] Añadir a `tests/HackMaster.Web.Tests/DicePageTests.cs` (bUnit, JSInterop simulado):
  - `dice3d-toggle` desactivado al entrar la primera vez (FR-010);
  - activarlo lo guarda (FR-009);
  - con 3D, al tirar se importa el módulo y se llama a `roll` con el plan de `DiceBoard.Plan`;
  - el total aparece cuando `roll` termina (FR-005);
  - tocar `dice-board` llama a `finish` (FR-004);
  - tirar otra vez mientras ruedan llama a `finish` y empieza la tirada nueva (caso límite);
  - el historial guardado es el mismo con y sin 3D (FR-002)

### Implementation

- [X] T008 [US1] Escribir en `src/HackMaster.Web/wwwroot/js/dice3d.js`:
  - las formas de los dados con sus caras numeradas (d4, d6, d8, d10, d10 de decenas, d12, d20);
  - la física con paso fijo;
  - `simulate`, que fuerza la cara (research §2);
  - hasta pasar T006
- [X] T009 [US1] Completar `dice3d.js` con `isSupported`, `create`, `roll` (reproducción en pantalla, como mucho 4 s), `finish` y `dispose` según `contracts/dice3d-js.md`
- [X] T010 [US1] Crear `src/HackMaster.Web/Components/DiceBoardView.razor` (se llama así para no chocar con `Domain.DiceBoard`) y conectar la opción «Dados 3D» en `src/HackMaster.Web/Pages/Dice.razor` hasta pasar T007, con CSS pensado para 390 px

---

## Phase 4: User Story 2 - Modo sin 3D de respaldo (Priority: P1)

**Goal**: el tirador funciona siempre: sin WebGL, con demasiados dados o si el módulo falla.
**Independent Test**: paso 3 de `quickstart.md` (puntos 1 y 5); navegador sin WebGL.
**Verificación**: verificación común; pasan las pruebas nuevas de `DicePageTests`.
**Restricciones**: sin 3D, el tirador se comporta exactamente como en F002 (FR-012).

### Tests (primero; deben fallar antes de implementar)

- [ ] T011 [P] [US2] Añadir a `tests/HackMaster.Web.Tests/DicePageTests.cs`:
  - `isSupported` falso → `dice3d-warning` y la opción no se puede activar (FR-011);
  - una `JSException` en `roll` → el total sale igual, aviso y modo sin 3D;
  - una tirada con `TooMany` → sin animación y con aviso (FR-007);
  - dados sin forma → nota en el detalle (FR-008);
  - con el 3D desactivado no se importa el módulo (FR-016)

### Implementation

- [ ] T012 [US2] Completar `DiceBoard.razor` y `Dice.razor` con los avisos y el paso al modo sin 3D hasta pasar T011

---

## Phase 5: User Story 3 - Dados que penetran en 3D (Priority: P2)

**Goal**: los dados extra ruedan después de su dado y se distinguen.
**Independent Test**: paso 3 de `quickstart.md` con la penetración activada.
**Verificación**: verificación común; pasa la prueba nueva de `Dice3DTests`.
**Restricciones**: el orden de las olas lo decide `DiceBoard.Plan`.

- [ ] T013 [P] [US3] Añadir a `tests/HackMaster.Web.Tests/E2E/Dice3DTests.cs`: un plan con olas de penetración se simula con las caras correctas en cada ola, y los dados extra llevan la marca de penetración (atributo de depuración del módulo) (FR-006)
- [ ] T014 [US3] Implementar en `dice3d.js` las olas (los extra ruedan cuando se para la ola anterior) y el aspecto distinto de los dados extra hasta pasar T013

---

## Phase 6: Polish & Cross-Cutting Concerns

**Objetivo**: calidad medida, documentación al día y pull request.
**Verificación**: verificación común; CI en verde en el pull request.

- [ ] T015 [P] Añadir a `tests/HackMaster.Web.Tests/E2E/NoHorizontalOverflowTests.cs` `/dados` con «Dados 3D» activado y una tirada en pantalla, a 390 y 820 px (SC-006)
- [ ] T016 [P] Añadir a `Dice3DTests` que una tirada en pantalla de `6d6` termina en menos de 4 s (SC-004) y que las caras de arriba de los dados en pantalla al pararse (consulta de depuración del módulo) coinciden con el detalle de la tirada (SC-001, FR-001)
- [ ] T017 Medir con Playwright los bytes descargados al abrir `/habilidades` y `/dados` sin 3D, comparar con `main` y anotarlo en `research.md` (SC-005)
- [ ] T018 Medir con el usuario los fotogramas por segundo en su móvil con `6d6` y anotar modelo y resultado en `research.md` (SC-003, FR-013)
- [ ] T019 Revisión independiente del diff por un subagente con contexto limpio y aplicar lo que encuentre
- [ ] T020 Actualizar `docs/ESTADO.md` y `tests/README.md`; abrir el pull request en castellano
- [ ] T021 Tras fusionar, comprobar `/dados` con 3D en el sitio publicado

---

## Dependencies & Execution Order

- Phase 1 y Phase 2 no dependen entre sí.
- US1 (Phase 3) depende de las dos.
- US2 (Phase 4) depende de US1, porque usa el componente.
- US3 (Phase 5) depende de US1.
- Polish (Phase 6), de todas.

### Tareas que necesitan al usuario

- T018: el móvil es suyo.
- T020 y T021: pull request y fusión.

### Parallel Opportunities

- T002 y T003.
- T006 y T007.
- T015 y T016.

## Implementation Strategy

1. **MVP**: fases 1 a 3, los dados ruedan y muestran el resultado del tirador.
2. **Fase 4**: respaldo garantizado.
3. **Fase 5**: penetración en 3D.
4. **Fase 6**: medición, revisión y entrega.
