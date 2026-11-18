# Data Model: Estilo de fantasía heroica (F007)

Esta funcionalidad no tiene datos del juego. Solo guarda una preferencia en el dispositivo.

## Preferencia de modo

| Campo | Valores |
|---|---|
| `schemaVersion` | `1` |
| `mode` | `"light"` (normal) o `"dark"` (noche) |

- La lee y la escribe solo el script del `<head>` de `index.html` (`contracts/theme.md`), porque
  tiene que aplicarse antes de que Blazor arranque.
- Sin valor, con JSON roto, con otra `schemaVersion` o con un modo desconocido: "sin elegir", se
  sigue a `prefers-color-scheme`.
- No hay clase en C#: el componente `ThemeToggle` solo pregunta el modo actual y pide el otro. Una
  clase que nadie usara para leer ni escribir sería una abstracción "por si acaso" (principio III).
  Las reglas de lectura se prueban en el navegador (`ThemeTests`).

## Guardado

| Clave | Formato | Versión |
|---|---|---|
| `hackmaster.theme` | `{"schemaVersion":1,"mode":"light" \| "dark"}` | 1 (nueva) |

No cambia el formato de `hackmaster.suites` ni de `hackmaster.rolls`.
