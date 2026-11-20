# Feature Specification: Grimorio (F009)

**Feature Branch**: `009-grimorio`

**Created**: 2026-11-20

**Status**: Draft

**Input**: User description: "F009 — Grimorio. Como jugador, quiero buscar y filtrar los conjuros de clérigo y de mago, ver su ficha y consultar las reglas de los grimorios y de la transcripción de conjuros, igual que consulto las habilidades. Fuentes: Manual del Jugador en castellano y en inglés (para cotejar erratas), los manuales de erratas y la guía de magos (SSG)."

## Clarifications

### Session 2026-11-20

- Q: ¿Cómo llegan los conjuros a la aplicación? → A (usuario): «Quiero que leas los conjuros del
  manual y los subas tú, por tandas». El agente los lee del MJ (cotejando cada uno con la imagen de
  su página) y los escribe en una hoja nueva, `Hackconjuros.xlsx`, junto a la de habilidades y
  fuera del repositorio; el importador la convierte en `data/spells.json`, como las habilidades
  (constitución V). La hoja de habilidades del usuario no se toca.
- Q: ¿Descripciones? → A (usuario): «Si no puedes copiar las descripciones ya te daré yo una en
  documento». El texto del manual no se publica (constitución I); hasta que llegue ese documento,
  cada conjuro lleva un resumen propio del agente, corto y con libro y página.
- Q: ¿Todos de golpe o por tandas? → A (usuario): por tandas publicables. Tanda 1: la pestaña
  Grimorio, las reglas de libros de conjuros y transcripción, y los conjuros de nivel 1 de mago y de
  clérigo. Después, nivel a nivel; los trucos y la SSG al final.

Decididas por el agente:

- Solo conjuros de mago y de clérigo (los dos con su escuela de magia: en los de clérigo el MJ la da «a
  efectos de referencia», p. 253); los de druida del
  Apéndice D quedan fuera hasta que el usuario los pida.
- Lo que no se lea con seguridad en la página se marca «pendiente de verificar» y va a las dudas.
- Se aplican las erratas oficiales (`HM-FullErrata.pdf`, `Erratas Manual del Jugador`); si cambian
  un dato, se indica en el conjuro.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buscar conjuros (Priority: P1)

Como jugador, quiero buscar un conjuro por nombre y filtrar por clase, nivel y escuela,
para encontrarlo rápido en la mesa.

**Independent Test**: Abrir «Grimorio», escribir «proyectil» y ver «Proyectil mágico» (mago, nivel
1); filtrar por clérigo y nivel 1 y ver solo esos.

**Acceptance Scenarios**:

1. **Given** el Grimorio, **When** escribo parte del nombre sin acentos ni mayúsculas, **Then**
   aparecen los conjuros que lo contienen.
2. **Given** el Grimorio, **When** filtro por clase, nivel o escuela, **Then** solo aparecen
   los que cumplen todos los filtros, y los filtros quedan en la dirección para compartirla.
3. **Given** la lista, **Then** cada conjuro muestra nombre, clase, nivel y escuela.

---

### User Story 2 - Ficha del conjuro (Priority: P1)

Como jugador, quiero abrir un conjuro y ver todos sus datos y un resumen, con el libro y la página.

**Acceptance Scenarios**:

1. **Given** un conjuro, **When** lo abro, **Then** veo clase, nivel, escuela, alcance,
   componentes, duración, tiempo de lanzamiento, área de efecto, tirada de salvación, si es
   reversible, libro y página y el resumen.
2. **Given** un dato pendiente de verificar, **Then** se marca como en las habilidades.

---

### User Story 3 - Reglas de los grimorios (Priority: P2)

Como jugador, quiero consultar las reglas de los libros de conjuros y de la transcripción de
conjuros, resumidas y con su página, sin buscar en el manual.

**Acceptance Scenarios**:

1. **Given** el Grimorio, **When** abro «Reglas», **Then** veo los resúmenes con libro y página.

### Edge Cases

- Conjuros con el mismo nombre en mago y clérigo: son dos fichas distintas.
- Conjuros reversibles: se indica en la ficha.
- Datos que dependen del nivel del lanzador («1 asalto por nivel»): se muestran como texto.
- Sin conjuros de un nivel todavía (tandas): la lista lo dice («aún no cargados»).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Pestaña «Grimorio» en el menú con la lista de conjuros cargados.
- **FR-002**: Búsqueda por nombre insensible a acentos y mayúsculas.
- **FR-003**: Filtros por clase, nivel y escuela, combinables y guardados en la dirección.
- **FR-004**: Ficha de cada conjuro con todos los campos de US2 y su resumen propio.
- **FR-005**: Página de reglas de libros de conjuros y transcripción, resumidas con libro y página.
- **FR-006**: Los datos se generan con el importador desde `Hackconjuros.xlsx`; el texto del manual
  no se versiona ni se publica.
- **FR-007**: El importador falla si falta un dato obligatorio (nombre, clase, nivel, libro) o si
  dos conjuros de la misma clase tienen el mismo nombre.
- **FR-008**: Sin desbordamiento a 390 y 820 px en los dos modos.

### Key Entities

- **Conjuro (Spell)**: nombre, clase (mago o clérigo), nivel, escuelas, alcance,
  componentes, duración, tiempo de lanzamiento, área de efecto, tirada de salvación, reversible,
  libros y páginas, resumen propio, datos pendientes.
- **Regla del grimorio**: título, resumen propio, libro y página.

## Success Criteria *(mandatory)*

- **SC-001**: Tanda 1: todos los conjuros de nivel 1 de mago y de clérigo del MJ cargados, cada uno
  cotejado con su página.
- **SC-002**: Encontrar un conjuro por nombre lleva menos de 10 s.
- **SC-003**: Ningún texto literal del manual en el repositorio ni en el sitio publicado.
- **SC-004**: Sin desbordamiento a 390 y 820 px.

## Assumptions

- La estructura de los datos se calca de la del MJ (Apéndices C y D).
- Las tandas siguientes no cambian el formato: solo añaden filas a la hoja.
