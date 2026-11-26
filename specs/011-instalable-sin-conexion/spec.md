# Feature Specification: Aplicación instalable y sin conexión (F013)

**Feature Branch**: `011-instalable-sin-conexion`

**Created**: 2026-11-26

**Status**: Draft

**Input**: User description: "F013 — Aplicación instalable y sin conexión. Como jugador, quiero instalar la aplicación en el móvil o el ordenador y usarla en la mesa aunque no haya conexión, como piden A1 y A2. Aceptación: se puede instalar desde el navegador y se abre como una aplicación propia, con su nombre e icono; tras la primera visita con conexión, todo lo que no necesita la cuenta funciona sin conexión, incluso al recargar o abrir una dirección interna; el inicio de sesión y la sincronización (F006) no se guardan para usarlos sin conexión; las versiones nuevas llegan sin tener que borrar nada; funciona con la ruta base de GitHub Pages; sin desbordamiento a 390 px y 820 px."

## Clarifications

### Session 2026-11-26

- Q: ¿Cómo llega una versión nueva a quien ya tiene la aplicación guardada? → A: aviso «Hay una
  versión nueva» con un botón «Actualizar» que recarga con ella; si no se pulsa, se aplica sola la
  próxima vez que se abre la aplicación.
- Q: ¿Botón «Instalar» propio? → A: sí, en la página de inicio cuando el navegador lo permite, y en
  iPhone y iPad una nota con «Compartir → Añadir a pantalla de inicio»; no se ve si ya está
  instalada.
- Q: ¿La copia sin conexión incluye los dados 3D y los sonidos? → A: sí, todo, en segundo plano tras
  la primera visita.
- Q: ¿Qué icono? → A: un dibujo propio del agente, hecho con código: un dado de 20 caras dorado
  sobre granate.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Usar la aplicación en la mesa sin conexión (Priority: P1)

Como jugador, quiero abrir la aplicación en la mesa de juego aunque no haya cobertura ni wifi, y
consultar habilidades, conjuros, mis fichas y tirar dados como siempre.

**Why this priority**: es lo que piden `PRODUCTO.md` A1 y A2 («a veces sin conexión en la mesa») y lo
que hoy falta: sin conexión, la página no abre.

**Independent Test**: visitar la aplicación una vez con conexión, cortar la conexión y recargar;
abrir habilidades, una ficha, el grimorio y el tirador (también en 3D), y abrir escribiendo a mano
una dirección interna (por ejemplo, la ficha de una habilidad).

**Acceptance Scenarios**:

1. **Given** que visité la aplicación con conexión y terminó de prepararse, **When** la abro sin
   conexión, **Then** se abre y funcionan la consulta de habilidades, conjuntos, estilos, grimorio,
   dudas, el creador de conjuntos, el tirador de dados (2D y 3D, con sonido) y las fichas.
2. **Given** sin conexión, **When** recargo la página o abro directamente una dirección interna
   (`/habilidades/{nombre}`, `/fichas/{id}`…), **Then** se abre esa página, no un error del
   navegador.
3. **Given** sin conexión, **When** creo o cambio una ficha, un conjunto o una tirada, **Then** se
   guarda en el dispositivo como hasta ahora.
4. **Given** sin conexión y con la sesión iniciada, **When** uso la aplicación, **Then** los cambios
   quedan pendientes y se suben cuando vuelve la conexión, como ya hace F006.

---

### User Story 2 - Instalar la aplicación (Priority: P1)

Como jugador, quiero instalar la aplicación en la pantalla de inicio del móvil o en el ordenador
para abrirla con un toque, como una aplicación más, con su nombre y su icono.

**Why this priority**: «aplicación web instalable» es el objetivo del producto (A1).

**Independent Test**: en un navegador que lo permita, comprobar que la aplicación se ofrece para
instalar; instalarla y abrirla: se abre en su propia ventana, sin barra de direcciones, con el nombre
«HackMaster Companion» y su icono.

**Acceptance Scenarios**:

1. **Given** la aplicación publicada, **When** la visito en un navegador que permite instalar, **Then**
   el navegador la reconoce como instalable.
2. **Given** la aplicación instalada, **When** la abro desde su icono, **Then** se abre en la página de
   inicio de la aplicación (dentro de su ruta base), en su propia ventana.
3. **Given** un navegador que ofrece instalar y la aplicación sin instalar, **When** abro la página
   de inicio, **Then** veo un botón «Instalar» que abre el aviso de instalación del navegador.
4. **Given** un iPhone o iPad sin la aplicación instalada, **When** abro la página de inicio,
   **Then** veo una nota que explica «Compartir → Añadir a pantalla de inicio».
5. **Given** la aplicación ya instalada y abierta desde su icono, **Then** no se ve ni el botón ni la
   nota.

---

### User Story 3 - Recibir las versiones nuevas (Priority: P2)

Como jugador, quiero que la aplicación guardada se actualice cuando se publica una versión nueva, sin
tener que borrar datos ni reinstalar.

**Why this priority**: sin esto, guardar la aplicación para usarla sin conexión dejaría a la gente
con versiones viejas.

**Independent Test**: con la aplicación guardada, publicar una versión nueva y volver a abrirla con
conexión: sale el aviso, «Actualizar» muestra la versión nueva, y las fichas, conjuntos y tiradas
siguen ahí.

**Acceptance Scenarios**:

1. **Given** una versión nueva publicada, **When** abro la aplicación con conexión, **Then** la versión
   nueva se descarga sola en segundo plano.
2. **Given** la versión nueva descargada, **Then** aparece el aviso «Hay una versión nueva» con un
   botón «Actualizar»; al pulsarlo, la página se recarga con la versión nueva.
3. **Given** el aviso sin pulsar, **When** cierro la aplicación y la vuelvo a abrir, **Then** se abre
   con la versión nueva.
4. **Given** cualquier actualización, **Then** no se pierde nada de lo guardado en el dispositivo.

---

### Edge Cases

- Primera visita sin terminar de preparar la copia (se corta la conexión a mitad): la aplicación no
  queda medio guardada; la próxima visita con conexión lo vuelve a intentar y, mientras, sin conexión
  no se abre (como hoy).
- Iniciar sesión sin conexión: no se puede; la aplicación lo dice con un mensaje claro en lugar de
  llevar a un error del navegador.
- Las direcciones de inicio y cierre de sesión (`authentication/…`) y las peticiones a la cuenta en la
  nube nunca se responden con una copia guardada.
- Dos pestañas abiertas cuando llega una versión nueva: ninguna se rompe por mezclar archivos de dos
  versiones; la nueva se usa cuando se pulsa «Actualizar» o se cierran todas.
- Desarrollo local: la copia sin conexión no se activa al probar en el equipo, para que los cambios
  se vean al recargar.
- El navegador no permite guardar la aplicación (modo privado, espacio lleno): la aplicación funciona
  con conexión como hoy.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La aplicación publicada DEBE poder instalarse desde los navegadores que lo permiten, con
  el nombre «HackMaster Companion», un nombre corto, su icono, los colores del estilo (F007) y
  abriéndose en su propia ventana.
- **FR-002**: Al instalarla, DEBE abrirse en la página de inicio dentro de la ruta base del sitio
  publicado (`/HackMasterCompanion/`), y sus direcciones DEBEN quedarse dentro de esa ruta.
- **FR-003**: Tras una visita con conexión, la aplicación DEBE guardar en el dispositivo una copia de
  todos sus archivos propios para funcionar sin conexión, incluidos los dados 3D, las letras y los
  sonidos.
- **FR-004**: Sin conexión, DEBEN funcionar todas las páginas que no necesitan la cuenta, también al
  recargar o al abrir directamente una dirección interna.
- **FR-005**: La copia NO DEBE incluir ni responder las direcciones de inicio y cierre de sesión
  (`authentication/…`), las peticiones a la cuenta en la nube ni nada de otros sitios; esas siempre
  van a la red.
- **FR-006**: Sin conexión, el botón «Iniciar sesión» DEBE avisar de que hace falta conexión en lugar
  de llevar a una página de error.
- **FR-007**: Cuando se publique una versión nueva, la aplicación DEBE descargarla en segundo plano al
  abrirse con conexión, pasar a ella sin mezclar archivos de dos versiones y borrar la copia vieja.
- **FR-008**: Cuando la versión nueva esté lista, la aplicación DEBE mostrar el aviso «Hay una
  versión nueva» con el botón «Actualizar», que recarga la página con ella; sin pulsarlo, la versión
  nueva se usa la próxima vez que se abra la aplicación.
- **FR-009**: Ninguna actualización ni la instalación DEBEN borrar ni cambiar los datos guardados en
  el dispositivo (fichas, conjuntos, historial, aspecto de los dados, modo, cuenta).
- **FR-010**: La página de inicio DEBE mostrar un botón «Instalar» cuando el navegador ofrece
  instalar la aplicación, y en iPhone y iPad una nota con «Compartir → Añadir a pantalla de inicio»;
  ninguno de los dos se ve si la aplicación ya está instalada (abierta en su propia ventana).
- **FR-011**: En desarrollo local la copia sin conexión NO DEBE activarse.
- **FR-012**: Lo que se añada a la interfaz DEBE estar en castellano, verse en los dos modos (F007) y
  no desbordar en horizontal a 390 px ni a 820 px.
- **FR-013**: Preparar la copia NO DEBE retrasar la primera carga: se hace cuando la página ya está
  abierta (F003 FR-016).

### Key Entities

- **Copia de la aplicación**: los archivos de una versión publicada, guardados en el dispositivo;
  una por versión, y se borra la anterior al cambiar.
- **Descripción de la aplicación**: nombre, nombre corto, icono, colores, página de inicio y ámbito,
  que el navegador usa para instalarla.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Tras una visita con conexión, el 100 % de las pantallas de la aplicación (inicio,
  habilidades y su ficha, conjuntos, estilos, grimorio y conjuros, dudas, dados, fichas) se abren
  sin conexión, también recargando cada una, en la prueba automática del sitio publicado con su
  ruta base.
- **SC-002**: Sin conexión, la aplicación instalada se abre en menos de 3 s en un móvil medio
  (`PRODUCTO.md` A2; comprobación manual del usuario).
- **SC-003**: La aplicación cumple lo que el navegador pide para instalar (descripción con nombre,
  icono de 192 y 512 px, página de inicio, ventana propia y trabajo sin conexión), comprobado por una
  prueba automática.
- **SC-004**: Ninguna petición a `authentication/…`, a la cuenta en la nube o a otros sitios queda en
  la copia (prueba automática).
- **SC-005**: Tras publicar una versión nueva, el 100 % de lo guardado en el dispositivo sigue igual,
  sale el aviso y «Actualizar» muestra la versión nueva (prueba automática con dos versiones).
- **SC-006**: Lo que se descarga antes de que la aplicación se vea no crece más de un 1 % (como F008
  SC-005).

## Assumptions

- Es una aplicación web instalable desde el navegador; las tiendas de aplicaciones siguen fuera de
  alcance (A3).
- «Primera visita» es una visita completa con conexión; antes de ella no hay nada guardado.
- Los datos del juego ya viajan dentro de la aplicación (F001), así que la copia los incluye sin
  tocar `data/`.
- El icono es un dibujo propio del agente, hecho con código: un dado de 20 caras dorado sobre
  granate, sin descargas; el usuario puede cambiarlo más adelante.
- La copia completa ocupa unos 15 MB en el dispositivo y se descarga comprimida (unos 5 MB).
- iPhone y iPad no ofrecen un aviso para instalar: se instala con «Compartir → Añadir a pantalla de
  inicio».
- Iniciar sesión necesita conexión siempre; la sesión ya iniciada y la cola de cambios de F006 siguen
  como están.
