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
| `roll(element, plan, look)` | como en F003, más `look`; cada dado del plan lleva `parent` (índice o `null`) | `Promise` | Dibuja con el aspecto; suena si `look.sound`; lanza el efecto de penetración en la posición del dado padre cuando aparece un dado extra; aura en los dados extra. La animación dura como mucho 3,3 s, contando la preparación (antes 3,6 s; se bajó para que la tirada completa, con lo que hace la página, siga por debajo de 4 s). Solo un efecto por dado que penetra (un d100 tiene dos d10 extra con el mismo padre). |
| `preview(element, look)` | contenedor, aspecto | — | Vista previa: un d20 girando con ese aspecto. Llamarla otra vez cambia el aspecto sin recrear la escena. |
| `stopPreview(element)` | contenedor | — | Para la animación y libera la vista previa. |
| `unlockAudio()` | — | — | Crea o reanuda el contexto de audio y empieza a descargar las grabaciones de `sounds/` que falten; se llama al pulsar «Tirar» y solo con sonido. No espera a la descarga: lo que no haya llegado no suena en esa tirada, y lo que falló se vuelve a pedir en la siguiente. Nunca lanza excepciones. |

## Funciones de prueba

| Función | Salida |
|---|---|
| `shownDice(element)` | como en F003, más `material`, `textured`, `transmission`, `reflective`, `aura` (bool) y `auraColor` de cada dado |
| `heardSounds(element)` | sonidos programados en la última tirada: `[{ at, volume, kind, sample, gain, cutoff }]` (`kind`: material o efecto; `gain` y `cutoff`: volumen y filtro de agudos que tocó según la fuerza; `sample`: grabación que sonó, o `null` si no había audio o no había llegado) |
| `recordingsFor(kind, volume)` | grabaciones posibles para un material o efecto con esa fuerza (0–1); desde 0,3 (5,4 m/s) se usan las de golpe fuerte |
| `recordingsReady()` | `Promise` que se cumple cuando acaba la descarga de grabaciones en curso |
| `loadedRecordings()` | nombres de las grabaciones ya listas para sonar |
| `shownEffects(element)` | efectos de la última tirada, uno por dado que penetra: `[{ effect, parent, launched }]`; `launched` dice si llegó a verse en pantalla |
| `inspect(plan, seed)` | como en F003, más `roundedVertices` (vértices de la malla redondeada) y `planeError` (cuánto se separan sus caras de las del poliedro; debe ser casi 0); `turns` (vueltas de campana de cada dado en la tirada; girar en plano no cuenta) y `strengths` (fuerza de cada golpe) |

## Errores

Como en F003: una excepción llega a .NET como `JSException` y el tirador pasa a modo sin 3D con
aviso. Los fallos de audio no son errores: se ignoran (FR-010).
