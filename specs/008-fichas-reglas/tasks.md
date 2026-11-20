# Tasks: Fichas con reglas (F005)

**Tests**: obligatorios y antes del código (constitución IV), con libro y página en el nombre.

## Phase 1: Dominio

- [X] T001 [P] Pruebas de `HonorDie` en `tests/HackMaster.Domain.Tests/HonorDieTests.cs`: los 13 dados de la Tabla 5C (MJ p. 85), texto, `TryParse` rechaza otros, tirar suma el bono
- [X] T002 [P] Pruebas de `SkillRules` en `tests/HackMaster.Domain.Tests/SkillRulesTests.cs`: ejemplo de Corte a la yugular 29 % (MJ p. 100), media a la baja (MJ p. 307), tope 125 % (MJ p. 101), sin característica → nulo, niveles (MJ p. 100), tirar mejora con `IRandom`, prueba: ≤ maestría éxito, 01–05 y 98–00 automáticos, modificador (MJ Glosario)
- [X] T003 Implementar `src/HackMaster.Domain/SkillRules.cs` hasta que pasen T001 y T002

## Phase 2: Formato versión 3

- [X] T004 Pruebas en `CharacterStorageTests.cs`: ida y vuelta con Dado de Honor y mejoras, migración 2 → 3 (y 1 → 3), Dado de Honor desconocido leído como vacío
- [X] T005 Añadir `HonorDie` a `Character`, `Improvements` a `CharacterSkill`, `CharactersVersion = 3` y la migración en `StoredData.cs`; ajustar `HasSameData` y `Problems` (mejoras negativas)

## Phase 3: Ficha (US1 y US2)

- [X] T006 Extraer `src/HackMaster.Web/RollHistoryUpdate.cs` de `Pages/Dice.razor` sin cambiar su comportamiento (pasan las pruebas de los dados)
- [X] T007 Pruebas bUnit en `CharacterPagesTests.cs`: elegir Dado de Honor, maestría calculada y nivel, apuntar y quitar mejora, tirar mejora, maestría escrita de F004 conservada y borrable, tirar habilidad con modificador y guardado en el historial
- [X] T008 Implementar los cambios del editor (`Pages/CharacterEditor.razor`; se descartó un componente aparte porque la habilidad comparte con el editor la validación de números) hasta que pase T007; estilos en `app.css`

## Phase 4: Cierre

- [X] T009 Prueba de navegador en `E2E/CharacterTests.cs`: apuntar mejora, ver 29 %, tirar y ver la tirada en `/dados`; sin desbordamiento
- [X] T010 `tests/README.md`, `docs/ESTADO.md`, `dotnet format`, todas las pruebas
- [X] T011 Revisión independiente y correcciones
- [ ] T012 Pull request, CI en verde y fusión con el visto bueno del usuario
- [ ] T013 Comprobación del usuario en su móvil
