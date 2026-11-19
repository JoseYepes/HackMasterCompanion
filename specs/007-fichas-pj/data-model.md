# Data Model: Fichas de PJ (F004)

## Character (dominio)

| Campo | Tipo | Reglas |
|---|---|---|
| `Id` | texto | obligatorio, no vacío; no cambia al editar |
| `Name` | texto | obligatorio tras recortar espacios |
| `Race`, `Class`, `Alignment` | texto | opcionales, libres |
| `Level` | entero? | ≥ 0 si se da |
| `Abilities` | diccionario `Ability` → `AbilityScore` | las 7 claves (FUE, DES, CON, INT, SAB, CAR, ASP) siempre presentes al leer |
| `HitPoints`, `Honor` | entero? | admiten negativos |
| `Skills` | lista de `CharacterSkill` | en el orden en que se añadieron |
| `Equipment`, `Notes` | texto | opcionales, multilínea |
| `CreatedAt`, `UpdatedAt` | fecha | `UpdatedAt` ≥ `CreatedAt` |

## AbilityScore

| Campo | Tipo | Reglas |
|---|---|---|
| `Score` | entero? | ≥ 0 si se da |
| `Fraction` | entero? | 0–99 si se da; se muestra `Score/Fraction` con dos cifras |

## CharacterSkill

| Campo | Tipo | Reglas |
|---|---|---|
| `Slug` | texto | referencia a una habilidad de `data/skills.json` |
| `Name` | texto | nombre al añadirla; se muestra si el slug ya no existe |
| `Mastery` | entero? | ≥ 0 si se da (porcentaje) |
| `Points` | entero? | ≥ 0 si se da |

## Validación (`Character.Problems`)

Devuelve los mensajes en castellano de lo que impide guardar: nombre vacío y números negativos donde
no se admiten. Sin mensajes, se puede guardar.

## Copia (`Character.Duplicate(newId, now)`)

Misma ficha con otro `Id`, nombre `"<nombre> (copia)"`, `CreatedAt` y `UpdatedAt` = `now`, y listas
nuevas (independiente de la original).

## Formatos guardados

### Lo guardado — clave `hackmaster.characters` (IndexedDB)

```json
{ "schemaVersion": 2, "characters": [ { ...Character } ] }
```

### Archivo de una ficha — ver [contracts/character-file.md](contracts/character-file.md)

### Versión 1 → 2

- v1: `abilities` con FUE, DES, CON, INT, SAB y CAR.
- v2: añade `ASP` (vacío al migrar). Un archivo v1 sin alguna de las seis se completa con vacío.
- `schemaVersion` mayor que 2: se rechaza con «versión más reciente». Sin versión o dañado: se
  rechaza con mensaje y no se escribe nada.
