# Implementation Plan: Estilo de fantasía heroica (F007)

**Branch**: `005-estilo-fantasia` | **Date**: 2026-11-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/005-estilo-fantasia/spec.md`

## Summary

Nuevo aspecto para toda la aplicación, sin cambiar su contenido ni su comportamiento:

- **Modo normal, «pergamino y tinta»** (maqueta A): fondo de pergamino, tinta, granate y dorado.
- **Modo noche, «taberna»** (maqueta B): madera oscura, crema y ámbar, con las mismas letras.
- **Letras propias**: Cinzel en los títulos y EB Garamond en el texto, copiadas en `wwwroot/fonts/`
  (105 KB, licencia OFL, descarga aprobada por el usuario).
- **Colores en variables CSS**: `:root` para el modo normal y `html[data-theme="dark"]` para el
  noche; las reglas de cada pantalla solo usan variables.
- **Botón ☀/☾** en la barra (`ThemeToggle`). Un script en el `<head>` de `index.html` aplica el modo
  guardado antes de pintar, para que no haya destello.
- **Mesa de dados**: cuero granate o madera oscura, solo con CSS; el módulo 3D no cambia.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS); CSS; JavaScript en línea en `index.html`

**Primary Dependencies**: las de F003. Nuevas: dos letras (Cinzel y EB Garamond, Fontsource
5.3.0, OFL 1.1) como archivos `woff2`, sin paquetes NuGet ni npm.

**Storage**: `localStorage`, clave nueva `hackmaster.theme` (versión 1)

**Testing**: xUnit (`ThemeContrastTests`), bUnit (`ThemeTogglePageTests`),
Playwright (`ThemeTests` y `NoHorizontalOverflowTests` en los dos modos)

**Target Platform**: navegadores modernos, móvil primero (390 px), tableta (820 px) y PC

**Project Type**: aplicación web de cliente + biblioteca de dominio (sin cambios en `Domain`)

**Performance Goals**:
- cambio de modo en menos de 0,5 s (SC-003);
- primera carga como mucho un 10 % mayor (SC-005; se espera un 4 %).

**Constraints**:
- 0 avisos de compilación.
- Contrastes de FR-006 en los dos modos.
- Sin peticiones a otros sitios.
- Sin desbordamiento a 390 y 820 px.
- Ningún cambio de contenido ni de comportamiento (FR-013): no se cambian textos, `data-testid`
  ni el orden de los elementos.

**Scale/Scope**:
- `app.css` (unas 860 líneas) se reorganiza en variables.
- 1 componente nuevo (`ThemeToggle`); sin clases nuevas en C#.
- Un script de unas 30 líneas en `index.html`.
- 5 archivos de letras y 2 licencias.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F007 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Sin contenido del juego nuevo. Las maquetas usaban datos ya publicados. | PASA |
| II. Prioridad de fuentes | No aplica. | PASA |
| III. Dominio puro y arquitectura mínima | `Domain` no cambia. La preferencia la gestiona solo el script de `index.html`; no se crea una clase en C# que nadie usaría (data-model.md). Sin interfaces, capas ni proyectos nuevos. | PASA |
| IV. Test primero | Las pruebas de la preferencia, los contrastes y el botón se escriben antes y se ven fallar. Las de navegador cubren el destello, el recuerdo y el desbordamiento. | PASA |
| V. Local primero | Letras dentro de la aplicación, sin servicios externos. La preferencia guardada lleva `schemaVersion`. | PASA |
| VI. Entrega verificable | Comandos y pasos manuales en `quickstart.md`. | PASA |
| VII. Coste y seguridad | Sin Azure ni secretos. Letras OFL con versión fija, licencia copiada y SHA-256 comprobado. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright, GitHub Pages. | PASA |

Re-evaluación tras el diseño (Phase 1): sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/005-estilo-fantasia/
├── spec.md
├── plan.md              # este archivo
├── research.md          # letras, paleta, botón, mesa, pruebas, tamaño
├── data-model.md        # la preferencia y la clave hackmaster.theme
├── quickstart.md
├── contracts/
│   └── theme.md         # data-theme, localStorage y window.hackmasterTheme
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/HackMaster.Web/
├── wwwroot/
│   ├── index.html                 # script de <head>, metas color-scheme y theme-color, precarga de letras
│   ├── css/app.css                # @font-face, variables de los dos modos, reglas con variables, mesa
│   └── fonts/                     # nuevo: 5 woff2, cinzel.LICENSE, eb-garamond.LICENSE, README.md
├── Components/ThemeToggle.razor   # nuevo
└── Layout/MainLayout.razor        # + ThemeToggle en la barra

tests/HackMaster.Web.Tests/
├── ThemeContrastTests.cs          # nuevo: lee app.css
├── ThemeTogglePageTests.cs        # nuevo (bUnit)
└── E2E/
    ├── ThemeTests.cs              # nuevo
    └── NoHorizontalOverflowTests.cs  # + modo noche
```

**Structure Decision**: sin proyectos nuevos. Todo el cambio está en `HackMaster.Web` y sus
pruebas.

## Complexity Tracking

Sin desviaciones de la constitución.
