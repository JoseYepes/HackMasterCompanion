# Feature Specification: Dados 3D (F003)

**Feature Branch**: `004-dados-3d`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "F003 — Dados 3D. Como jugador, quiero ver los dados rodar con física. Aceptación: existe modo "sin 3D" de respaldo; el resultado mostrado coincide con el dominio en 1 000 tiradas de prueba; ≥ 30 fps en móvil medio (comprobación manual documentada)."

## Clarifications

### Session 2026-11-18

- Q: ¿Cómo se dibuja el d100? → A: Como dos d10, uno para las decenas y otro para las unidades,
  como se tira en la mesa; el valor sigue siendo el del tirador (100 se ve como 00 y 0).
- Q: ¿Cuántos dados a la vez en 3D? → A: 20, contando los extra por penetración.
- Q: ¿Los dados 3D empiezan activados la primera vez? → A: No: empiezan desactivados y el jugador
  los activa cuando quiera; la elección se recuerda.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver los dados rodar (Priority: P1)

Como jugador, quiero que al tirar en `/dados` los dados aparezcan en pantalla y rueden con física
hasta pararse mostrando el resultado, para que la tirada se sienta como en la mesa.

**Why this priority**: Es la razón de ser de la funcionalidad.

**Independent Test**: Tirar "2d6+3" en un móvil y ver dos dados de seis caras rodar y pararse; las
caras de arriba coinciden con los valores del detalle de la tirada y el total es su suma más 3.

**Acceptance Scenarios**:

1. **Given** el tirador con los dados 3D activados, **When** el jugador tira "2d6+3", **Then**
   aparecen dos dados de seis caras que ruedan y se paran, y la cara de arriba de cada uno es el
   valor que muestra el detalle de la tirada.
2. **Given** una fórmula con dados de distintos tipos ("1d20+1d8+1d4"), **When** tira, **Then**
   cada dado se ve con su forma (d20, d8, d4) y muestra su valor.
3. **Given** una tirada en curso, **When** los dados se paran, **Then** el total y el detalle
   aparecen como en F002; el historial se guarda igual.
4. **Given** una tirada en curso, **When** el jugador toca la zona de los dados, **Then** la
   animación termina al momento y se muestra el resultado.
5. **Given** la misma tirada vista en 3D y en el detalle de texto, **When** se comparan, **Then**
   coinciden siempre: el resultado lo decide el tirador de F002, no la animación.

---

### User Story 2 - Modo sin 3D de respaldo (Priority: P1)

Como jugador, quiero poder tirar sin dados 3D (por ejemplo en un móvil lento o con poca batería),
y que la aplicación lo haga sola si el dispositivo no puede mostrarlos.

**Why this priority**: Es un criterio de aceptación de la funcionalidad y garantiza que el tirador
siempre funciona.

**Independent Test**: Desactivar los dados 3D y tirar: el resultado sale al instante como en F002.
Abrir `/dados` en un navegador sin gráficos 3D: el tirador funciona sin 3D y lo indica.

**Acceptance Scenarios**:

1. **Given** el tirador, **When** el jugador desactiva "Dados 3D", **Then** las tiradas se muestran
   como en F002, sin animación, y la elección se recuerda al volver a abrir la aplicación.
2. **Given** un dispositivo o navegador que no puede mostrar gráficos 3D, **When** se abre el
   tirador, **Then** funciona sin 3D, con un aviso breve de por qué, y la opción "Dados 3D" no se
   puede activar.
3. **Given** un jugador que nunca ha activado los dados 3D, **When** abre el tirador, **Then** los
   dados 3D están desactivados y no se descarga nada de la parte 3D.
4. **Given** que la parte 3D falla durante una tirada, **When** ocurre el fallo, **Then** el
   resultado se muestra igualmente en texto y el tirador pasa a modo sin 3D con un aviso.

---

### User Story 3 - Dados que penetran en 3D (Priority: P2)

Como jugador, quiero ver también en 3D los dados extra cuando la opción "Los dados penetran" está
activada, para entender de dónde sale el total.

**Why this priority**: Completa la tirada en 3D, pero el resultado ya se ve en el detalle de texto.

**Independent Test**: Con la penetración activada, tirar "10d4" hasta que salga un 4: aparece un
dado extra que rueda después y queda marcado como dado de penetración.

**Acceptance Scenarios**:

1. **Given** la penetración activada, **When** un dado saca su máximo, **Then** después de pararse
   rueda un dado extra del mismo tipo, distinguible de los demás, con el valor del detalle.
2. **Given** una cadena de penetraciones, **When** se tira, **Then** los dados extra aparecen uno
   tras otro, hasta 20 dados en pantalla (FR-007); el resto se cuenta en el detalle de texto.

---

### Edge Cases

- Dados que no existen como objeto físico (d2, d3, d7, d1000…): no se dibujan en 3D; su resultado
  sale en el detalle de texto y se indica que esos dados no tienen forma 3D.
- d100: se dibuja como dos d10 (decenas y unidades); cuenta como dos dados para el límite de
  FR-007. 100 se ve como 00 y 0; 7 se ve como 00 y 7.
- Fórmulas con muchos dados (por ejemplo "100d6"): con más de 20 dados la tirada se muestra sin
  3D, con un aviso.
- Tirar otra vez mientras los dados aún ruedan: la tirada anterior termina al momento y empieza la
  nueva.
- Girar el móvil o cambiar el tamaño de la ventana durante una tirada: los dados siguen visibles y
  el resultado no cambia.
- La pestaña pasa a segundo plano durante una tirada: al volver, la tirada ya ha terminado y el
  resultado es el mismo.
- Fórmula no válida: no se tira nada ni se anima nada (como en F002).

## Requirements *(mandatory)*

### Functional Requirements

**Resultado**

- **FR-001**: El resultado de cada tirada DEBE seguir decidiéndolo el tirador de F002; la parte 3D
  solo lo muestra. Cada dado 3D DEBE pararse con la cara que corresponde a su valor.
- **FR-002**: Total, detalle e historial DEBEN ser los mismos con y sin 3D.

**Animación**

- **FR-003**: Al tirar con los dados 3D activados, cada dado con forma física (d4, d6, d8, d10,
  d12, d20) DEBE aparecer con su forma y rodar con física hasta pararse. Un d100 se dibuja como
  dos d10 (decenas y unidades).
- **FR-004**: Una tirada DEBE terminar en como mucho 4 segundos; tocar la zona de los dados la
  termina al momento.
- **FR-005**: El total y el detalle de texto DEBEN aparecer cuando los dados se paran (o al
  terminar la animación por cualquier motivo).
- **FR-006**: Con la penetración activada, los dados extra DEBEN rodar después del dado que los
  provoca y distinguirse visualmente.
- **FR-007**: Como mucho 20 dados en pantalla, contando los extra por penetración y cada d10 de
  un d100. Una tirada que empieza con más se muestra sin 3D, con un aviso; los extra por
  penetración que pasen del límite se cuentan solo en el detalle.
- **FR-008**: Los dados sin forma física se muestran solo en el detalle de texto, con una nota.

**Modo sin 3D**

- **FR-009**: El tirador DEBE tener una opción "Dados 3D" que el jugador activa o desactiva; la
  elección DEBE guardarse en el dispositivo como las demás opciones del tirador.
- **FR-010**: Los dados 3D DEBEN empezar desactivados la primera vez; el jugador los activa
  cuando quiera.
- **FR-011**: Si el dispositivo no puede mostrar gráficos 3D, o la parte 3D falla, el tirador DEBE
  funcionar sin 3D y avisar una vez, sin perder la tirada.
- **FR-012**: Sin 3D, el tirador DEBE comportarse exactamente como en F002.

**Calidad**

- **FR-013**: La animación DEBE ir a 30 fotogramas por segundo o más en un móvil de gama media
  con 6 dados en pantalla; se comprueba a mano y se anota el resultado.
- **FR-014**: La zona de los dados DEBE caber en 390 px y en 820 px de ancho sin desbordamiento
  horizontal, y no tapar el campo de fórmula ni el botón de tirar.
- **FR-015**: El resultado DEBE estar siempre también en texto, para lectores de pantalla.
- **FR-016**: La parte 3D NO DEBE retrasar la carga de las demás páginas: solo se descarga al
  abrir el tirador con los dados 3D activados.

### Key Entities *(include if feature involves data)*

- **Tirada** (de F002): fórmula, dados con su valor, dados extra por penetración y total. F003 no
  la cambia: la muestra.
- **Opciones del tirador**: dados que penetran (F002) y dados 3D (nueva), guardadas en el
  dispositivo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En 1 000 tiradas de prueba con dados de todas las formas físicas, la cara que
  muestra cada dado 3D al pararse coincide en el 100 % de los casos con el valor del tirador.
- **SC-002**: Con los dados 3D desactivados, o en un navegador sin gráficos 3D, el tirador
  funciona y da el resultado al instante en el 100 % de los casos.
- **SC-003**: 30 fotogramas por segundo o más en un móvil de gama media con 6 dados (comprobación
  manual documentada, con el modelo de móvil).
- **SC-004**: Ninguna tirada en 3D tarda más de 4 segundos en mostrar el resultado.
- **SC-005**: Las páginas que no son el tirador no tardan más en cargar que antes de F003 (mismo
  tamaño de descarga inicial, con un margen del 5 %).
- **SC-006**: Sin desbordamiento horizontal a 390 px ni a 820 px con los dados en pantalla.

## Assumptions

- Los dados 3D solo aparecen en el tirador `/dados`; las fichas (F004-F005) podrán usarlos más
  adelante.
- Sin sonido en F003.
- El aspecto de los dados es sencillo (un color, números legibles); no hay personalización.
- La comprobación de SC-001 se hace con una prueba automática que pide a la parte 3D la cara de
  arriba de cada dado al pararse.
- Un "móvil de gama media" es el que tenga el usuario para la comprobación manual; se anota el
  modelo.
