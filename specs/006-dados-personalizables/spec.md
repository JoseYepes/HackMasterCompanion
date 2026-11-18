# Feature Specification: Dados personalizables y sonido (F008)

**Feature Branch**: `006-dados-personalizables`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "F008 — Dados personalizables y sonido. Como jugador, quiero elegir el color, el material y los bordes de mis dados 3D, y oír cómo ruedan, para que se parezcan a dados de verdad (calidad y ambiente parecidos a los de Baldur's Gate 3, sin copiar sus diseños). Aceptación: color del dado y de los números; bordes redondeados; varios materiales o texturas; sonido de choques que depende de la fuerza del golpe, con botón para silenciarlo; texturas y sonidos de licencia libre e incluidos en la aplicación (descarga con permiso del usuario); opción de calidad más baja para móviles lentos."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Elegir el aspecto de mis dados (Priority: P1)

Como jugador, quiero elegir el material de mis dados (por ejemplo, marfil, piedra, metal o gema) y
su color y el de los números, y verlos antes de tirar, para que los dados 3D sean «mis dados».

**Why this priority**: Es la razón de ser de la funcionalidad.

**Independent Test**: Abrir la configuración de los dados en `/dados`, elegir un material y unos
colores, ver la vista previa, tirar "2d6+1d20" y comprobar que los dados que ruedan son como los
elegidos y que los números se leen.

**Acceptance Scenarios**:

1. **Given** el tirador con los dados 3D activados, **When** el jugador abre la configuración de
   los dados, **Then** ve los materiales disponibles y una vista previa de un dado que gira.
2. **Given** la configuración abierta, **When** elige un material, un color de dado y un color de
   números, **Then** la vista previa cambia al momento.
3. **Given** un aspecto elegido, **When** tira, **Then** los dados ruedan con ese aspecto y el
   resultado sigue siendo el que decide el tirador (FR-001 de F003).
4. **Given** un color de números que no se leería sobre el color del dado, **When** lo elige,
   **Then** la aplicación lo avisa o corrige el contraste para que los números se lean.
5. **Given** un aspecto elegido, **When** el jugador vuelve otro día, **Then** sus dados tienen el
   mismo aspecto.

---

### User Story 2 - Dados con bordes de dado de verdad (Priority: P1)

Como jugador, quiero que los dados tengan las aristas redondeadas y algo de desgaste, no
poliedros perfectos, para que parezcan dados físicos.

**Why this priority**: Es lo que más cambia la impresión de «dado de verdad» y lo pidió el usuario.

**Independent Test**: Tirar un d20 y un d6 y ver de cerca que las aristas y las esquinas están
redondeadas y que la cara de arriba sigue siendo la que dice el detalle.

**Acceptance Scenarios**:

1. **Given** cualquier forma de dado (d4, d6, d8, d10, d12, d20 y el d10 de decenas), **When**
   rueda, **Then** sus aristas y esquinas se ven redondeadas.
2. **Given** dados redondeados, **When** se paran, **Then** la cara de arriba coincide con el valor
   del detalle en el 100 % de las tiradas, como en F003 (SC-001 de F003).

---

### User Story 3 - Oír los dados (Priority: P2)

Como jugador, quiero oír los dados al chocar con la mesa y entre ellos, más fuerte cuanto más
fuerte es el golpe, y poder silenciarlos, para que la tirada suene como en la mesa.

**Why this priority**: Aporta mucho ambiente, pero el tirador funciona igual sin sonido.

**Independent Test**: Activar el sonido, tirar "6d6" y oír choques que se apagan a medida que los
dados se paran; silenciar y tirar otra vez sin oír nada.

**Acceptance Scenarios**:

1. **Given** el sonido activado, **When** los dados chocan con la mesa o entre sí, **Then** suena
   un golpe cuyo volumen depende de la fuerza del choque.
2. **Given** el sonido activado, **When** se tira, **Then** el sonido del material elegido es
   acorde (por ejemplo, el metal suena distinto que la piedra), si ese material tiene sonido propio.
3. **Given** el botón de silencio, **When** el jugador lo pulsa, **Then** no suena nada hasta que
   lo vuelva a activar, y la elección se recuerda.
4. **Given** el móvil en silencio o el navegador sin permiso para reproducir sonido, **When** se
   tira, **Then** la tirada funciona igual, sin errores.

---

### User Story 4 - Calidad para móviles lentos (Priority: P2)

Como jugador con un móvil lento, quiero bajar la calidad de los dados (sin texturas ni efectos),
para que la animación vaya fluida.

**Why this priority**: Los materiales pesan más que los dados actuales; sin esta opción algunos
móviles podrían ir a tirones.

**Independent Test**: Elegir calidad «baja», tirar "6d6" y comprobar que los dados se ven con
colores lisos y la animación es fluida.

**Acceptance Scenarios**:

1. **Given** la configuración, **When** el jugador elige calidad baja, **Then** los dados se
   dibujan sin texturas ni reflejos, con el color elegido.
2. **Given** calidad baja, **When** se tira, **Then** el resultado y la duración son los mismos que
   con calidad alta.

---

### Edge Cases

- Dados extra por penetración: DEBEN seguir distinguiéndose de los normales sea cual sea el
  aspecto elegido. [NEEDS CLARIFICATION: ¿cómo se marcan los dados extra cuando el jugador elige
  sus colores? Por ejemplo, con un segundo color que elige él, con un brillo o aura, o siempre
  dorados]
- El d100 (dos d10): los dos dados toman el aspecto elegido.
- Un material con texturas que aún no ha cargado: los dados se ven con su color liso mientras
  tanto y la tirada no espera.
- Sin WebGL o con el 3D desactivado: la configuración de aspecto no se muestra o se muestra
  desactivada; el sonido no aplica sin 3D.
- Configuración guardada de una versión anterior o rota: se usan los valores por defecto y no se
  pierde el historial.
- Con el modo noche (F007), la configuración y la vista previa siguen el estilo de la aplicación.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El jugador DEBE poder elegir el material de sus dados entre al menos 4 materiales
  (por ejemplo, marfil, piedra, metal y gema translúcida), con el marfil actual como opción por
  defecto.
- **FR-002**: El jugador DEBE poder elegir el color del dado y el color de los números.
- **FR-003**: Si el color de los números no contrasta lo suficiente con el del dado (menos de
  3:1), la aplicación DEBE avisarlo antes de guardar o proponer un color que se lea.
- **FR-004**: [NEEDS CLARIFICATION: ¿el aspecto es el mismo para todos los dados, o se puede
  elegir uno distinto por tipo de dado (por ejemplo, el d20 de otro color)?]
- **FR-005**: La configuración DEBE tener una vista previa de un dado que gira, que cambia al
  momento con cada elección.
- **FR-006**: Todas las formas de dado DEBEN tener las aristas y las esquinas redondeadas, y la
  cara de arriba DEBE seguir coincidiendo con el resultado del tirador en el 100 % de las tiradas.
- **FR-007**: Los dados extra por penetración DEBEN distinguirse de los normales con cualquier
  aspecto elegido (ver Edge Cases).
- **FR-008**: Los choques de los dados entre sí y con la mesa DEBEN sonar, con un volumen que
  depende de la fuerza del choque, y sin que suenen decenas de golpes a la vez.
- **FR-009**: DEBE haber un control para silenciar el sonido, que se recuerda.
  [NEEDS CLARIFICATION: ¿el sonido empieza activado o silenciado la primera vez?]
- **FR-010**: Sin permiso para reproducir sonido, o si el sonido falla, la tirada DEBE funcionar
  igual y sin mensajes de error.
- **FR-011**: DEBE haber una opción de calidad baja que dibuja los dados sin texturas ni reflejos,
  con el mismo resultado y la misma duración.
- **FR-012**: La configuración (material, colores, sonido y calidad) DEBE recordarse en el
  dispositivo, con versión de formato y migración desde lo guardado por F003.
- **FR-013**: Las texturas y los sonidos DEBEN ser de licencia libre, formar parte de la aplicación
  y no cargarse de servicios de terceros; su descarga necesita el permiso del usuario.
- **FR-014**: Las texturas y los sonidos DEBEN descargarse solo al activar los dados 3D, como el
  resto del 3D (FR-016 de F003).
- **FR-015**: Los diseños de los dados DEBEN ser propios: NO DEBEN copiar los de ningún juego.
- **FR-016**: Los controles nuevos DEBEN cumplir lo de F007: contraste en los dos modos, botones
  de 44 px y sin desbordamiento a 390 y 820 px.

### Key Entities *(include if feature involves data)*

- **Aspecto de los dados**: material, color del dado, color de los números y, según FR-004, si es
  común a todos los dados o por tipo.
- **Preferencias de los dados**: aspecto, sonido activado o silenciado y calidad. Se guardan en el
  dispositivo; en F006 se guardarán también en la cuenta del jugador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: En 1 000 tiradas simuladas con dados redondeados, la cara de arriba coincide con la
  pedida en el 100 % de los dados, igual que en F003.
- **SC-002**: Un jugador cambia el aspecto de sus dados y lo ve en la vista previa en menos de
  30 segundos desde que abre la configuración.
- **SC-003**: Con calidad alta, la animación de "6d6" va fluida en el móvil del usuario
  (comprobación manual); con calidad baja, al menos tan fluida como los dados de F003.
- **SC-004**: Una tirada con sonido no tarda más que sin sonido (sigue en menos de 4 s).
- **SC-005**: Sin activar los dados 3D, la descarga de la aplicación no crece.
- **SC-006**: El usuario reconoce sus dados como «dados de verdad» y le gustan (comprobación
  manual en su móvil).

## Assumptions

- Los materiales y colores concretos se proponen al usuario con imágenes antes del plan, como se
  hizo con el estilo de F007.
- Las texturas pueden generarse con código (vetas, ruido, brillo) siempre que se pueda; solo se
  descargan imágenes o sonidos si no queda bien de otra forma, y siempre con permiso.
- Los sonidos serán grabaciones de licencia libre (por ejemplo, dominio público o CC0) o generados
  con código.
- La mesa sigue siendo la de F007 (cuero o madera según el modo); hacerla elegible queda fuera de
  esta funcionalidad salvo que el usuario lo pida.
- La configuración se guarda solo en el dispositivo; guardarla en la cuenta es parte de F006.
- Inspiración: dados de calidad de videojuego (materiales ricos, bordes gastados, brillos), sin
  copiar los diseños de ningún juego concreto.
