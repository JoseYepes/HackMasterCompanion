---

description: "Task list for F007 — Estilo de fantasía heroica"
---

# Tasks: Estilo de fantasía heroica (F007)

**Input**: Design documents from `/specs/005-estilo-fantasia/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/theme.md, quickstart.md

**Tests**: incluidas. Principio IV: cada prueba se escribe y se ve fallar antes de implementar. El
nombre de cada prueba es la regla más su fuente (`Text_Has_4_5_Contrast_In_Both_Modes__FR006`).

**Organization**: por historia de usuario; cada fase es un bucle con objetivo, verificación,
restricciones y un límite de 8 iteraciones (formato de `CLAUDE.md`).

**Verificación común**: `dotnet build -warnaserror`, `dotnet test --no-build`,
`dotnet format --verify-no-changes`; y, en las fases de interfaz, las pruebas `E2E` con la
aplicación en marcha (`tests/README.md`).

**Restricciones comunes**:

- No borrar ni debilitar pruebas.
- `Domain` no cambia.
- No cambiar textos, `data-testid` ni el orden de los elementos de ninguna pantalla (FR-013).
- Sin paquetes nuevos; las únicas dependencias nuevas son las letras ya descargadas.
- Los archivos de `wwwroot/lib/` y `wwwroot/fonts/` no se editan.
- Interfaz en castellano.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario de `spec.md` (US1…US3)

---

## Phase 1: Setup

- [X] T001 Descargar, con el permiso del usuario, Cinzel 600 y 700 y EB Garamond 400, 600 y cursiva 400 (Fontsource 5.3.0, jsDelivr) con sus `LICENSE` en `src/HackMaster.Web/wwwroot/fonts/` (aparte de `lib/`, que solo se descarga con los dados 3D), comprobar su SHA-256 y anotarlas en `fonts/README.md`

---

## Phase 2: Foundational (colores de los dos modos)

**Objetivo**: las variables de color de los dos modos existen, cumplen los contrastes y ninguna
regla usa colores sueltos.
**Verificación**: verificación común; pasa `ThemeContrastTests`.
**Restricciones**: los colores salen de la tabla de `research.md` §2; si un par no llega al
contraste, se ajusta el color y se anota en `research.md`.

### Tests (primero; deben fallar antes de implementar)

- [X] T002 [P] Escribir `tests/HackMaster.Web.Tests/ThemeContrastTests.cs`: lee
  `src/HackMaster.Web/wwwroot/css/app.css`, toma las variables de `:root` y de
  `html[data-theme="dark"]`, y comprueba con la fórmula de WCAG 2.1 cada par de `research.md` §2:
  - texto normal, secundario, enlaces, errores, avisos y botones ≥ 4,5 (FR-006);
  - bordes de controles, títulos grandes y dados sobre la mesa ≥ 3 (FR-006, FR-012);
  - las dos listas de variables tienen los mismos nombres (ningún color se queda sin modo noche)
- [X] T003 [P] Añadir a `ThemeContrastTests`: fuera de los bloques de variables, `app.css` no
  tiene colores sueltos (`#…`, `rgb(…)`, nombres como `lightyellow` o `white`), para que nada se
  quede sin modo noche (FR-002, FR-009)
- [X] T004 Descartada en el análisis (2026-11-18): era una clase `ThemePreference` en C# que nadie
  usaría, porque la preferencia la lee y escribe el script de `index.html` (data-model.md,
  principio III). Sus casos (JSON roto, otra versión, modo desconocido) pasan a T011

### Implementation

- [X] T005 En `src/HackMaster.Web/wwwroot/css/app.css`: `@font-face` de las 5 letras con `font-display: swap` (FR-014), variables de color de los dos modos (`:root` y `html[data-theme="dark"]`) y sustituir todos los colores sueltos (`#fff`, `#b42318`, `lightyellow`…) por variables, hasta pasar T002 y T003

---

## Phase 3: User Story 1 - Aspecto de pergamino y tinta (Priority: P1) 🎯 MVP

**Goal**: todas las pantallas con el aspecto de la maqueta A.

**Independent Test**: recorrer las 12 pantallas de SC-001 en modo normal.

**Objetivo**: estilo A en las 12 pantallas, sin desbordamiento y sin peticiones externas.
**Verificación**: verificación común + `E2E`; captura de Habilidades, una ficha y Dados a 390 px.
**Restricciones**: FR-013 (solo aspecto).

### Tests (primero)

- [X] T006 [P] [US1] Ampliar `tests/HackMaster.Web.Tests/E2E/NoHorizontalOverflowTests.cs` para que recorra las pantallas en los dos modos (FR-011, SC-006)
- [X] T007 [P] [US1] Escribir en `tests/HackMaster.Web.Tests/E2E/ThemeTests.cs`:
  - al abrir las 12 pantallas no sale ninguna petición fuera del origen de la aplicación (FR-008, SC-004);
  - los `h1` usan Cinzel y el texto EB Garamond, y `document.fonts` las tiene cargadas (FR-001, FR-007);
  - todos los botones visibles de Habilidades, el editor de conjuntos y Dados miden al menos 44 × 44 px a 390 px (FR-010)

### Implementation

- [X] T008 [US1] Dar el aspecto de la maqueta A en `app.css`: letras (Cinzel solo en títulos, nombre de la aplicación; los nombres de las tarjetas van en Garamond seminegrita porque en Cinzel los largos se partían mal), barra granate con borde dorado, tarjetas de pergamino en las listas, campos, botones, etiquetas, avisos, foco visible (FR-010) y botones de 44 px, hasta pasar T006 y T007
- [X] T009 [US1] En `src/HackMaster.Web/wwwroot/index.html`: precargar Cinzel 600 y EB Garamond 400; adaptar los colores de la pantalla de carga y del aviso de error de Blazor a las variables

**Checkpoint**: la aplicación ya tiene el aspecto A; el modo noche aún no se puede elegir.

---

## Phase 4: User Story 2 - Modo noche de taberna (Priority: P1)

**Goal**: botón ☀/☾ que cambia al modo noche, se recuerda y no da destellos.

**Independent Test**: activar el modo noche, recargar y seguir en modo noche sin destello.

**Objetivo**: contrato `contracts/theme.md` cumplido.
**Verificación**: verificación común + `E2E`.
**Restricciones**: el script de `<head>` es corto, sin dependencias, y funciona si `localStorage` falla.

### Tests (primero)

- [X] T010 [P] [US2] Escribir `tests/HackMaster.Web.Tests/ThemeTogglePageTests.cs` (bUnit, JavaScript simulado):
  - con `current()` = `light`, el botón se llama «Cambiar a modo noche»; con `dark`, «Cambiar a modo normal» (FR-003);
  - al pulsarlo llama a `hackmasterTheme.set` con el otro modo y cambia su nombre;
  - el botón está en la barra de navegación de `MainLayout`
- [X] T011 [P] [US2] Añadir a `tests/HackMaster.Web.Tests/E2E/ThemeTests.cs`:
  - con `{"schemaVersion":1,"mode":"dark"}` guardado, `data-theme="dark"` ya está puesto en `DOMContentLoaded` (FR-005);
  - tras pulsar el botón y recargar, sigue el modo elegido (FR-004);
  - sin preferencia y con el dispositivo en oscuro, se abre en modo noche; en claro, en normal (FR-004);
  - con JSON roto, `schemaVersion` 2 o un modo desconocido guardado, se trata como "sin elegir" y sigue al dispositivo; si `localStorage` falla, el botón cambia el modo igualmente (contrato);
  - del clic al cambio de color del fondo de la página, menos de 500 ms (SC-003);
  - el botón mide al menos 44 × 44 px y no se pierde lo escrito en la búsqueda al cambiar (FR-005, FR-010)

### Implementation

- [X] T012 [US2] Script en el `<head>` de `src/HackMaster.Web/wwwroot/index.html`, antes de la hoja de estilos: leer `hackmaster.theme`, poner `data-theme`, `<meta name="color-scheme">`, `<meta name="theme-color">` y `window.hackmasterTheme` (`current`, `set`) según `contracts/theme.md`
- [X] T013 [US2] Crear `src/HackMaster.Web/Components/ThemeToggle.razor` (iconos SVG de sol y luna, `aria-label`, `data-testid="theme-toggle"`) y ponerlo en la barra de `src/HackMaster.Web/Layout/MainLayout.razor`, hasta pasar T010 y T011

**Checkpoint**: los dos modos funcionan en todas las pantallas.

---

## Phase 5: User Story 3 - La mesa de dados acompaña al estilo (Priority: P2)

**Goal**: cuero granate en modo normal, madera oscura en modo noche.

**Independent Test**: tirar `2d6+1d20` con dados 3D en los dos modos.

### Tests (primero)

- [X] T014 [P] [US3] Añadir a `tests/HackMaster.Web.Tests/E2E/ThemeTests.cs`: el fondo de `[data-testid=dice-board]` cambia al cambiar de modo, y cambiar de modo a mitad de una tirada 3D no cambia el resultado mostrado (FR-012, US3 escenario 3)

### Implementation

- [X] T015 [US3] En `app.css`: `.dice-board` de cuero (degradado granate y costura dorada) en modo normal y de madera (vetas con `repeating-linear-gradient`) en modo noche; bandeja de dados con las variables del modo, hasta pasar T014

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T016 Medir la primera carga sin caché y compararla con los 2837 KB de antes (SC-005); anotarlo en `research.md` §6
- [X] T017 [P] Actualizar `tests/README.md` (pruebas nuevas) y `docs/ESTADO.md`
- [X] T018 Revisión independiente del diff (subagente con contexto limpio): regresiones, pruebas débiles, contraste, FR-013 y constitución; corregir lo que encuentre
- [X] T019 Pull request en castellano; tras fusionar, comprobar el sitio publicado en los dos modos
- [ ] T020 Comprobación manual del usuario en su móvil: reconoce las maquetas A y B (SC-007)

---

## Dependencies & Execution Order

- Phase 2 bloquea al resto: sin variables no hay modos.
- US1 (Phase 3) antes que US2 (Phase 4): el modo noche redefine las variables que usa US1.
- US3 (Phase 5) depende de Phase 2; puede hacerse en paralelo con US2.
- Polish al final; T019 y T020 después de fusionar.

## Parallel Example

```text
T002 contrastes            ‖  T003 colores sueltos
T006 desbordamiento         ‖  T007 ThemeTests (US1)
T010 ThemeTogglePageTests   ‖  T011 ThemeTests (US2)
```

## Implementation Strategy

MVP = Phases 1–3: la aplicación ya se ve como el pergamino. Después el modo noche (US2) y la mesa
(US3), cada uno comprobable por separado.

## Coverage (FR → tareas)

| Requisito | Tareas |
|---|---|
| FR-001, FR-007 | T007, T008 |
| FR-002 | T002, T003, T005, T011, T013 |
| FR-003 | T010, T013 |
| FR-004, FR-005 | T011, T012 |
| FR-006 | T002, T005 |
| FR-008, FR-014 | T001, T005, T007 |
| FR-009 | T002, T003, T008 |
| FR-010 | T007, T008, T011 |
| FR-011 | T006 |
| FR-012 | T002, T014, T015 |
| FR-013 | restricción común; revisión T018 |
| SC-001…SC-007 | T006, T007, T011, T016, T020 |
