# Data Model: Datos y consulta de habilidades (F001)

Tipos de `HackMaster.Domain` (identificadores en inglés). Son inmutables y los comparten el
importador, que los escribe, y la aplicación, que los lee. Los nombres de campo en JSON usan
`camelCase`.

## Valores comunes

| Tipo | Descripción |
|---|---|
| `Slug` (texto) | Identificador para direcciones: nombre normalizado, espacios y signos → "-". Único dentro de su tipo de elemento (habilidad, conjunto de habilidades, conjunto oficial, estilo); dos nombres del mismo tipo con igual `slug` son un **error** de importación. |
| `Ability` (enumeración) | `FUE`, `DES`, `CON`, `INT`, `SAB`, `CAR`. Un valor que no esté en la lista (la hoja contiene "ASP") no se descarta: va a `raw` y el campo a `pending`. |
| `BookRef` | `acronym` (p. ej. `MJ`, `CG`, `GrG`, `HJ8`) y `page` (texto anulable: lo que sigue al acrónimo, tal como está en la fuente; "HJ 4.05" da `HJ` y `4.05`). Una celda como "MJ 329 / GMG 71" da dos referencias. |
| `ElementKind` (enumeración) | `Skill`, `Suite`, `OfficialSuite`, `Style`. Tipo de elemento al que apunta un componente o una duda. |
| `Pending` | `pending`: lista de nombres de campo pendientes de verificar. `raw`: diccionario campo → texto original de la hoja. |

## Skill (habilidad)

| Campo | Tipo | Origen (pestaña "Habilidades") | Reglas |
|---|---|---|---|
| `slug` | Slug | columna A | único |
| `name` | texto | A | único sin distinguir acentos ni mayúsculas; se conserva la grafía de la hoja |
| `searchName` | texto | A | `TextNormalizer.Normalize(name)` |
| `type` | texto | B | uno de los tipos presentes en la hoja (12 hoy) |
| `cost` | entero anulable | C | PC; "NA" → nulo; "1 R" → nulo + `pending` + `raw` |
| `abilities` | lista de Ability | D | separadas por "," o "/" |
| `masteryDie` | texto anulable | E | normalizado a `d4`, `d6`, `d8`, `d10`, `d12`, `d20`, `NA` o `*`; "8" y "d8" → `d8`; otro valor (5, 3, 2) → nulo + `pending` + `raw` |
| `prerequisite` | Prerequisite | F | ver más abajo |
| `price` | entero anulable | G | po; "250 po" → 250; "NA" → nulo |
| `difficulty` | entero anulable | H | %; admite negativos; una fracción (0,1) se convierte a porcentaje (10) |
| `books` | lista de BookRef | I | al menos una; si no hay → `pending` |
| `classes` | lista de texto | J | "Todas" o grupos y clases tal como figuran |
| `summary` | texto anulable | pestaña "Descripciones", columna C | vacío → nulo + `pending` |
| `searchSummary` | texto anulable | — | `Normalize(summary)` |
| `pending`, `raw` | Pending | — | — |

La columna D de "Descripciones" (texto de manual) **no existe** en el modelo ni se lee.

## Prerequisite (requisito)

| Campo | Tipo | Reglas |
|---|---|---|
| `segments` | lista de `PrerequisiteSegment` | vacía si no hay requisito ("N"); concatenados reproducen el texto de la hoja |

`PrerequisiteSegment`:

| Campo | Tipo | Reglas |
|---|---|---|
| `text` | texto | fragmento tal como está en la hoja |
| `kind` | `Text`, `Skill`, `Suite`, `OfficialSuite`, `Condition`, `Unverified` | `Text` para separadores ("," " o "); `Unverified` para "(sin verificar)" y "?". Un nombre se busca en este orden: habilidad, conjunto de habilidades, conjunto oficial |
| `target` | Slug anulable | obligatorio si `kind` es `Skill`, `Suite` u `OfficialSuite`; debe existir |

Validación en la importación: una parte que no es habilidad, conjunto, condición
reconocida ni anotación de duda es un **error** (FR-005).

## SkillSuite (conjunto)

| Campo | Tipo | Origen (pestaña "Conjuntos de habilidades") | Reglas |
|---|---|---|---|
| `slug`, `name`, `searchName` | — | columna B de la fila de título del bloque | únicos dentro de cada `kind`; un `Custom` y un `Official` pueden compartir nombre ("Cartógrafo") |
| `kind` | `Custom` u `Official` | bloque en el que aparece | 36 `Custom` (reglas de HJ 4.05), 23 `Official` |
| `cost` | entero anulable | fila de título | tal como figura; no se recalcula en F001 |
| `abilities` | lista de Ability | fila de título | — |
| `masteryDie` | texto anulable | fila de título | mismas reglas que en Skill. En los `Custom` la hoja no lo da (se calcula con las reglas de HJ 4.05, F002): queda nulo **sin** marcarse pendiente |
| `price`, `difficulty` | entero anulable | fila de título | "?" o vacío → nulo + `pending` |
| `books` | lista de BookRef | fila de título | — |
| `classes` | lista de texto | fila de título | solo en los `Custom` |
| `components` | lista de `SuiteComponent` | filas del bloque | vacía en los `Official` → `pending` incluye `components` |
| `prerequisite` | Prerequisite | columna de requisito de los `Official` | si es "Las de las habilidades que…" se guarda como `Text` |
| `pending`, `raw` | Pending | — | — |

`SuiteComponent`: `name` (texto de la hoja), `kind` (`Skill`, `Suite` u `OfficialSuite`) y `target`
(Slug). Debe resolver a una habilidad o a otro conjunto, con el mismo orden de búsqueda que los
requisitos; si no, **error**. Los valores por componente que
repite la pestaña (PC, precio…) no se guardan: la referencia es la pestaña "Habilidades".

Relación inversa calculada al cargar: para cada habilidad, los conjuntos que la contienen
(FR-019).

## CombatStyle (estilo de combate)

`slug`, `name`, `searchName`, `abilities`, `masteryDie`, `prerequisite`, `price`, `difficulty`,
`books`, `pending`, `raw`. Sin `cost` (la fuente indica "NA"). Origen: bloque "Estilos de combate"
de la pestaña de conjuntos.

## Doubt (duda)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | texto | estable: origen + slug del elemento + campo |
| `origin` | `Sheet` o `Import` | `Sheet`: escrita por el mantenedor en la pestaña "Dudas"; `Import`: detectada al importar |
| `subject` | texto | nombre del elemento tal como está escrito |
| `target` | {`kind`, `slug`} | elemento afectado; `kind` es `Skill`, `Suite`, `OfficialSuite` o `Style`. En `Sheet`, cada fila nombra un único elemento con su nombre exacto, que debe existir (si no, error); una duda sobre varios elementos ocupa una fila por elemento |
| `source` | texto anulable | columna "Fuente" |
| `question` | texto | la duda; en `Import`, generada ("Dado de maestría «5» no es un dado existente") |
| `provisional` | texto anulable | columna "Qué he hecho" |

## Archivos generados

| Archivo | Contenido |
|---|---|
| `data/skills.json` | `{ "schemaVersion": 1, "skills": [Skill…] }` |
| `data/suites.json` | `{ "schemaVersion": 1, "suites": [SkillSuite…] }` |
| `data/styles.json` | `{ "schemaVersion": 1, "styles": [CombatStyle…] }` |
| `data/doubts.json` | `{ "schemaVersion": 1, "doubts": [Doubt…] }` |
| `docs/dudas.md` | las mismas dudas, legibles, con aviso de "archivo generado" |

Orden dentro de cada lista: por `searchName` (dudas: por `origin` y después `subject`).

## GameData (en la aplicación)

Carga única al arrancar: listas de `Skill`, `SkillSuite`, `CombatStyle` y `Doubt`, índices por
`slug`, y las relaciones inversas (habilidad → conjuntos, elemento → dudas). Si `schemaVersion` no
es el esperado, la aplicación muestra un error en lugar de datos parciales.

## SkillQuery (consulta de habilidades)

Entrada de `SkillQuery.Apply(skills, criteria)`:

| Campo de `SkillCriteria` | Tipo | Comportamiento |
|---|---|---|
| `text` | texto | normalizado; coincide si está contenido en `searchName` o en `searchSummary` |
| `types`, `books`, `classes` | conjuntos de texto | vacío = sin filtro; dentro de uno basta un valor; `classes` incluye siempre las habilidades de "Todas"; `books` compara el acrónimo |
| `priceMin`, `priceMax`, `difficultyMin`, `difficultyMax` | entero anulable | con algún límite activo, las habilidades sin valor quedan excluidas |
| `sort` | `Name`, `Cost`, `Price`, `Difficulty` + sentido | los valores nulos van al final |

Salida: lista de `SkillMatch` (`skill`, `matchedBy`: `Name` o `Summary`, `snippet`: fragmento del
resumen si coincide solo por resumen). Con `text` no vacío, las coincidencias por nombre preceden
a las de resumen y, dentro de cada grupo, se aplica `sort`.

`SkillFacets.From(skills)` devuelve las opciones de cada filtro a partir de los datos (FR-017).
