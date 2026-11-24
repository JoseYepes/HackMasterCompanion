# Modelo de datos: F006

## En la nube (Cosmos DB, base de datos `hackmaster`)

### `userdata` (partición `/userId`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id` | texto | `{kind}:{itemId}`; `itemId` es el `Id` de la ficha, el `Slug` del conjunto o `main` para los documentos únicos |
| `userId` | texto | `oid` del token; lo pone la API, nunca el cliente |
| `kind` | texto | `character`, `suite`, `rolls`, `theme` |
| `schemaVersion` | entero | el de `StoredData` para ese tipo (`CharactersVersion`, `SuitesVersion`, `RollsVersion`; `theme` = 1) |
| `updatedAt` | fecha (ISO 8601, UTC) | hora del cambio en el dispositivo; decide quién gana un conflicto |
| `deleted` | booleano | marca de borrado; `payload` vacío |
| `payload` | texto (JSON) | la ficha, el conjunto, el `RollHistoryFile` o `{ "schemaVersion": 1, "mode": "light" \| "dark" }` |
| `_etag` | texto | versión de Cosmos; se usa en `If-Match` |

Límite: 2 MB por documento (el del historial ya está acotado en el dispositivo).

### `invitations` (partición `/email`)

| Campo | Tipo | Reglas |
|---|---|---|
| `id`, `email` | texto | correo en minúsculas y sin espacios |
| `invitedAt` | fecha | |
| `invitedBy` | texto | `oid` del administrador |

Los administradores son las cuentas (`oid`) del ajuste `ADMIN_ACCOUNT_IDS` de la API, que no se
guarda en el repositorio. Se usa el identificador y no el correo, porque el correo de un token no
siempre está verificado. `invitedBy` guarda el `oid` de quien invita.

## En el dispositivo

| Clave | Contenido |
|---|---|
| `hackmaster.characters`, `hackmaster.suites`, `hackmaster.rolls`, `hackmaster.theme` | sin sesión, como hasta ahora |
| `hackmaster.u.{oid}.characters`, `.suites`, `.rolls` | copia local de la cuenta (el modo de pantalla sigue en `hackmaster.theme`, research.md R5) |
| `hackmaster.u.{oid}.sync` | `SyncState`: `schemaVersion` 1, etiquetas conocidas por documento, cambios pendientes |

### `SyncState` (nuevo, en `HackMaster.Domain`)

- `Known`: `id` → `etag` de la última versión recibida de la nube.
- `Pending`: lista de `PendingChange(id, kind, updatedAt, deleted, payload)`. Si se cambia dos veces
  el mismo `id`, queda solo el último cambio.

### Reglas de fusión (`SyncMerge`, puras, con test)

1. Lo que la nube trae y no está pendiente sustituye a la copia local.
2. Si un `id` pendiente choca con la nube (409), gana el de `updatedAt` mayor. Si hay empate, gana
   la nube. Se devuelve un aviso con el nombre del elemento descartado.
3. Una marca de borrado más reciente que la otra versión borra el elemento; una edición más
   reciente que el borrado lo recupera.
4. Los documentos de una `schemaVersion` anterior se migran con `StoredData` al leerlos. Los de una
   posterior o ilegibles no se borran: se ignoran y se avisa.
