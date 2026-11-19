# Tasks: Fichas de PJ (F004)

**Input**: `specs/007-fichas-pj/` (spec, plan, research, data-model, contracts, quickstart)

**Tests**: obligatorios (constitución IV): cada prueba se escribe antes del código y se ve fallar.

## Phase 1: Setup

- [X] T001 Añadir la entrada «F004» en `docs/ESTADO.md` (rama, carpeta y decisiones de la aclaración) y cerrar F008 (PR #11 fusionado)

## Phase 2: Foundational (dominio y formatos)

- [X] T002 [P] Pruebas de la ficha en `tests/HackMaster.Domain.Tests/CharacterTests.cs`: `Problems` (nombre vacío, negativos en nivel, características, fracción fuera de 0–99, maestría y puntos; PG y Honor negativos válidos), `Duplicate` (otro id, «(copia)», fechas, listas independientes), formato `12/45` de `AbilityScore`
- [X] T003 [P] Pruebas de formatos en `tests/HackMaster.Domain.Tests/CharacterStorageTests.cs`: escribir y leer lo guardado y el archivo da la misma ficha campo a campo (SC-001); v1 → v2 con ASP vacío y el resto igual (SC-002); versión mayor, sin versión, `kind` distinto, sin id o nombre y JSON dañado dan error; las 7 características siempre presentes al leer
- [X] T004 Implementar `Character`, `AbilityScore`, `CharacterSkill`, `Problems` y `Duplicate` en `src/HackMaster.Domain/Character.cs` hasta que pase T002
- [X] T005 Implementar `CharactersFile`, `CharacterFile`, `ReadCharacters`, `ReadCharacterFile`, `Write` y la migración 1 → 2 en `src/HackMaster.Domain/StoredData.cs` hasta que pase T003

## Phase 3: User Story 1 — Crear y editar (P1) 🎯 MVP

**Independent Test**: crear «Grimbold» con todos los campos, recargar y ver lo mismo.

- [X] T006 [P] [US1] Módulo `src/HackMaster.Web/wwwroot/js/characters.js` (`get`, `set`, `download`) según `contracts/character-file.md`
- [X] T007 [P] [US1] Pruebas bUnit de `PlayerCharacters` e `IndexedStore` en `tests/HackMaster.Web.Tests/PlayerCharactersTests.cs`: guardar crea y actualiza, relee antes de escribir (otra pestaña), no escribe si lo guardado no se pudo leer, aviso si el navegador no deja guardar
- [X] T008 [US1] Implementar `src/HackMaster.Web/IndexedStore.cs` y `src/HackMaster.Web/PlayerCharacters.cs` y registrarlos en `Program.cs` hasta que pase T007
- [X] T009 [P] [US1] Pruebas bUnit de las páginas en `tests/HackMaster.Web.Tests/CharacterPagesTests.cs`: lista vacía y con fichas, nombre obligatorio, guardar y volver a la lista, buscar y añadir una habilidad sin importar acentos, habilidad no encontrada marcada
- [X] T010 [US1] Implementar `src/HackMaster.Web/Pages/Characters.razor` (`/fichas`) y `src/HackMaster.Web/Pages/CharacterEditor.razor` (`/fichas/nueva`, `/fichas/{id}`), enlace «Fichas» en `Layout/MainLayout.razor` y estilos en `wwwroot/css/app.css` hasta que pase T009

## Phase 4: User Story 2 — Exportar e importar (P1)

**Independent Test**: exportar, borrar, importar y ver la misma ficha.

- [X] T011 [P] [US2] Pruebas bUnit en `tests/HackMaster.Web.Tests/CharacterPagesTests.cs`: exportar llama a `download` con el archivo y el nombre; importar añade, pregunta si ya existe (sustituir o copia), muestra el error de un archivo malo sin cambiar nada
- [X] T012 [US2] Implementar exportar (editor y lista) e importar (`InputFile`, máximo 1 MB) en las páginas y `PlayerCharacters.ImportAsync` hasta que pase T011

## Phase 5: User Story 3 — Duplicar y borrar (P2)

- [X] T013 [P] [US3] Pruebas bUnit: duplicar crea «(copia)»; borrar pide confirmación y solo borra si se confirma
- [X] T014 [US3] Implementar duplicar y borrar en las páginas y en `PlayerCharacters` hasta que pase T013

## Phase 6: Polish

- [X] T015 [P] Pruebas de navegador en `tests/HackMaster.Web.Tests/E2E/CharacterTests.cs`: crear, recargar y ver (IndexedDB real); exportar e importar idéntica (descarga de Playwright); sin peticiones fuera del sitio (SC-003); sin desbordamiento a 390 y 820 px (SC-005)
- [ ] T016 Actualizar `tests/README.md`, `docs/ESTADO.md` y marcar tareas; `dotnet format`, todas las pruebas
- [ ] T017 Revisión independiente (subagente) del diff y corrección de lo que encuentre
- [ ] T018 Pull request, CI en verde y fusión con el visto bueno del usuario
- [ ] T019 Comprobación del usuario en su móvil (quickstart, comprobación a mano)

## Dependencies

- T002–T005 antes de todo lo demás. US1 antes de US2 y US3 (usan `PlayerCharacters` y las páginas).
- US2 y US3 tocan los mismos archivos: van una tras otra.
