# Data Model: Dados personalizables y sonido (F008)

## Dominio (`HackMaster.Domain`)

### DiceMaterial (enumeración)

`Ivory` (marfil, por defecto), `Marble` (mármol), `OldGold` (oro viejo), `BlackIron` (hierro
negro), `Ruby` (rubí), `Obsidian` (obsidiana), `Bone` (hueso), `Amethyst` (amatista), `Wood`
(madera), `Stone` (piedra), `Crystal` (cristal). Se guardan como texto (`"Marble"`).

### PenetrationEffect (enumeración)

`Explosion` (por defecto), `Fire`, `Blood`, `None`.

### DiceQuality (enumeración)

`High` (por defecto), `Low`.

### DiceLook

| Campo | Tipo | Reglas |
|---|---|---|
| `Material` | `DiceMaterial` | uno de los 11 |
| `Color` | texto `#rrggbb` | color base; cada material tiene uno por defecto |
| `Ink` | texto `#rrggbb` | color de los números |
| `Effect` | `PenetrationEffect` | |
| `Sound` | `bool` | `true` la primera vez (aclaración 3) |
| `Quality` | `DiceQuality` | |

- `DiceLook.Default`: marfil `#f4efe6`, números `#1f2328`, explosión, sonido, calidad alta.
- `DiceLook.ForMaterial(material)`: el aspecto con el color y los números por defecto de ese
  material (los del prototipo), conservando efecto, sonido y calidad.
- `DiceLook.InkContrast`: contraste WCAG 2.1 entre `Color` e `Ink`.
- `DiceLook.ReadableInk()`: el más contrastado de un claro (`#f8f1e0`) y un oscuro (`#1f2328`).
- Colores que no son `#rrggbb` → se usan los del material (lectura tolerante).

### BoardDie (cambio)

Se añade `Parent` (`int?`): para un dado extra por penetración, el índice en `BoardPlan.Dice` del
dado que penetró; nulo en los demás. En un d100 penetrante, el padre es el d10 de las unidades.

## Guardado (`hackmaster.rolls`, versión 3)

```json
{
  "schemaVersion": 3,
  "penetrating": false,
  "dice3d": true,
  "look": { "material": "Marble", "color": "#2f5a46", "ink": "#f1e5c4",
            "effect": "Explosion", "sound": true, "quality": "High" },
  "rolls": [ … ]
}
```

- `StoredData.RollsVersion` pasa a 3. Migración 2 → 3: se añade `look` con `DiceLook.Default`.
  La 1 → 2 sigue igual (se encadenan).
- Un `look` incompleto o con valores desconocidos se completa con los valores por defecto, sin
  perder el historial.
- `hackmaster.suites` no cambia (versión 1).
