# Data Model: Fichas con reglas (F005)

## HonorDie

| Campo | Tipo | Reglas |
|---|---|---|
| `Sides` | entero | 3, 4, 6, 8, 10, 12 o 20 |
| `Bonus` | entero | 0, 1 o 2 (solo las combinaciones de la Tabla 5C) |

Texto: `d12+1`. `HonorDie.All` lista los 13 de la tabla en orden; `TryParse` acepta solo esos.

## SkillImprovement

| Campo | Tipo | Reglas |
|---|---|---|
| `Mastery` | entero | tirada del dado de maestría, ≥ 0 |
| `Honor` | entero | tirada del Dado de Honor (con su bono), ≥ 0 |

## CharacterSkill (versión 3)

| Campo | Cambio |
|---|---|
| `Mastery` | ahora «maestría escrita a mano» (de F004); opcional |
| `Improvements` | nuevo: lista de `SkillImprovement`, vacía por defecto |

## Character (versión 3)

| Campo | Cambio |
|---|---|
| `HonorDie` | nuevo: texto (`d12+1`) u omitido |

## Migración 2 → 3

Sin cambios de datos: `honorDie` ausente y `improvements` vacías. Se sube la versión para que una
aplicación antigua no lea un archivo nuevo y pierda las mejoras al guardar. Un `honorDie` que no
esté en la tabla se lee como vacío.

## Resultados

- `MasteryLevel`: NoIdea (0), Novice (1–25), Apprentice (26–50), Advanced (51–75), Expert (76–100),
  Master (101–125).
- `SkillCheck(Roll, Modifier, Total, Mastery, Outcome)` con `Outcome` = Success, Failure,
  AutomaticSuccess o AutomaticFailure.
