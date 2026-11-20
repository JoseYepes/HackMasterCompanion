# Implementation Plan: Grimorio (F009)

**Branch**: `009-grimorio` | **Date**: 2026-11-20 | **Spec**: [spec.md](./spec.md)

## Summary

Pestaña «Grimorio» con los conjuros de mago y de clérigo y las reglas de los libros de conjuros:

- **Datos**: el agente lee el MJ (PDF local del usuario) página a página, cotejando cada conjuro con
  la imagen de su página, y escribe los datos de cabecera y un resumen propio en
  `Hackconjuros.xlsx` (pestañas «Conjuros» y «Reglas»), una hoja nueva junto a la de habilidades y
  fuera del repositorio. `tools/DataImport conjuros <hoja> data/` la convierte en
  `data/spells.json` (constitución V). El texto del manual no se copia nunca.
- **Dominio**: `Spell`, `SpellClass`, `GrimoireRule`, `SpellsFile`, `SpellQuery`, `SpellFacets`.
- **Web**: `GrimoireData` (aparte de `GameData`: otra hoja, llega por tandas y vacía no es error) y
  las páginas `/grimorio`, `/grimorio/{clase}/{slug}` y `/grimorio/reglas`.
- **Tandas**: 1) reglas y nivel 1 de mago y clérigo; después nivel a nivel, y trucos y SSG al final.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS). **Primary Dependencies**: ninguna nueva (ClosedXML ya
la usa el importador). **Storage**: `data/spells.json` incrustado. **Testing**: xUnit
(`SpellQueryTests`, `SpellImportTests`), bUnit (`GrimoirePagesTests`), Playwright
(`E2E/GrimoireTests`). **Constraints**: sin texto del manual en el repositorio (SC-003); sin
desbordamiento a 390 y 820 px.

## Constitution Check

| Principio | Evaluación en F009 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Datos copiados de la cabecera de cada conjuro, cotejados con la página; resúmenes propios; lo dudoso, «?» (pendiente). Erratas revisadas. | PASA |
| II. Prioridad de fuentes | MJ en castellano; erratas oficiales y en castellano revisadas por tanda. | PASA |
| III. Dominio puro y arquitectura mínima | Tipos de dominio sin dependencias; `GrimoireData` aparte porque su ciclo de datos es otro. | PASA |
| IV. Test primero | Pruebas de búsqueda, importación y páginas antes del código. | PASA |
| V. Local primero | JSON generado desde una hoja con el importador; la app no lee la hoja. | PASA |
| VI. Entrega verificable | `quickstart.md`. | PASA |
| VII. Coste y seguridad | Sin Azure ni dependencias nuevas. | PASA |
