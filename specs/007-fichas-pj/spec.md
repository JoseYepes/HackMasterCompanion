# Feature Specification: Fichas de PJ (F004)

**Feature Branch**: `007-fichas-pj`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "F004 — Fichas de PJ. Como jugador, quiero crear, editar, duplicar, borrar, exportar e importar fichas. Aceptación: exportar e importar deja la ficha idéntica; migración de esquema v1→v2 con test; funciona sin conexión."

## Clarifications

### Session 2026-11-19

El usuario pidió el 2026-11-19 que el agente tome las decisiones dentro de cada fase y le pregunte
solo al terminarla. Estas aclaraciones las decidió el agente; el usuario puede cambiarlas.

- Q: ¿Qué campos tiene una ficha? → A: Los del borrador de `PRODUCTO.md` B3 (nombre, raza, clase,
  nivel, alineamiento, características, PG, Honor, habilidades con su maestría y puntos, equipo y
  notas), con **siete** características: las seis del borrador más Aspecto (ASP), que es la séptima
  que ya usa la aplicación en las habilidades. Las reglas (costes, PC disponibles, tiradas) son de
  F005; aquí solo se guardan los valores que escribe el jugador.
- Q: ¿Raza, clase y alineamiento se eligen de una lista? → A: No: son texto libre. La aplicación no
  tiene esas listas en sus datos y no se inventan (constitución I). Las habilidades sí se eligen de
  las 343 de los datos.
- Q: ¿Qué es la versión 1 del formato, si las fichas no existían? → A: El formato del borrador B3
  (seis características). La versión 2, la que escribe la aplicación, añade Aspecto; un archivo de
  la versión 1 se importa con Aspecto vacío. La migración se prueba como en el historial de tiradas.
- Q: ¿Qué significa «funciona sin conexión» en F004? → A: Igual que en F007 (SC-004): crear, editar
  y guardar fichas no hace ninguna petición a internet ni necesita servidor. Hacer la aplicación
  instalable y que abra sin conexión (trabajador de servicio) sigue pendiente aparte, sin
  funcionalidad asignada (`docs/ESTADO.md`).
- Q: ¿Qué pasa al importar una ficha que ya está en el dispositivo? → A: Se pregunta si sustituir
  la que hay o añadirla como copia; si no está, se añade tal cual.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Crear y editar una ficha (Priority: P1)

Como jugador, quiero crear la ficha de mi personaje, rellenar sus datos y encontrarla igual al
volver otro día.

**Why this priority**: Sin crear y guardar no hay nada más que hacer con las fichas.

**Independent Test**: Abrir «Fichas», crear una ficha llamada «Grimbold», rellenar raza, clase,
nivel, características, PG, Honor, dos habilidades, equipo y notas; recargar la página y ver los
mismos datos.

**Acceptance Scenarios**:

1. **Given** ninguna ficha guardada, **When** el jugador abre «Fichas», **Then** ve que no hay
   fichas y un botón para crear una.
2. **Given** el formulario de una ficha nueva, **When** guarda sin nombre, **Then** se le pide un
   nombre y no se guarda.
3. **Given** una ficha guardada, **When** cambia un dato y guarda, **Then** la lista muestra el
   cambio y al recargar sigue ahí.
4. **Given** el editor, **When** añade una habilidad, **Then** la busca por nombre (sin importar
   acentos ni mayúsculas) entre las de los datos y puede indicar su maestría y sus puntos.
5. **Given** una ficha con una habilidad que ya no existe en los datos, **When** la abre,
   **Then** la habilidad se muestra marcada como no encontrada y no se pierde.

---

### User Story 2 - Exportar e importar una ficha (Priority: P1)

Como jugador, quiero guardar una ficha en un archivo y cargarla en otro dispositivo o navegador,
para no perderla y poder pasarla.

**Why this priority**: Es la única forma de copia de seguridad hasta F006, y es criterio de
aceptación.

**Independent Test**: Exportar «Grimbold», borrarla, importar el archivo y comprobar que la ficha
es idéntica a la exportada.

**Acceptance Scenarios**:

1. **Given** una ficha, **When** el jugador pulsa «Exportar», **Then** el navegador guarda un
   archivo con el nombre de la ficha.
2. **Given** ese archivo, **When** lo importa en un dispositivo sin la ficha, **Then** la ficha
   aparece idéntica (todos sus campos).
3. **Given** el archivo de una ficha que ya está en el dispositivo, **When** lo importa,
   **Then** puede elegir sustituirla o añadirla como copia.
4. **Given** un archivo que no es una ficha, o está dañado, **When** lo importa, **Then** ve un
   mensaje claro y no cambia ninguna ficha.
5. **Given** un archivo del formato antiguo (versión 1), **When** lo importa, **Then** la ficha
   aparece con Aspecto vacío y el resto igual.
6. **Given** un archivo de una versión más reciente de la aplicación, **When** lo importa,
   **Then** se le dice que la aplicación es antigua y no se importa.

---

### User Story 3 - Duplicar y borrar fichas (Priority: P2)

Como jugador, quiero duplicar una ficha para probar variantes y borrar las que ya no uso.

**Why this priority**: Comodidad; las fichas básicas ya funcionan sin esto.

**Independent Test**: Duplicar «Grimbold», ver «Grimbold (copia)» con los mismos datos, borrarla
tras confirmar y ver que la original sigue.

**Acceptance Scenarios**:

1. **Given** una ficha, **When** pulsa «Duplicar», **Then** aparece otra con los mismos datos y
   el nombre «(copia)» al final, independiente de la original.
2. **Given** una ficha, **When** pulsa «Borrar», **Then** se le pide confirmación; si confirma,
   desaparece; si no, sigue igual.

---

### Edge Cases

- El navegador no deja guardar (modo privado, espacio lleno): se avisa, como en los conjuntos, y
  las fichas se conservan mientras la página esté abierta.
- Lo guardado está dañado o es de una versión más reciente: se avisa y no se escribe nada para
  no perder las fichas.
- Dos pestañas abiertas: guardar en una no borra lo que guardó la otra.
- Nombres repetidos: se permiten (dos personajes pueden llamarse igual); cada ficha tiene su
  identificador.
- Valores numéricos: nivel, características, PG y Honor admiten vacío («sin rellenar»); los
  negativos solo en Honor y PG.
- Textos largos (equipo, notas) y nombres largos no desbordan a 390 px.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La aplicación DEBE tener una página «Fichas» en el menú con la lista de fichas
  guardadas (nombre, raza, clase y nivel) y un botón para crear una.
- **FR-002**: El jugador DEBE poder crear y editar una ficha con: nombre (obligatorio), raza,
  clase, nivel, alineamiento, las siete características (FUE, DES, CON, INT, SAB, CAR, ASP), PG,
  Honor, habilidades (habilidad de los datos, maestría y puntos), equipo y notas.
- **FR-003**: Las habilidades de una ficha DEBEN elegirse entre las de los datos con búsqueda
  insensible a acentos y mayúsculas; una habilidad que ya no exista DEBE conservarse y mostrarse
  como no encontrada.
- **FR-004**: Las fichas DEBEN guardarse en el dispositivo y seguir ahí al recargar o volver otro
  día, sin conexión ni servidor.
- **FR-005**: El jugador DEBE poder duplicar una ficha; la copia es independiente y su nombre
  termina en «(copia)».
- **FR-006**: El jugador DEBE poder borrar una ficha tras confirmarlo.
- **FR-007**: El jugador DEBE poder exportar una ficha a un archivo y, al importarlo, obtener una
  ficha idéntica.
- **FR-008**: Al importar una ficha que ya existe, el jugador DEBE poder elegir entre sustituirla o
  añadirla como copia.
- **FR-009**: Los archivos de ficha y lo guardado en el dispositivo DEBEN llevar versión de
  formato; los de la versión 1 DEBEN migrarse a la 2, y los de una versión más reciente o dañados
  DEBEN rechazarse con un mensaje, sin cambiar ninguna ficha.
- **FR-010**: Si lo guardado no se puede leer, la aplicación DEBE avisarlo y no escribir nada
  encima.
- **FR-011**: Las páginas de fichas NO DEBEN desbordar en horizontal a 390 px ni a 820 px y DEBEN
  verse bien en los dos modos de pantalla (F007).

### Key Entities

- **Ficha (Character)**: identificador, nombre, raza, clase, nivel, alineamiento, siete
  características, PG, Honor, habilidades, equipo, notas, fechas de creación y de cambio.
- **Habilidad de la ficha (CharacterSkill)**: referencia a una habilidad de los datos (y su nombre
  cuando se guardó, para mostrarla si desaparece), maestría y puntos.
- **Archivo de ficha**: una ficha con la versión de formato, para exportar e importar.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Exportar una ficha con todos los campos rellenos e importarla en un dispositivo sin
  ella da una ficha igual campo a campo (prueba automática).
- **SC-002**: Un archivo de la versión 1 se importa a la versión 2 sin perder datos (prueba
  automática por campo).
- **SC-003**: Crear, editar, duplicar, borrar, exportar e importar fichas no hace ninguna petición
  fuera del sitio (prueba de navegador).
- **SC-004**: Crear una ficha con nombre, clase y una habilidad lleva menos de 1 minuto a una
  persona que no conoce la aplicación.
- **SC-005**: Sin desbordamiento horizontal a 390 px y 820 px en la lista y en el editor.

## Assumptions

- Las fichas son de un solo jugador por dispositivo hasta F006 (cuentas).
- Las reglas (PC, costes, tiradas desde la ficha) llegan en F005; F004 no valida valores de juego
  más allá de que sean números.
- La maestría se guarda como número entero (porcentaje) y los puntos como número entero; F005
  decidirá cómo se calculan.
- Las características se guardan como número entero y, si el jugador la escribe, su parte
  fraccionaria (por ejemplo 12/45), pendiente de verificar el rango con el MJ en F005.
