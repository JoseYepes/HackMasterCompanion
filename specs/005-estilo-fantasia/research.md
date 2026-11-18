# Research: Estilo de fantasía heroica (F007)

Decisiones técnicas del plan. Cada una dice qué se eligió, por qué y qué se descartó.

## 1. Letras

**Decisión**: Cinzel (600 y 700) para los títulos y EB Garamond (400, 600 y cursiva 400) para el
texto, en `woff2` con solo los caracteres latinos, copiadas en `wwwroot/fonts/` (versión 5.3.0 de
Fontsource, descargada de jsDelivr con permiso del usuario el 2026-11-18). Son 105 KB en total.

**Por qué**:
- Son las letras de la maqueta A que eligió el usuario.
- Licencia SIL OFL 1.1: se pueden incluir en la aplicación y redistribuir.
- Los archivos de Fontsource ya vienen recortados a latín y en `woff2`. El repositorio de Google
  Fonts solo tiene TTF completos (más de 1 MB en EB Garamond) y recortarlos exige herramientas
  que no hay en el equipo.
- El SHA-256 de los 7 archivos (5 letras y 2 licencias) coincide con el que publica jsDelivr.
- El subconjunto latino incluye ñ, vocales acentuadas, ü, ¿, ¡, « y ».

**Descartado**:
- Cargar desde Google Fonts: depende de un tercero (FR-008) y envía datos del visitante a Google.
- Letras del sistema: no dan el aspecto de la maqueta.
- Una sola letra variable: más pesada que los 5 pesos que se usan.

**Cómo se usan**: `@font-face` en `app.css` con `font-display: swap` (FR-014) y letras de reserva
parecidas: `Cinzel, "Trajan Pro", Georgia, serif` y `"EB Garamond", Garamond, Georgia, serif`.
Se precargan (`<link rel="preload">`) solo Cinzel 600 y EB Garamond 400, que aparecen en todas las
pantallas.

## 2. Colores y modos

**Decisión**: todos los colores pasan a variables CSS en `:root` (modo normal). El modo noche
redefine las mismas variables bajo `html[data-theme="dark"]`. Las reglas de cada pantalla solo
usan variables, nunca colores sueltos.

**Paleta** (contrastes calculados con la fórmula de WCAG 2.1):

| Uso | Normal | Noche | Contraste normal / noche |
|---|---|---|---|
| Fondo de página | `#f3e7cc` (pergamino) | `#1d1611` (madera) | — |
| Tarjetas | `#f8efd9` | `#2a2019` | — |
| Texto | `#3b2a1a` (tinta) | `#ecdcbf` (crema) | 11,2 / 13,2 sobre el fondo |
| Texto secundario | `#6b5440` | `#bfa985` | 6,2 / 7,0 sobre tarjeta |
| Color principal, títulos y enlaces | `#5a1f1b` (granate) | `#f2b544` (ámbar) | 10,4 / 9,8 sobre el fondo |
| Barra de navegación | `#5a1f1b` con texto `#f3e6c8` | `#120d09` con texto `#d9c49c` | 10,3 / 11,3 |
| Enlace activo en la barra | `#f0c75e` | `#f2b544` | 7,9 / 9,8 |
| Botón principal | `#5a1f1b` con texto `#f3e6c8` | `#b8862e` con texto `#1d1611` | 10,3 / 5,5 |
| Bordes de controles | `#8a6a3a` | `#8a6a36` | 4,1 / 3,2 (mínimo 3) |
| Aviso | `#f6e3a8` con texto `#5a3d00` | `#3d3010` con texto `#f5d98b` | 7,8 / 9,3 |
| Error | `#8b1a1a` | `#ff9b85` | 8,1 / 7,8 sobre tarjeta |
| Mesa de dados | cuero `#4a1a16` | madera `#3a2717` | dados marfil 12,3 / 12,0; dados extra dorados `#e8b54a` (antes rojos `#b42318`, 2,2:1 sobre el cuero) |

El borde de la maqueta B (`#7a5a2a`) daba 2,8 sobre el fondo de noche: se aclara a `#8a6a36`.

**Descartado**:
- Clases distintas por pantalla para cada modo: duplicaría el CSS y se olvidarían casos.
- `@media (prefers-color-scheme)` como única fuente: el jugador no podría elegir (FR-004).

## 3. Botón de modo y preferencia

**Decisión**:
- Un componente `ThemeToggle` en la barra de `MainLayout`, con un icono SVG (sol o luna) y un
  nombre accesible: «Cambiar a modo noche» o «Cambiar a modo normal». Mide al menos 44 × 44 px.
- La preferencia se guarda en `localStorage` con la clave `hackmaster.theme`, como JSON con
  `schemaVersion` (principio V): `{"schemaVersion":1,"mode":"dark"}`.
- Un script corto dentro de `index.html`, en `<head>` y antes de la hoja de estilos, lee la
  preferencia y pone `data-theme` en `<html>` antes de pintar nada. Sin preferencia, usa
  `prefers-color-scheme` (FR-004, FR-005). Expone `window.hackmasterTheme` con `current()` y
  `set(mode)` para el componente (contrato en `contracts/theme.md`).
- `<meta name="color-scheme" content="light dark">` y `<meta name="theme-color">`, que cambia con
  el modo, para que la barra del navegador del móvil acompañe.

**Por qué**:
- El script en `<head>` es lo único que evita el destello: Blazor tarda en arrancar y, si el modo
  lo pusiera Blazor, la página se vería un instante en modo normal (FR-005).
- Iconos SVG y no los caracteres ☀ y ☾: las letras elegidas no los tienen y algunos móviles los
  dibujan como emoji de colores o como un cuadro vacío.

**Descartado**:
- Guardar la preferencia a través de `BrowserStore` desde Blazor: el script de `<head>` tiene que
  leerla antes de que Blazor exista, así que la escribe y la lee el propio script.
- Tres opciones (normal, noche, automático): descartado por el usuario (aclaración 2).

## 4. Mesa de dados

**Decisión**: solo CSS. El lienzo 3D ya es transparente (`alpha: true` en `dice3d.js`) y el tapete
verde actual es el fondo CSS de `.dice-board`. Se cambia por:

- **Normal**: cuero granate (`#4a1a16`) con un degradado radial más claro en el centro y una
  costura dorada en el borde.
- **Noche**: madera oscura (`#3a2717`) con vetas hechas con `repeating-linear-gradient`.

**Por qué**: la mesa no toca el módulo 3D, no se descargan imágenes y cambiar de modo a mitad de
tirada no afecta a la animación (US3, escenario 3).

La revisión independiente (T018) vio que los dados extra por penetración, rojos (`#b42318`), casi
desaparecían sobre el cuero (2,2:1). Se cambió esa única constante de `dice3d.js` a dorado
(`#e8b54a`, números `#2a1608`). `ThemeContrastTests` lee ahora los colores del propio módulo y los
compara con las dos mesas, en vez de una variable `--die` que nada usaba.

**Descartado**: texturas de imagen o generadas en el módulo 3D (más peso; quedan para F008).

## 5. Pruebas

| Qué | Cómo |
|---|---|
| Contrastes (FR-006, SC-002) | Prueba xUnit en `Web.Tests` que lee las variables de `app.css` de los dos modos y comprueba cada par de la tabla de §2 con la fórmula de WCAG. |
| Preferencia (FR-004) | Playwright: valores rotos, de otra versión o con un modo desconocido se tratan como "sin elegir"; `localStorage` que falla no impide cambiar de modo. |
| Botón (FR-003) | bUnit: nombre accesible según el modo, el clic llama a `hackmasterTheme.set` y cambia el nombre. |
| Sin destello y recordar (FR-005, US2) | Playwright: con la preferencia guardada, `data-theme` ya está puesto en `DOMContentLoaded`; tras recargar sigue el modo; sin preferencia sigue a `prefers-color-scheme`. |
| Cambio rápido (SC-003) | Playwright: del clic al cambio de color del fondo, menos de 500 ms. |
| Letras propias (FR-008, SC-004) | Playwright: ninguna petición sale del origen de la aplicación al abrir las 12 pantallas; `document.fonts` tiene Cinzel y EB Garamond cargadas. |
| Desbordamiento (FR-011, SC-006) | Se amplía `NoHorizontalOverflowTests` a los dos modos. |
| Botones de 44 px (FR-010) | Playwright: el botón de modo mide al menos 44 × 44 px a 390 px. |
| Aspecto (SC-007) | A mano: el usuario compara con las maquetas en su móvil. |

## 6. Tamaño (SC-005)

Antes del estilo, la primera carga comprimida era de unos 2837 KB (medido en F003).

**Medido el 2026-11-18 (T016)**, publicando en Release `main` (a88806b) y la rama y comparando
los archivos comprimidos con Brotli:

| Archivo | Antes | Después |
|---|---|---|
| `app.css` | 2,7 KB | 4,1 KB |
| `index.html` | 0,7 KB | 1,3 KB |
| `HackMaster.Web` | 103,1 KB | 103,8 KB |
| Letras (`fonts/`, ya comprimidas) | — | 102,5 KB como mucho |

En total, unos 106 KB más: un **3,7 %**, por debajo del 10 % de SC-005. La cursiva solo se
descarga si alguna pantalla la usa, así que lo normal es algo menos.

Las letras van en `wwwroot/fonts/` y no en `lib/`, porque la prueba
`Without_3D_Nothing_Of_The_3D_Part_Is_Downloaded__FR016` (F003) vigila que nada de `lib/` se
descargue sin activar el 3D, y las letras se cargan en todas las páginas.
