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

**Decisión (2026-11-19)**: **grabaciones CC0** en `wwwroot/sounds/`, que suenan con Web Audio.
Sustituye al sonido sintetizado de la primera versión: el usuario lo probó en su móvil y le pareció
«demasiado cutre».

- Durante la simulación (que se hace antes de animar, F003 research §2) se apuntan los choques
  con su paso y su fuerza. Al reproducir, cada choque suena en su momento, escalado al tiempo de la
  animación. Esto no cambia.
- Cada golpe usa una grabación al azar del grupo de su material, de golpe suave o fuerte según la
  fuerza (desde 0,3 de la fuerza llena, 5,4 m/s), con un tono algo distinto cada vez (±6 %) y uno propio por material (el oro,
  más grave que el hierro; el cristal, el más agudo).

| Material | Grabaciones | Origen |
|---|---|---|
| Madera | `impactWood_*` | Kenney, Impact Sounds 1.0 |
| Oro viejo, hierro negro | `impactMetal_*` | Kenney, Impact Sounds 1.0 |
| Rubí, amatista, cristal | `impactGlass_*` | Kenney, Impact Sounds 1.0 |
| Mármol | `impactPlate_*` (cerámica) | Kenney, Impact Sounds 1.0 |
| Piedra | `impactMining_*` | Kenney, Impact Sounds 1.0 |
| Marfil, hueso, obsidiana | `die-throw-*` (un dado de verdad cayendo en la mesa) | Kenney, Casino Audio 1.1 |

- Efectos de penetración: explosión `bang_08`/`bang_09` (rubberduck), fuego `flame_0`
  (themightyglider) y sangre `squish*` (EZduzziteh), de OpenGameArt.
- Las 38 grabaciones pesan 326 KB. Se descargan la primera vez que se tira con sonido (al llamar a
  `unlockAudio`), no al abrir la aplicación ni con el sonido silenciado. La tirada no espera a la
  descarga (SC-004): lo que no haya llegado no suena en esa tirada y lo que falló se vuelve a pedir
  en la siguiente. Con calidad baja o menos movimiento, el destello suena con el efecto elegido.
- Si la tirada acaba sola, los últimos golpes se dejan sonar 0,6 s más; si el jugador toca la mesa
  o tira otra vez, se cortan enseguida.
- **Según la fuerza (comprobación del usuario, 2026-11-19: «el sonido no se modifica según la
  fuerza»)**: la fuerza llena a 18 m/s (antes 12, y un 14 % de los golpes salía al máximo). El
  volumen crece como la fuerza elevada a 1,6 (del golpe más flojo al más fuerte, unas 25 veces),
  los golpes flojos pasan por un filtro que quita los agudos (de 700 Hz a 16 kHz según la fuerza) y
  los fuertes suenan algo más agudos. Las grabaciones de golpe fuerte se usan desde 5,4 m/s (0,3 de la
  fuerza llena; antes 0,45 de 12, la misma fuerza). El compresor ya no iguala los golpes (umbral −12 dB, 3:1); solo
  evita que saturen muchos juntos.
- Como mucho 4 golpes cada 50 ms y nada por debajo de un umbral (FR-008). Esto no cambia.
- El contexto de audio se crea al pulsar «Tirar» (los navegadores no dejan sonar sin un gesto del
  usuario). Si falla, se ignora (FR-010).
- Licencias, origen, tamaño y SHA-256 de cada archivo en `wwwroot/sounds/README.md`. Los archivos
  están sin modificar. La descarga se hizo con el permiso del usuario.

**Por qué**: suenan a dados de verdad y a cada material. Son CC0, así que no hay que citar a nadie
(aun así se deja constancia del origen), y pesan poco.

**Descartado**: el sonido sintetizado (primera versión, rechazada por el usuario). También grabar
el sonido de cada material por separado o usar paquetes de pago.

## 3 bis. Giro de los dados (comprobación del usuario, 2026-11-19)

«Los dados no giran mucho»: salían con giros al azar que no tenían que ver con la marcha. Medidas
como vueltas de campana (cuánto gira la dirección «arriba» vista desde el dado; girar en plano como
una peonza no cuenta), el d4 daba 0,9 de mediana.

**Decisión**: el dado sale rodando hacia donde va (giro sobre el eje perpendicular a la marcha,
4 rad/s por cada m/s, ±20 %, más un poco al azar). Ahora da entre 2,4 (d4) y 3,2 (d8) vueltas de
campana de mediana; todos acaban dentro de la mesa y la mediana del tiempo hasta pararse casi no
cambia. Se probó también más rozamiento (0,6): no cambiaba el giro y se descartó (revisión).
**Pendiente**: el d20 llega algo más a menudo al tope de 5 s de simulación (7 de 60 tiradas, antes
1), y el d12 llega casi siempre, ya antes de este cambio (no llega a quedarse quieto). No rompe
nada (la animación sigue durando como mucho 3,3 s), pero el final de esas tiradas se ve acelerado. La cara de arriba se sigue forzando igual (SC-001 de F003 pasa).
`inspect` devuelve `turns` (vueltas de campana de cada dado) y `strengths` (fuerza de cada golpe).

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

**Medido el 2026-11-18 (T019)**, publicando en Release `main` y la rama y comparando los archivos
comprimidos con Brotli:

| Archivo | Antes | Después | Se descarga |
|---|---|---|---|
| `app.css` | 4,1 KB | 4,3 KB | siempre |
| `HackMaster.Web` | 103,8 KB | 107,7 KB | siempre |
| `HackMaster.Domain` | 37,3 KB | 40,3 KB | siempre |
| `dice3d.js` | 7,0 KB | 13,4 KB | solo con dados 3D |

Sin 3D la descarga crece unos 7 KB, un **0,25 %**: el panel y las reglas del aspecto son código C#
que se carga siempre. El plan decía «no crece», lo que no era posible con el panel en C#; SC-005 se
ajustó a «menos de un 1 %». Texturas, sonidos y efectos no ocupan nada: se generan con código.
