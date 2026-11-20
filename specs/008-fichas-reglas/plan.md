# Implementation Plan: Fichas con reglas (F005)

**Branch**: `008-fichas-reglas` | **Date**: 2026-11-20 | **Spec**: [spec.md](./spec.md)

## Summary

La ficha calcula la maestría de cada habilidad y deja tirarla:

- **Dominio**: `HonorDie` (los dados de la Tabla 5C), `SkillImprovement` (una tirada del dado de
  maestría y una del Dado de Honor), `SkillRules` (maestría base, maestría con tope de 125 %, nivel,
  tirar una mejora y prueba de habilidad). Una prueba por regla con el libro y la página del MJ.
- **Formato de fichas versión 3**: Dado de Honor en la ficha y mejoras en cada habilidad; la
  maestría de F004 se conserva como «maestría escrita». Migración 2 → 3 con prueba.
- **Editor**: Dado de Honor, y en cada habilidad maestría calculada con su nivel y desglose, «Tirar
  mejora», «Apuntar mejora» a mano, quitar mejoras y «Tirar» con modificador. La tirada de
  habilidad se guarda en el historial del tirador (`RollHistoryUpdate`, extraído de `Dice.razor`
  porque ahora lo usan dos páginas).

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS)

**Primary Dependencies**: ninguna nueva.

**Storage**: IndexedDB (`hackmaster.characters`, versión 3) y `localStorage` (`hackmaster.rolls`, sin
cambios de formato).

**Testing**: xUnit (`SkillRulesTests`, `HonorDieTests`, `CharacterStorageTests`), bUnit
(`CharacterPagesTests`), Playwright (`E2E/CharacterTests`).

**Performance Goals**: tirar una habilidad en menos de 1 s (SC-003).

**Constraints**: 0 avisos; sin desbordamiento a 390 y 820 px; reglas solo del MJ (constitución I).

**Scale/Scope**: 3 tipos de dominio, 1 versión de formato, cambios en el editor y en `Dice.razor`.

## Constitution Check

| Principio | Evaluación en F005 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Reglas del MJ p. 85, 100–101 y 307 y Glosario; la Tabla 5C no se copia (el jugador elige su dado). | PASA |
| II. Prioridad de fuentes | MJ en castellano; discrepancias a la pestaña «Dudas». | PASA |
| III. Dominio puro y arquitectura mínima | Reglas puras en `Domain`, azar por `IRandom`. `RollHistoryUpdate` se extrae porque hay un segundo uso real. | PASA |
| IV. Test primero | Una prueba por regla con libro y página, antes del código. | PASA |
| V. Local primero | Formato versión 3 con migración probada. | PASA |
| VI. Entrega verificable | `quickstart.md`. | PASA |
| VII. Coste y seguridad | Sin Azure, sin dependencias nuevas. | PASA |

## Project Structure

```text
src/HackMaster.Domain/SkillRules.cs        # HonorDie, SkillImprovement, MasteryLevel, SkillCheck, SkillRules
src/HackMaster.Domain/Character.cs         # HonorDie en Character; Improvements en CharacterSkill (Mastery pasa a ser la maestría escrita)
src/HackMaster.Domain/StoredData.cs        # CharactersVersion = 3, migración 2 → 3
src/HackMaster.Web/RollHistoryUpdate.cs    # leer, cambiar y guardar el historial (de Dice.razor)
src/HackMaster.Web/Pages/CharacterEditor.razor
tests/…/SkillRulesTests.cs, HonorDieTests.cs, CharacterStorageTests.cs, CharacterPagesTests.cs, E2E/CharacterTests.cs
```

## Complexity Tracking

Sin desviaciones.
