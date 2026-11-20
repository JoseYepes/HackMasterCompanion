# Research: Fichas con reglas (F005)

Fuente: MJ en castellano (PDF local del usuario, nunca en el repositorio); páginas impresas.

## 1. Maestría

- **Decision**: `maestría = min(125, base + Σ mejoras)`; `base` = característica relevante, o la
  media de varias redondeada a la baja (MJ p. 307); cada mejora = una tirada del dado de maestría de
  la habilidad + una del Dado de Honor (MJ p. 100–101). Ejemplo para la prueba: Corte a la yugular,
  DES 18, d4 → 3, Dado de Honor d12 → 8: 29 % (MJ p. 100).
- **Rationale**: es la regla del MJ; guardar las mejoras (y no solo el total) permite recalcular si
  cambia la característica y quitar una mejora apuntada por error.
- **Alternatives considered**: guardar solo el total (no se recalcula).
- Una característica con fracción (15/47) cuenta como 15: la regla habla de la puntuación; si el MJ
  dice otra cosa queda pendiente de verificar.

## 2. Maestría escrita (fichas de F004)

- **Decision**: el campo `mastery` de F004 pasa a significar «maestría escrita a mano». Si hay
  maestría escrita, manda ella y se muestra como escrita; el jugador puede borrarla para que se
  calcule. Las habilidades nuevas de F005 no la tienen.
- **Rationale**: no perder lo que el jugador apuntó en F004 (SC-004).

## 3. Dado de Honor

- **Decision**: `HonorDie(Sides, Bonus)` con los valores de la Tabla 5C (MJ p. 85): d3, d4, d4+1,
  d6, d6+1, d8, d8+1, d10, d10+1, d12, d12+1, d12+2 y d20. El jugador lo elige; no se calcula con
  la tabla (spec, aclaraciones).
- Las casillas «1» de la tabla (Honor muy bajo) quedan pendientes de verificar: no se ofrecen.

## 4. Prueba de habilidad

- **Decision**: se tira 1d100 (01–100, el 00 es 100) con `DiceRoller` y `IRandom`; total = tirada +
  modificador; 01–05 éxito automático, 98–100 fallo automático; si no, éxito si total ≤ maestría
  (MJ p. 100 y Glosario, «Prueba de habilidad»). Se guarda en el historial del tirador como la
  fórmula `1d100`, `1d100+N` o `1d100-N`, sin penetración.

## 5. Historial compartido

- **Decision**: `RollHistoryUpdate.ApplyAsync(BrowserStore, RollHistoryFile actual, cambio)` con la
  lógica de `Dice.razor` (releer, cambiar, escribir; si no se puede leer, no escribir). `Dice.razor`
  y el editor la usan.
