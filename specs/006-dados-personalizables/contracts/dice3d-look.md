# Contrato: aspecto, sonido y efectos del módulo de dados 3D

Amplía `specs/004-dados-3d/contracts/dice3d-js.md`. El módulo sigue en
`src/HackMaster.Web/wwwroot/js/dice3d.js` y solo se carga con los dados 3D activados.

## Objeto `look`

```js
{ material: 'Marble', color: '#2f5a46', ink: '#f1e5c4',
  effect: 'Explosion', sound: true, quality: 'High' }
```

Valores como en `data-model.md`. Un valor desconocido se trata como el de por defecto.

## Funciones nuevas o cambiadas

| Función | Entrada | Salida | Notas |
|---|---|---|---|
| `roll(element, plan, look)` | como en F003, más `look`; cada dado del plan lleva `parent` (índice o `null`) | `Promise` | Dibuja con el aspecto; suena si `look.sound`; lanza el efecto de penetración en la posición del dado padre cuando aparece un dado extra; aura en los dados extra. Sigue durando como mucho 3,6 s. |
| `preview(element, look)` | contenedor, aspecto | — | Vista previa: un d20 girando con ese aspecto. Llamarla otra vez cambia el aspecto sin recrear la escena. |
| `stopPreview(element)` | contenedor | — | Para la animación y libera la vista previa. |
| `unlockAudio()` | — | — | Crea o reanuda el contexto de audio; se llama al pulsar «Tirar». Nunca lanza excepciones. |

## Funciones de prueba

| Función | Salida |
|---|---|
| `shownDice(element)` | como en F003, más `material`, `textured`, `transmission` y `aura` (bool) de cada dado |
| `heardSounds(element)` | sonidos programados en la última tirada: `[{ at, volume, kind }]` (`kind`: material o efecto) |
| `shownEffects(element)` | efectos lanzados en la última tirada: `[{ effect, parent }]` |
| `inspect(plan, seed)` | como en F003, más `roundedVertices` (vértices de la malla redondeada) y `planeError` (cuánto se separan sus caras de las del poliedro; debe ser casi 0) |

## Errores

Como en F003: una excepción llega a .NET como `JSException` y el tirador pasa a modo sin 3D con
aviso. Los fallos de audio no son errores: se ignoran (FR-010).
