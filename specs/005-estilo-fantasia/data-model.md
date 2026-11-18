# Data Model: Estilo de fantasía heroica (F007)

Esta funcionalidad no tiene datos del juego. Solo guarda una preferencia en el dispositivo.

## ThemePreference (`HackMaster.Web`)

No va en `Domain`: no es una regla del juego, sino una preferencia de la interfaz.

| Campo | Tipo | Valores |
|---|---|---|
| `SchemaVersion` | entero | `1` |
| `Mode` | `ThemeMode` | `Light` (normal) o `Dark` (noche) |

- `ThemeMode.Light` se guarda como `"light"` y `ThemeMode.Dark` como `"dark"`.
- `ThemePreference.Parse(string? json)` devuelve la preferencia, o nulo si no hay o no se entiende
  (JSON roto, otra versión, modo desconocido): nulo significa "sin elegir".
- `ThemePreference.Toggle()` devuelve la preferencia con el otro modo.
- `ToJson()` escribe exactamente el formato del contrato (`contracts/theme.md`).

La lectura que importa al arrancar la hace el script de `index.html`. `ThemePreference` existe para
que las reglas (qué se considera válido, cuál es el otro modo) tengan pruebas en C#, y el
componente la usa para el nombre accesible del botón.

## Guardado

| Clave | Formato | Versión |
|---|---|---|
| `hackmaster.theme` | `{"schemaVersion":1,"mode":"light" \| "dark"}` | 1 (nueva) |

No cambia el formato de `hackmaster.suites` ni de `hackmaster.rolls`.
