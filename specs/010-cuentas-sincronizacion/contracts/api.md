# Contrato de la API (F006)

Base: `https://{static-web-app}/api`. Todas las peticiones llevan `Authorization: Bearer {token}`
(token de acceso de External ID, ámbito `api://…/data`), salvo que la prueba T00x obligue a usar
otra cabecera. Respuestas JSON. CORS solo para el origen de GitHub Pages y `http://localhost:*`.

| Código | Cuándo |
|---|---|
| 401 | sin token, token caducado o firma, emisor o audiencia incorrectos |
| 403 | correo no invitado (`{ "error": "not-invited" }`), o ruta de administración sin ser administrador |
| 400 | JSON no válido, `id` distinto del de la ruta, tipo desconocido, o sin `If-Match` ni `If-None-Match: *` |
| 409 | `If-Match` no coincide, o ya existe con `If-None-Match: *`; el cuerpo es el documento de la nube |
| 413 | documento mayor de 2 MB |
| 503 | Cosmos no responde o cuota agotada (`{ "error": "unavailable" }`) |

## `GET /api/me`
200 `{ "name": "…", "email": "…", "isAdmin": false }`.

## `GET /api/data?since={segundos Unix}`
200 `{ "items": [Doc…], "serverTime": 1795000000 }`. `serverTime` es la hora de la nube en segundos
Unix; se pasa tal cual como `since` en la siguiente petición. Sin `since`, todo. Incluye las marcas de borrado.
`Doc` = `{ id, kind, schemaVersion, updatedAt, deleted, payload, etag }`.

## `PUT /api/data/{id}`
Cabecera `If-Match: {etag}` o `If-None-Match: *` si es nuevo. Cuerpo: `Doc` sin `etag`. El
`userId` lo pone la API. 200 con el `Doc` guardado (nuevo `etag`), o 409.

## `GET /api/export`
200, archivo `hackmaster-cuenta.json`: `{ "schemaVersion": 1, "exportedAt", "items": [Doc…] }`.

## `DELETE /api/account`
Borra todos los documentos de `userdata` de la cuenta. 204. La exportación no incluye las marcas de borrado.

## Administración (solo correos de `ADMIN_EMAILS`)
- `GET /api/invitations` → 200 `[{ email, invitedAt }]`
- `PUT /api/invitations/{email}` → 204
- `DELETE /api/invitations/{email}` → 204. Las sesiones abiertas reciben 403 en la siguiente
  petición.
