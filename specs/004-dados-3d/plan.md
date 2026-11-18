# Implementation Plan: Dados 3D (F003)

**Branch**: `004-dados-3d` | **Date**: 2026-11-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-dados-3d/spec.md`

## Summary

Dados 3D con física en el tirador `/dados`:

- **El resultado no cambia de origen.** Lo sigue decidiendo el tirador de F002 (`IRandom`).
- **Dominio.** Una función pura de `Domain`, `DiceBoard.Plan`, traduce cada tirada a la lista de
  dados que se dibujan: forma, cara, olas de penetración, d100 como dos d10 y el límite de 20.
- **JavaScript.** Un módulo propio, `wwwroot/js/dice3d.js`, usa three.js 0.187.0 y cannon-es
  0.20.0. Simula la física sin dibujar, cambia los números del dado para que la cara que queda
  arriba sea la pedida y reproduce la simulación en pantalla.
- **Carga.** Las librerías se copian en `wwwroot/lib/` y se importan solo al activar «Dados 3D»,
  que empieza desactivado.
- **Respaldo.** Sin WebGL, o si el módulo falla, el tirador funciona como en F002 y lo avisa.

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS); JavaScript ES2022 (módulo ES) para el 3D

**Primary Dependencies**:

- las de F002;
- **nuevas, en el navegador**: three.js 0.187.0 y cannon-es 0.20.0 (MIT), copiadas en
  `wwwroot/lib/` sin npm.

`Domain` sigue sin dependencias.

**Storage**: `localStorage` (`hackmaster.rolls` pasa a la versión 2 con `dice3d`)

**Testing**: xUnit (`DiceBoardTests`, migración), bUnit (página con JavaScript simulado),
Playwright (`Dice3DTests`: 1 000 tiradas simuladas, duración, desbordamiento); fotogramas por
segundo a mano

**Target Platform**: navegadores modernos con WebGL; sin WebGL, modo sin 3D

**Project Type**: aplicación web de cliente + biblioteca de dominio

**Performance Goals**:

- 30 fotogramas por segundo o más con 6 dados en un móvil medio (SC-003);
- tirada en 4 s o menos (SC-004);
- el resto de páginas no crece en descarga (SC-005).

**Constraints**:

- 0 avisos de compilación.
- Sin desbordamiento a 390 y 820 px.
- El 3D no se descarga si no se activa.
- La cara mostrada coincide con el dominio en el 100 % de los casos.

**Scale/Scope**: 1 módulo JS (unas 400 líneas), 1 componente nuevo, 2 librerías copiadas (unos
2,4 MB sin comprimir y unos 500 KB comprimidos)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F003 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Sin contenido de juego nuevo; el d100 como dos d10 es como se tira en la mesa (aclarado con el usuario). | PASA |
| II. Prioridad de fuentes | No aplica. | PASA |
| III. Dominio puro y arquitectura mínima | Las reglas de qué se dibuja van en `Domain` (`DiceBoard`), sin dependencias. El azar sigue en `IRandom`. No se crea `IDiceRenderer` aunque `PRODUCTO.md` lo proponga: no hay un segundo caso de uso (research §5). Sin proyectos nuevos. | PASA |
| IV. Test primero | TDD en `DiceBoard` y en la migración. El JavaScript se prueba con Playwright (SC-001), que es la única forma de probar la física real. | PASA |
| V. Local primero | Las librerías van en el repositorio, sin CDN: funcionan sin conexión. El formato guardado pasa a la versión 2 con migración probada. | PASA |
| VI. Entrega verificable | Comandos en `quickstart.md`; comprobación manual de fotogramas documentada. | PASA |
| VII. Coste y seguridad | Sin Azure ni secretos. Las librerías son MIT, con versión fija y licencia copiada. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright, GitHub Pages. | PASA |

Re-evaluación tras el diseño (Phase 1): sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/004-dados-3d/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/dice3d-js.md
├── checklists/requirements.md
└── tasks.md                      (lo crea /speckit-tasks)
```

### Source Code (repository root)

```text
src/
├── HackMaster.Domain/
│   ├── DiceBoard.cs                DieShape, BoardDie, BoardPlan, Plan()
│   └── StoredData.cs               versión 2 de hackmaster.rolls (dice3d) y migración
└── HackMaster.Web/
    ├── Components/DiceBoard.razor  importa dice3d.js, tira, espera, cae al modo sin 3D
    ├── Pages/Dice.razor            opción «Dados 3D», muestra el texto al acabar
    └── wwwroot/
        ├── js/dice3d.js            módulo propio (contracts/dice3d-js.md)
        └── lib/                    three.module.js, three.core.js, cannon-es.js, LICENSE y versiones
tests/
├── HackMaster.Domain.Tests/DiceBoardTests.cs, StoredDataTests.cs (migración)
└── HackMaster.Web.Tests/
    ├── DicePageTests.cs            bUnit con JSInterop simulado
    └── E2E/Dice3DTests.cs          Playwright
```

**Structure Decision**: sin proyectos nuevos. JavaScript propio en `wwwroot/js` y librerías de
terceros en `wwwroot/lib`, con su licencia.

## Orden de trabajo

1. **Dominio**: `DiceBoard.Plan` y la migración del formato guardado (TDD).
2. **Librerías**: descargar three.js y cannon-es (con permiso del usuario) y anotar las versiones.
3. **Módulo `dice3d.js`**:
   - `simulate`, primero, para la prueba de SC-001;
   - después `create`, `roll`, `finish` y `dispose`.
4. **Componente y página**: opción «Dados 3D», componente `DiceBoard`, avisos y respaldo.
5. **Playwright**, medición de SC-003 y SC-005 a mano y revisión independiente.

## Riesgos

| Riesgo | Mitigación |
|---|---|
| La física no es determinista entre la simulación y la reproducción | Paso fijo y la misma semilla en las dos; si falla, se reproduce la grabación de posiciones en vez de volver a simular |
| Rendimiento bajo en móvil | Sombras sencillas, como mucho 20 dados, menos resolución en pantallas densas; si no llega a 30 fotogramas por segundo, bajar calidad |
| three.js cambia su API en versiones futuras | Versión fija y copiada; actualizarla es un cambio explícito |
| Tamaño de las librerías | Solo se descargan al activar el 3D; se mide SC-005 |

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Dependencias nuevas en el navegador (three.js, cannon-es) | La spec exige dados con física; hacerlo a mano sería mucho más código y menos fiable | Las librerías de dados ya hechas no permiten forzar la cara (dice-box) o están abandonadas (dice-box-threejs); research §1 |
