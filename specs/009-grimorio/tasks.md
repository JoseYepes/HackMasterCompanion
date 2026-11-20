# Tasks: Grimorio (F009)

## Tanda 1 — pestaña, reglas y nivel 1

- [X] T001 Pruebas de búsqueda y filtros en `tests/HackMaster.Domain.Tests/SpellQueryTests.cs`
- [X] T002 `src/HackMaster.Domain/Spell.cs` (Spell, SpellClass, GrimoireRule, SpellsFile, SpellQuery, SpellFacets)
- [X] T003 Pruebas del importador en `tests/HackMaster.DataImport.Tests/SpellImportTests.cs` (campos, «?» pendiente, resumen que falta, errores, duplicados, orden `conjuros`)
- [X] T004 `tools/DataImport/SpellImporter.cs` y la orden `conjuros` en `Cli.cs`
- [X] T005 Pruebas bUnit en `tests/HackMaster.Web.Tests/GrimoirePagesTests.cs`
- [X] T006 `GrimoireData`, páginas `Grimoire`, `SpellDetail` y `GrimoireRules`, enlace en el menú y estilos
- [X] T007 Leer el MJ: 67 conjuros de mago de nivel 1 (p. 175–187) y 20 de clérigo de nivel 1 (p. 253–256), cotejados con la imagen de cada página y con la lista alfabética del Apéndice A; 7 reglas (p. 19, 124–125, 180, 253); erratas revisadas
- [X] T008 Generar `Hackconjuros.xlsx` e importar `data/spells.json`
- [X] T009 Pruebas de navegador en `tests/HackMaster.Web.Tests/E2E/GrimoireTests.cs`
- [X] T010 Revisión independiente, pull request y fusión con el visto bueno del usuario

## Tandas siguientes (mismo formato, solo filas nuevas)

- [ ] T011 Nivel 2 de mago y de clérigo
- [ ] T012 Niveles 3 a 9 de mago y 3 a 7 de clérigo, uno a uno
- [ ] T013 Trucos de mago
- [ ] T014 Conjuros de la SSG
- [ ] T015 Sustituir los resúmenes por los del documento del usuario, si llega
