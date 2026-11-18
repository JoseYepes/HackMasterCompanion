# Contrato: módulo JavaScript de los dados 3D

Archivo `src/HackMaster.Web/wwwroot/js/dice3d.js` (módulo ES). Lo importa el componente
`DiceBoardView` con `import()` solo cuando los dados 3D están activados. Importa three.js y cannon-es
desde `wwwroot/lib/`.

| Función | Entrada | Salida | Notas |
|---|---|---|---|
| `isSupported()` | — | `bool` | Hay WebGL disponible. Si es falso, el tirador va sin 3D (FR-011). |
| `create(element)` | contenedor HTML | — | Crea la escena dentro del contenedor y se adapta a su tamaño. El módulo guarda la mesa asociada al contenedor: las demás funciones reciben el mismo contenedor. |
| `roll(element, plan)` | `{ dice: [{ shape, face, wave, penetration }] }`, con `shape` como texto (`"D6"`, `"D10Tens"`…) | `Promise` que se resuelve al pararse los dados | Los dados extra (`wave` > 0) ruedan después de los de la ola anterior. La animación dura como mucho 3,6 s; si la página no se ve, un temporizador la termina igual (FR-004). |
| `finish(element)` | — | — | Termina la tirada al momento y deja los dados en su cara final (tocar la zona, nueva tirada). |
| `shownFaces(element)` | — | lista de caras | Caras de arriba de los dados en pantalla (pruebas, T016). |
| `simulate(plan, seed)` | `BoardPlan`, semilla | lista de caras de arriba | Sin dibujar. Para la prueba de SC-001. |
| `dispose(element)` | — | — | Libera la escena al salir de la página. |

**Errores**: cualquier excepción del módulo llega a .NET como `JSException`. El componente la
captura, muestra el resultado en texto y pasa a modo sin 3D con un aviso (FR-011).

**Elementos de prueba**: `dice3d-toggle` (opción «Dados 3D»), `dice-board` (zona de los dados),
`dice3d-warning` (avisos de 3D no disponible, demasiados dados o fallo).

**F008**: el aspecto, el sonido, los efectos y la vista previa amplían este contrato en
`specs/006-dados-personalizables/contracts/dice3d-look.md`.
