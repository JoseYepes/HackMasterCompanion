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

- [X] T011 Nivel 2 de mago y de clérigo: 61 de mago (MJ p. 187–198) y 23 de clérigo (p. 257–260), cotejados con la lista del Apéndice A; erratas aplicadas a rigor mortis, rito menor, puño atronador GpC y reparar
- [X] T012 Niveles 3 a 9 de mago y 3 a 7 de clérigo, uno a uno. Hechos los niveles 8 y 9 de mago (tanda 8): 25 y 25 (p. 243–252). Hecho el nivel 3 (tanda 3): 54 de mago (MJ p. 198–207) y 25 de clérigo (p. 260–264); errata aplicada a paralización. Hecho el nivel 4 (tanda 4): 57 de mago (p. 207–218) y 18 de clérigo (p. 264–267). Hecho el nivel 5 (tanda 5): 47 de mago (p. 218–226) y 18 de clérigo (p. 268–271); errata aplicada a visión falsa. Hecho el nivel 6 (tanda 6): 48 de mago (p. 226–236) y 13 de clérigo (p. 271–273). Hecho el nivel 7 (tanda 7): 33 de mago (p. 236–243) y 14 de clérigo (p. 273–276), con lo que acaba el clérigo; errata aplicada a cabrear deidad
- [ ] T013 Trucos de mago
- [ ] T014 Conjuros de la SSG
- [ ] T015 Sustituir los resúmenes por los del documento del usuario, si llega
- [ ] T016 Pestaña «Dudas» en `Hackconjuros.xlsx` importada a la lista de dudas, con las de la tanda 2: erratas aplicadas (rigor mortis, rito menor, puño atronador GpC, reparar), nombres distintos en el Apéndice A (nube de bruma/nube brumosa, aura de silencio/zona de silencio), «curar heridas luminosas» en sanar heridas ligeras (errata probable del MJ) y la esfera Elemental (Aire) de polvareda, que el formato no recoge aparte
