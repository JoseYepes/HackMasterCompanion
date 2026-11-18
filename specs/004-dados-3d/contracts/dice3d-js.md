# Contrato: módulo JavaScript de los dados 3D

Archivo `src/HackMaster.Web/wwwroot/js/dice3d.js` (módulo ES). Lo importa el componente
`DiceBoard` con `import()` solo cuando los dados 3D están activados. Importa three.js y cannon-es
desde `wwwroot/lib/`.

| Función | Entrada | Salida | Notas |
|---|---|---|---|
| `isSupported()` | — | `bool` | Hay WebGL disponible. Si es falso, el tirador va sin 3D (FR-011). |
| `create(element)` | contenedor HTML | `board` | Crea la escena dentro del contenedor y se adapta a su tamaño. |
| `roll(board, plan)` | `BoardPlan` en JSON | `Promise` que se resuelve al pararse los dados | Los dados extra (`wave` > 0) ruedan después de los de la ola anterior. Termina en ≤ 4 s (FR-004). |
| `finish(board)` | — | — | Termina la tirada al momento y deja los dados en su cara final (tocar la zona, nueva tirada). |
| `simulate(plan, seed)` | `BoardPlan`, semilla | lista de caras de arriba | Sin dibujar. Para la prueba de SC-001. |
| `dispose(board)` | — | — | Libera la escena al salir de la página. |

**Errores**: cualquier excepción del módulo llega a .NET como `JSException`. El componente la
captura, muestra el resultado en texto y pasa a modo sin 3D con un aviso (FR-011).

**Elementos de prueba**: `dice3d-toggle` (opción «Dados 3D»), `dice-board` (zona de los dados),
`dice3d-warning` (avisos de 3D no disponible, demasiados dados o fallo).
