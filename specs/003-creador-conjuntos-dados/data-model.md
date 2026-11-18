# Data Model: Creador de conjuntos y tirador 2D (F002)

Tipos nuevos de `HackMaster.Domain` (inmutables, sin dependencias) y formato de lo guardado en el
dispositivo. Los tipos de F001 (`Skill`, `SkillSuite`, …) no cambian.

## Tirador

| Tipo | Campos y reglas |
|---|---|
| `DiceTerm` | `Sign` (+1 o −1); `Count` (1-100) y `Sides` (2-1000) para un grupo de dados, o `Constant` (0-10000) para un número. |
| `DiceFormula` | `Text` (normalizado: minúsculas, sin espacios) y `Terms`. Se obtiene con `DiceFormula.Parse(string)`, que devuelve la fórmula o un `DiceFormulaError`. Al menos un grupo; como mucho 100 dados en total y 100 caracteres. |
| `DiceFormulaError` | `Position` (índice del carácter), `Fragment` (trozo erróneo), `Message` (castellano) y `Example` ("2d6+3"). |
| `DieRoll` | `Sides`, `Value` (1..Sides), `IsPenetration` (dado extra por penetración; aporta `Value − 1`). |
| `GroupRoll` | `Term`, `Dice` (lista de `DieRoll`), `Subtotal` (con signo) y `PenetrationCapped` (se alcanzó el límite de 100 dados extra). |
| `RollResult` | `Formula`, `Penetrating` (bool), `Groups`, `Constants`, `Total`, `RolledAt` (fecha y hora, la pone quien llama). |
| `IRandom` | `int Next(int minInclusive, int maxInclusive)`. |
| `DiceRoller` | `RollResult Roll(DiceFormula, bool penetrating, IRandom, DateTimeOffset)`. Con penetración, cada dado que saca `Sides` añade otro dado; se repite mientras salga el máximo, hasta 100 extra por dado. |

## Creador de conjuntos

| Tipo | Campos y reglas |
|---|---|
| `SuiteComponentRef` | `Kind` (`ElementKind`: Skill, Suite, OfficialSuite) y `Slug`. Los conjuntos del jugador usan `Kind = Suite`. |
| `PlayerSuite` | `Slug`, `Name`, `Components` (lista de `SuiteComponentRef`), `Abilities` (elegidas entre las de los componentes), `CreatedAt` y `UpdatedAt`. No guarda valores calculados. |
| `SuiteCalculation` | `Cost`, `Price`, `Difficulty`, `MasteryDie` y `Classes`; cada uno con su valor o "pendiente" y el motivo. Además `Prerequisites` (requisitos de cada componente), `AvailableAbilities` (unión de las de los componentes), `Missing` (componentes que ya no existen) y `CanSave` (de 2 a 5 componentes, sin repetidos ni ciclos), con el motivo si no. |
| `SuiteCalculator` | `SuiteCalculation Calculate(IReadOnlyList<SuiteComponentValues>)`, una función pura. Cada `SuiteComponentValues` lleva los PC, precio, dificultad, dado y clases de un componente, con sus marcas de pendiente. Lo usan el creador y la prueba contra la hoja. |

Reglas de `SuiteCalculator` (research §1 a §4):

| Valor | Regla | Pendiente si… |
|---|---|---|
| PC | suma − 1 | algún componente no tiene PC numérico ("NA" o pendiente) |
| Precio | redondear(suma × 1,1), con los medios alejándose de cero | algún componente no tiene precio |
| Dificultad | mayor + 5 | algún componente no tiene dificultad |
| Dado | media de caras de los dados numéricos (mínimo 4), redondeada a la baja a d4, d6, d8, d10, d12 o d20 | algún dado está pendiente, o ninguno es numérico |
| Clases | "Todas" es neutro; si todas las restricciones son iguales, esa; si no, pendiente | hay restricciones distintas |

## Guardado en el dispositivo (`localStorage`)

Cada clave guarda un JSON con `schemaVersion`. La versión inicial es la 1.

```jsonc
// hackmaster.suites
{ "schemaVersion": 1, "suites": [
  { "slug": "mi-explorador", "name": "Mi explorador",
    "components": [ { "kind": "Skill", "slug": "rastrear" }, { "kind": "Suite", "slug": "herbalista" } ],
    "abilities": ["SAB"], "createdAt": "2026-11-18T10:00:00+01:00", "updatedAt": "2026-11-18T10:00:00+01:00" } ] }

// hackmaster.rolls
{ "schemaVersion": 1, "penetrating": false, "rolls": [
  { "formula": "2d6+3", "penetrating": false, "total": 10, "rolledAt": "2026-11-18T10:05:00+01:00",
    "groups": [ { "count": 2, "sides": 6, "sign": 1, "dice": [ { "value": 3, "penetration": false }, { "value": 4, "penetration": false } ], "subtotal": 7 } ],
    "constant": 3 } ] }
```

- **Historial**: como mucho 100 tiradas, de la más reciente a la más antigua.
- **Migraciones**: `StoredData.Migrate(json)` lleva cualquier versión anterior a la actual; una
  versión mayor que la conocida no se carga y se avisa (la aplicación es más antigua que los
  datos).
- **Nombres**: el `slug` sale del nombre con `TextNormalizer.ToSlug`, como en F001. No puede
  coincidir con el de otro conjunto de habilidades (de la hoja ni del jugador).
