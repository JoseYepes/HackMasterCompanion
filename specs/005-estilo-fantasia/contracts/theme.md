# Contrato: modo normal y modo noche

Lo comparten el script de `wwwroot/index.html`, el componente `ThemeToggle` y `app.css`.

## Atributo en `<html>`

- `data-theme="light"`: modo normal (pergamino).
- `data-theme="dark"`: modo noche (taberna).

Siempre está puesto antes de que se pinte la página. `app.css` define las variables de color en
`:root` y las redefine en `html[data-theme="dark"]`.

## Preferencia guardada

- Clave de `localStorage`: `hackmaster.theme`.
- Valor: `{"schemaVersion":1,"mode":"light"}` o `{"schemaVersion":1,"mode":"dark"}`.
- Si no hay valor, si no se puede leer o si `schemaVersion` no es 1, se trata como "sin elegir":
  se usa `prefers-color-scheme` y no se escribe nada hasta que el jugador pulse el botón.

## `window.hackmasterTheme`

Definido por el script de `<head>` de `index.html`.

| Función | Devuelve | Qué hace |
|---|---|---|
| `current()` | `"light"` o `"dark"` | El modo que se ve ahora. |
| `set(mode)` | nada | Pone `data-theme`, actualiza `<meta name="theme-color">` y guarda la preferencia. Con un valor que no sea `"light"` ni `"dark"` no hace nada. |

Si `localStorage` falla (modo privado, almacenamiento lleno), `set` cambia el modo igualmente y
solo pierde el recuerdo.

Las pestañas no se avisan entre sí: si se cambia el modo en una, las demás lo toman al recargar.
Cada pestaña es coherente consigo misma (anotado en la revisión, 2026-11-18).

## `ThemeToggle`

- `data-testid="theme-toggle"`.
- `aria-label`: «Cambiar a modo noche» en modo normal; «Cambiar a modo normal» en modo noche.
- Al arrancar, pregunta `current()`; al pulsar, llama a `set` con el otro modo.
- Si `hackmasterTheme` no existe o falla (por ejemplo, un `index.html` viejo en caché), el botón no
  cambia ni muestra el aviso de error de la aplicación.
