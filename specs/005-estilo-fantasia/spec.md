# Feature Specification: Estilo de fantasía heroica (F007)

**Feature Branch**: `005-estilo-fantasia`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "Dar a la aplicación un estilo de fantasía heroica. De tres maquetas (A · Pergamino y tinta, B · Taberna a la luz de las velas, C · Grimorio heroico), el usuario eligió «A con modo noche B»: el aspecto normal es el pergamino y el modo noche es la taberna. Se hace antes de F004 (Fichas de PJ)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Aspecto de pergamino y tinta (Priority: P1)

Como jugador, quiero que todas las pantallas tengan el aspecto de un pergamino escrito con tinta,
con títulos de inscripción, granate y dorado, para que la aplicación se sienta parte del juego.

**Why this priority**: Es la razón de ser de la funcionalidad y el aspecto que se ve por defecto.

**Independent Test**: Abrir Habilidades, una ficha de habilidad, Conjuntos, el editor de conjuntos,
Estilos, Dudas y Dados en un móvil: todas comparten el fondo de pergamino, la barra granate con
borde dorado y los títulos de inscripción de la propuesta A, y todo se lee bien.

**Acceptance Scenarios**:

1. **Given** cualquier pantalla de la aplicación, **When** se abre en modo normal, **Then** tiene
   fondo de pergamino claro, texto de color tinta, barra de navegación granate con borde dorado y
   títulos con letras de inscripción, como la maqueta A.
2. **Given** la lista de habilidades, **When** se muestra, **Then** cada habilidad es una tarjeta
   de pergamino con su nombre como título y sus datos (tipo, precio, dificultad, libro) como
   etiquetas, como en la maqueta A.
3. **Given** formularios (búsqueda, filtros, editor de conjuntos, fórmula de dados), **When** se
   usan, **Then** los campos, botones y mensajes de error siguen el mismo estilo y se distingue
   claramente cuál tiene el foco.
4. **Given** los avisos que ya existen (dudas, "pendiente de verificar", errores), **When** se
   muestran, **Then** siguen siendo reconocibles como avisos y no se confunden con texto normal.

---

### User Story 2 - Modo noche de taberna (Priority: P1)

Como jugador, quiero un modo noche oscuro de madera y ámbar, para usar la aplicación en la mesa
de juego con poca luz sin deslumbrar.

**Why this priority**: Es la otra mitad de la elección del usuario y se usa en las partidas.

**Independent Test**: Activar el modo noche: todas las pantallas pasan al aspecto de la maqueta B
(fondo de madera oscura, texto crema, títulos y acentos en ámbar). Cerrar y volver a abrir la
aplicación: sigue en modo noche.

**Acceptance Scenarios**:

1. **Given** la aplicación en modo normal, **When** el jugador activa el modo noche, **Then** todas
   las pantallas cambian al aspecto de taberna al momento, sin recargar ni perder lo que estaba
   escribiendo.
2. **Given** el modo noche elegido, **When** el jugador vuelve otro día, **Then** la aplicación se
   abre en modo noche.
3. **Given** un jugador que nunca ha elegido modo, **When** abre la aplicación, **Then** se usa el
   modo que pide su dispositivo (claro u oscuro); en cuanto elige uno, manda su elección.
4. **Given** el modo noche, **When** se abre cualquier pantalla, **Then** no aparece ningún
   destello claro al cargar ni al cambiar de pantalla.

---

### User Story 3 - La mesa de dados acompaña al estilo (Priority: P2)

Como jugador, quiero que el tirador de dados, la bandeja y la mesa de los dados 3D combinen con el
modo elegido, para que la tirada forme parte del mismo ambiente.

**Why this priority**: Mejora la experiencia, pero el tirador ya funciona sin ello.

**Independent Test**: Tirar "2d6+1d20" con los dados 3D en modo normal y en modo noche: la mesa,
los botones de la bandeja y el total cambian de aspecto con el modo, y las caras de los dados se
siguen leyendo.

**Acceptance Scenarios**:

1. **Given** el tirador en modo normal, **When** se tira con dados 3D, **Then** la mesa tiene un
   aspecto acorde con el pergamino (por ejemplo, tapete o cuero) y los números de los dados se leen.
2. **Given** el tirador en modo noche, **When** se tira con dados 3D, **Then** la mesa tiene un
   aspecto de madera oscura y los números de los dados se leen.
3. **Given** una tirada en curso, **When** el jugador cambia de modo, **Then** la tirada termina
   bien y muestra el mismo resultado.

---

### Edge Cases

- Sin conexión: el estilo completo, letras incluidas, se ve igual que con conexión.
- Las letras de inscripción aún no han cargado: el texto se ve con una letra de reserva parecida y
  no queda invisible mientras tanto.
- Nombres largos de habilidades ("Combate avanzado con dos armas (Advanced Two-Weapon Fighting)")
  en un móvil de 390 px: se parten en varias líneas sin desbordar.
- Textos en mayúsculas pequeñas o letras decorativas: no se usan para párrafos ni para datos
  (precios, dificultades, fórmulas), solo para títulos.
- El dispositivo pide menos animación: el cambio de modo no usa transiciones llamativas.
- La impresión de una pantalla no está en el alcance (ver Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Todas las pantallas DEBEN usar el estilo «pergamino y tinta» de la maqueta A en
  modo normal: fondo de pergamino claro, texto de color tinta, granate como color principal,
  dorado para bordes y detalles, y letras de inscripción en los títulos.
- **FR-002**: La aplicación DEBE ofrecer un modo noche con el estilo «taberna» de la maqueta B:
  fondo de madera oscura, texto crema, ámbar como color principal y las mismas letras de los
  títulos que en el modo normal o unas de la misma familia de fantasía.
- **FR-003**: DEBE haber un control visible en todas las pantallas para pasar de un modo a otro.
- **FR-004**: La elección de modo DEBE recordarse en el dispositivo; si el jugador nunca ha
  elegido, se DEBE usar el modo claro u oscuro que pida el dispositivo.
- **FR-005**: El cambio de modo DEBE aplicarse al momento, sin recargar la página ni perder lo
  escrito, y la aplicación NO DEBE mostrar un destello del otro modo al abrirse.
- **FR-006**: En los dos modos, el texto normal DEBE tener un contraste de al menos 4,5:1 con su
  fondo, y los títulos grandes, iconos y bordes de controles, de al menos 3:1.
- **FR-007**: Las letras decorativas solo DEBEN usarse en títulos; los párrafos, datos, campos y
  botones DEBEN usar una letra de lectura cómoda.
- **FR-008**: Las letras, colores e imágenes del estilo DEBEN formar parte de la aplicación y
  funcionar sin conexión; NO DEBEN cargarse de servicios de terceros.
- **FR-009**: Los avisos existentes (dudas, "pendiente de verificar", errores, aviso de dados 3D)
  DEBEN seguir distinguiéndose del texto normal en los dos modos.
- **FR-010**: Los botones y controles táctiles DEBEN medir al menos 44 × 44 px, y el foco del
  teclado DEBE verse claramente en los dos modos.
- **FR-011**: Ninguna pantalla DEBE desbordarse en horizontal a 390 px ni a 820 px en ninguno de
  los dos modos.
- **FR-012**: La mesa de los dados 3D DEBE cambiar de aspecto con el modo (US3), y los números de
  los dados DEBEN leerse en los dos.
- **FR-013**: El estilo NO DEBE cambiar el contenido, el orden de la información ni el
  comportamiento de ninguna pantalla: solo su aspecto y el nuevo control de modo.
- **FR-014**: Las letras de los títulos DEBEN tener una letra de reserva, de modo que el texto se
  vea aunque aún no hayan cargado.

### Key Entities *(include if feature involves data)*

- **Preferencia de modo**: normal, noche o "sin elegir" (sigue al dispositivo). Se guarda en el
  dispositivo del jugador; no es contenido del juego.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Las 12 pantallas de la aplicación (Inicio, Habilidades, ficha de habilidad,
  Conjuntos, ficha de conjunto, ficha de conjunto oficial, editor de conjuntos, Estilos, ficha de
  estilo, Dudas, Dados y la página de "no encontrada") se ven con el estilo elegido en los dos
  modos.
- **SC-002**: En los dos modos, el 100 % de las combinaciones de texto y fondo medidas cumplen los
  contrastes de FR-006.
- **SC-003**: Cambiar de modo tarda menos de 0,5 s en aplicarse en un móvil medio.
- **SC-004**: Sin conexión, después de la primera visita, el estilo se ve completo (letras
  incluidas) en las 12 pantallas.
- **SC-005**: La primera carga de la aplicación crece como mucho un 10 % respecto a antes del
  estilo.
- **SC-006**: Ninguna pantalla se desborda en horizontal a 390 px ni a 820 px en ninguno de los
  dos modos.
- **SC-007**: El usuario reconoce en la aplicación las maquetas A (modo normal) y B (modo noche)
  que eligió (comprobación manual en su móvil).

## Assumptions

- Se parte de las maquetas A y B enviadas al usuario el 2026-11-18; los colores exactos pueden
  ajustarse lo necesario para cumplir los contrastes de FR-006.
- Las letras serán de licencia libre que permita incluirlas en la aplicación (por ejemplo, la
  licencia SIL Open Font License); su elección y descarga se deciden en el plan, con permiso del
  usuario para descargarlas.
- Los dados 3D siguen siendo de color marfil con números oscuros; solo cambia la mesa.
- No se añaden ilustraciones, logotipos ni iconos nuevos de la aplicación: el estilo se consigue
  con colores, letras, bordes y texturas sencillas. Un logotipo propio podría ser una
  funcionalidad aparte.
- La impresión de pantallas y un tercer modo de alto contraste quedan fuera de alcance.
- No hay contenido del juego nuevo: los textos de las maquetas eran datos reales ya publicados.
