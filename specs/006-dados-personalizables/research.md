# Research: Dados personalizables y sonido (F008)

Decisiones técnicas del plan: qué se eligió, por qué y qué se descartó.

## 1. Aristas redondeadas sin cambiar la física

**Decisión**: la malla que se ve es la **suma de Minkowski** de un poliedro encogido y una esfera:
para cada dirección `d` de una esfera muy subdividida, el punto de la superficie es el vértice del
poliedro encogido que más avanza en `d`, más `r·d`. El poliedro se encoge con un factor `s` y
`r = h·(1 − s)`, donde `h` es la distancia del centro a las caras (todas las caras de cada forma
están a la misma distancia). Así **los planos de las caras no se mueven**: el dado redondeado se
apoya exactamente igual que el de F003.

**Por qué**:
- La física (cannon-es) sigue usando el poliedro de siempre: no cambia ni la simulación ni la
  forma de forzar la cara (F003 research §2), y la prueba de las 1 000 tiradas sigue valiendo.
- Probado en el prototipo enseñado al usuario el 2026-11-18.

**Descartado**:
- Biselar a mano cada forma: siete geometrías distintas y propensas a errores.
- Física con formas redondeadas: más lenta y la cara de arriba sería menos estable.

**Números**: cada cara lleva una lámina con su número (o los tres del d4) pegada al plano de la
cara, en vez de pintar el número en la textura de la cara como en F003, porque la malla redondeada
no tiene una cara por material.

## 2. Materiales

**Decisión**: 11 materiales, todos generados con código, sin descargar imágenes:

| Material | Cómo se hace | Calidad baja |
|---|---|---|
| Marfil | color mate | igual |
| Mármol | vetas dibujadas en un lienzo | color liso |
| Oro viejo, hierro negro | metal con arañazos en la rugosidad y reflejos del entorno | color liso |
| Rubí, amatista, cristal | material translúcido (transmisión) con barniz | color liso semitransparente |
| Obsidiana | negro brillante con barniz | color liso |
| Hueso | manchas y grietas en un lienzo | color liso |
| Madera | anillos y vetas en un lienzo | color liso |
| Piedra | moteado en un lienzo | color liso |

- Los reflejos salen de un **entorno generado con código** (un degradado cálido), como en el
  prototipo.
- El jugador cambia el color base de cualquier material y el color de los números.
- Las texturas se proyectan desde el centro del dado (coordenadas esféricas). En materiales de
  ruido no se nota la costura.

**Por qué**: no hay descargas (no hace falta permiso), el módulo crece pocos KB y todo funciona sin
conexión.

**Descartado**: texturas fotográficas de licencia libre (más peso y descarga; se pueden añadir más
adelante si algún material no convence).

## 3. Sonido

**Decisión**: sonido **sintetizado con Web Audio**, sin archivos:

- Durante la simulación (que se hace antes de animar, F003 research §2) se apuntan los choques
  con su paso y su fuerza. Al reproducir, cada choque suena en su momento, escalado al tiempo de la
  animación.
- Cada golpe es un ruido corto filtrado más un tono resonante que depende del material (madera
  grave y seca, metal agudo con eco, cristal y gemas tintineo, piedra y hueso chasquido).
- Volumen según la fuerza del choque; como mucho 4 golpes cada 50 ms y nada por debajo de un umbral
  (FR-008).
- Efectos de penetración: estallido (explosión), soplo (fuego), chapoteo (sangre).
- El contexto de audio se crea al pulsar «Tirar» (los navegadores no dejan sonar sin un gesto del
  usuario). Si falla, se ignora (FR-010).

**Por qué**: sin descargas ni licencias que comprobar, y el sonido depende del material.

**Descartado**: grabaciones CC0 (habría que descargarlas con permiso; quedan como mejora si el
sintetizado no convence al usuario).

## 4. Efectos al penetrar

**Decisión**:
- **Aura**: una luz aditiva detrás de cada dado extra, del color de acento de su material.
- **Efecto**: cuando aparece un dado extra, en la posición de su dado padre (el que sacó el
  máximo) se lanza un sistema de partículas de unos 0,6 s: chispas naranjas (explosión), llamas que
  suben (fuego) o gotas y salpicaduras en la mesa (sangre).
- El **dado padre** lo indica el dominio: `BoardDie` gana `Parent`, el índice del dado que penetró
  (data-model). Así el módulo no tiene que adivinarlo.
- Con `prefers-reduced-motion` o calidad baja: solo un destello breve y el aura (FR-018).

**Por qué**: el efecto se ve justo donde ocurre la penetración y no alarga la tirada (los dados
extra ya esperaban a que se parase su ola).

## 5. Dónde se guarda la configuración

**Decisión**: en el historial de tiradas (`hackmaster.rolls`), que ya guarda la opción de dados 3D:
el formato pasa a la **versión 3** con un objeto `look` (material, colores, efecto, sonido y
calidad). Una versión 2 se migra con los valores por defecto (marfil, explosión, sonido activado,
calidad alta).

**Por qué**: es donde F003 guardó la preferencia de 3D, la migración tiene pruebas (principio V) y
F006 sincronizará este mismo archivo.

**Descartado**: una clave nueva solo para el aspecto: dos sitios para las preferencias de los
dados.

## 6. Contraste de los números (FR-003)

**Decisión**: una regla del dominio calcula el contraste WCAG entre el color del dado y el de los
números. Por debajo de 3:1, la configuración avisa y ofrece «Usar números claros» u «oscuros», el
que contraste más.

## 7. Pruebas

| Qué | Cómo |
|---|---|
| Aspecto guardado, migración 2 → 3, valores rotos | xUnit en `StoredDataTests` |
| Contraste y color propuesto | xUnit `DiceLookTests` |
| `Parent` de los dados extra | xUnit `DiceBoardTests` |
| Panel de configuración (11 materiales, colores, efecto, sonido, calidad, aviso de contraste, vista previa) | bUnit `DiceLookPanelTests` |
| Cara de arriba con dados redondeados en 1 000 tiradas, y planos de las caras en su sitio | Playwright: se amplía `Dice3DTests` |
| Sonido: choques limitados, silencio, material | Playwright: el módulo informa de los sonidos programados (`heardSounds`) |
| Efecto y aura | Playwright: el módulo informa de efectos lanzados (`shownEffects`) |
| Calidad baja sin texturas ni transmisión | Playwright: `shownDice` incluye el tipo de material |
| Duración < 4 s con efectos y sonido | Playwright |
| Sin 3D no se descarga nada nuevo | prueba existente de F003 |
| Aspecto en el móvil | a mano, el usuario (SC-003, SC-006) |

## 8. Tamaño

El módulo `dice3d.js` crecerá unas centenas de líneas (unos 15 KB sin comprimir) y solo se
descarga con los dados 3D activados. Sin 3D, la descarga no cambia (SC-005).
