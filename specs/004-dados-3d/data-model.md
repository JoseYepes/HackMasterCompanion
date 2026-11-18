# Data Model: Dados 3D (F003)

## Dominio (`HackMaster.Domain`)

| Tipo | Campos y reglas |
|---|---|
| `DieShape` (enumeración) | `D4`, `D6`, `D8`, `D10`, `D10Tens` (d10 de decenas: 00, 10…90), `D12`, `D20`. |
| `BoardDie` | `Shape`, `Face` (texto de la cara que debe quedar arriba: "6", "00", "70"…), `Wave` (0 = tirada inicial, 1, 2… = dados extra por penetración, en el orden en que aparecen) y `IsPenetration`. |
| `BoardPlan` | `Dice` (lista de `BoardDie`, como mucho 20), `Unshaped` (grupos sin forma física: d2, d3, d1000…, que solo van en texto), `TooMany` (la tirada empieza con más de 20 dados: no se dibuja nada) y `Truncated` (los extra por penetración pasaron de 20; se dibujan los primeros). |
| `DiceBoard` | `BoardPlan Plan(RollResult result)`, función pura. |

Reglas de `DiceBoard.Plan`:

| Caso | Resultado |
|---|---|
| d4, d6, d8, d10, d12, d20 | un `BoardDie` por dado, con `Face` = valor |
| d100 (valor v) | dos dados: `D10Tens` con `Face` = decenas (00 para v = 100 o v < 10) y `D10` con `Face` = unidades (0 para 100 y para múltiplos de 10). v = 100 → "00" y "0"; v = 7 → "00" y "7"; v = 70 → "70" y "0" |
| d2, d3, d1000, cualquier otro | no se dibuja; el grupo va a `Unshaped` |
| penetración | los extra de cada dado van en la siguiente `Wave`, detrás de su dado |
| más de 20 dados con forma en la tirada inicial (un d100 cuenta 2) | `TooMany` = verdadero y `Dice` vacío |
| extras que pasan de 20 | se cortan y `Truncated` = verdadero |

## Bandeja de dados (US4)

| Tipo | Campos y reglas |
|---|---|
| `DiceTray` | `Counts` (dados por número de caras: 4, 6, 8, 10, 12, 20, 100) y `Modifier` (entero). Inmutable. |
| `DiceTray.FromText(text)` | Bandeja equivalente a la fórmula escrita, o nulo si no la entiende. Vacío → bandeja vacía; un número suelto ("+1", "-2") → solo modificador; una fórmula válida → sus dados sumados por caras y sus números sumados. No la entiende si tiene dados restados o dados que no están en la bandeja (d2, d3…). |
| `Add(sides)`, `AddModifier(delta)`, `ToText()` | `Add` suma un dado; `ToText` escribe los dados de menos a más caras y el modificador al final ("3d6+1d20+2"). Si el resultado pasa de los límites de `DiceFormula` (100 dados, número de 10 000), `Add`/`AddModifier` devuelven el motivo y la bandeja no cambia. |

## Guardado en el dispositivo

La opción «Dados 3D» se guarda junto a la de penetración en `hackmaster.rolls`. El formato pasa a
la **versión 2**, con un campo nuevo `dice3d` (bool, falso por defecto).

- **Migración 1 → 2** en `StoredData.Migrate`: añade `"dice3d": false` y no toca nada más.
- **Pruebas**: un historial guardado en la versión 1 se lee sin perder tiradas y con
  `dice3d = false` (principio V: migraciones con prueba).
