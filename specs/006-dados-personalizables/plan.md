# Implementation Plan: Dados personalizables y sonido (F008)

**Branch**: `006-dados-personalizables` | **Date**: 2026-11-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/006-dados-personalizables/spec.md`

## Summary

Los dados 3D de F003 pasan a ser personalizables, suenan y celebran las penetraciones:

- **Aristas redondeadas** solo en la malla que se ve, con los planos de las caras en su sitio: la
  física y la forma de forzar la cara de F003 no cambian (research §1).
- **11 materiales** generados con código (sin descargas), con color del dado y de los números
  elegibles y aviso de contraste (research §2 y §6).
- **Sonido con grabaciones CC0** (Kenney y OpenGameArt) en `wwwroot/sounds`, que suenan con Web Audio
  en los choques de la simulación (research §3; antes era sintetizado).
- **Efecto al penetrar** (explosión, fuego, sangre o ninguno) y **aura** en los dados extra; el
  dominio indica el dado padre (research §4).
- **Calidad baja** sin texturas, reflejos ni transmisión.
- **Panel «Aspecto de los dados»** en `/dados` con vista previa; todo se guarda en el historial
  (versión 3, con migración).

## Technical Context

**Language/Version**: C# sobre .NET 10 (LTS); JavaScript ES2022 en `dice3d.js`

**Primary Dependencies**: las de F003 (three.js 0.187.0, cannon-es 0.20.0). **Ninguna nueva** y
ninguna descarga: texturas y sonidos se generan con código.

**Storage**: `localStorage`, `hackmaster.rolls` pasa a la versión 3 (`look`)

**Testing**: xUnit (`DiceLookTests`, `StoredDataTests`, `DiceBoardTests`), bUnit
(`DiceLookPanelTests`), Playwright (`Dice3DTests` ampliada, `DiceLookTests` E2E); manual en el
móvil del usuario

**Target Platform**: navegadores con WebGL; sin WebGL, modo sin 3D como en F003

**Project Type**: aplicación web de cliente + biblioteca de dominio

**Performance Goals**:
- tirada en menos de 4 s con sonido y efectos (SC-004);
- fluida en el móvil del usuario con calidad alta (SC-003, manual);
- sin 3D, la descarga crece menos de un 1 % (SC-005).

**Constraints**:
- 0 avisos.
- La cara de arriba coincide con el tirador en el 100 % (SC-001).
- Sin desbordamiento a 390 y 820 px.
- Contraste de F007 en los controles nuevos.
- Diseños propios, sin copiar ningún juego (FR-015).

**Scale/Scope**:
- `dice3d.js` crece unas 400 líneas.
- 1 componente nuevo (`DiceLookPanel`).
- 3 enumeraciones y 1 registro en el dominio.
- Versión 3 del historial.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación en F008 | Resultado |
|---|---|---|
| I. Contenido sin inventar | Sin contenido de juego nuevo. «Dados que explotan» es como el usuario llama a la penetración en la mesa. | PASA |
| II. Prioridad de fuentes | No aplica. | PASA |
| III. Dominio puro y arquitectura mínima | Reglas puras en `Domain` (`DiceLook`, contraste, `Parent`), sin dependencias. Ni interfaces ni capas nuevas: el aspecto se pasa al módulo existente. | PASA |
| IV. Test primero | Dominio y panel con pruebas antes del código. El 3D y el sonido se prueban en Playwright con funciones de prueba del módulo. | PASA |
| V. Local primero | Todo generado con código, sin descargas ni servicios. Historial versión 3 con migración probada. | PASA |
| VI. Entrega verificable | Comandos en `quickstart.md`; comprobación manual en el móvil. | PASA |
| VII. Coste y seguridad | Sin Azure, sin secretos y sin dependencias nuevas. | PASA |
| Restricciones técnicas | .NET 10, Blazor WASM, xUnit + bUnit + Playwright, GitHub Pages. | PASA |

Re-evaluación tras el diseño (Phase 1): sin cambios.

## Project Structure

### Documentation (this feature)

```text
specs/006-dados-personalizables/
├── spec.md
├── plan.md               # este archivo
├── research.md           # redondeo, materiales, sonido, efectos, guardado, contraste, pruebas
├── data-model.md         # DiceLook, enumeraciones, Parent y el historial versión 3
├── quickstart.md
├── contracts/
│   └── dice3d-look.md    # look, preview, sonidos y efectos del módulo
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
src/HackMaster.Domain/
├── DiceLook.cs                     # nuevo: DiceMaterial, PenetrationEffect, DiceQuality, DiceLook
├── DiceBoard.cs                    # + Parent en BoardDie
└── StoredData.cs                   # RollsVersion 3, Look, migración 2 → 3

src/HackMaster.Web/
├── Components/DiceLookPanel.razor  # nuevo: materiales, colores, efecto, sonido, calidad, vista previa
├── Components/DiceBoardView.razor  # pasa el aspecto y parent al módulo; desbloquea el audio
├── Pages/Dice.razor                # panel y guardado del aspecto
├── wwwroot/js/dice3d.js            # redondeo, materiales, sonido, efectos, aura, vista previa
└── wwwroot/css/app.css             # estilos del panel con las variables de F007

tests/
├── HackMaster.Domain.Tests/DiceLookTests.cs, StoredDataTests.cs, DiceBoardTests.cs
└── HackMaster.Web.Tests/DiceLookPanelTests.cs, E2E/Dice3DTests.cs, E2E/DiceLookTests.cs
```

**Structure Decision**: sin proyectos nuevos.

## Complexity Tracking

Sin desviaciones de la constitución.
