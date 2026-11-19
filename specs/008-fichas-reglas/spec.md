# Feature Specification: Fichas con reglas (F005)

**Feature Branch**: `008-fichas-reglas`

**Created**: 2026-11-19

**Status**: Draft — a la espera de las reglas del manual (ver «Reglas pendientes de fuente»)

**Input**: User description: "F005 — Fichas con reglas. Como jugador, quiero costes, PC disponibles y tiradas de habilidad desde la ficha. Aceptación: un test por regla, con casos de los manuales referenciados por libro y página en el nombre del test."

## Reglas pendientes de fuente

La constitución (principios I y II) prohíbe inventar reglas. La aplicación ya tiene, por habilidad,
su coste en PC, sus características, su dado de maestría y su dificultad (de la hoja). Las reglas
generales siguientes **no están en la hoja** y hacen falta, con libro y página, antes de planificar:

1. **PC disponibles**: [NEEDS CLARIFICATION: ¿cuántos puntos de construcción (PC) tiene un personaje al crearse y de dónde salen (raza, clase, características bajas, otros), y cuántos gana al subir de nivel? Tabla y página del MJ.]
2. **Maestría al comprar una habilidad**: [NEEDS CLARIFICATION: ¿cómo se calcula la maestría inicial al comprar una habilidad (característica, media de varias, tirada del dado de maestría…) y qué pasa al comprarla otra vez? ¿Cambia el coste según la clase? Página del MJ.]
3. **Tirada de habilidad**: [NEEDS CLARIFICATION: ¿cómo se tira una habilidad (d100 contra la maestría, efecto de la dificultad, éxitos o fallos especiales)? Página del MJ.]

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver los PC gastados y los disponibles (Priority: P1)

Como jugador, quiero que la ficha me diga cuántos PC tengo, cuántos he gastado en habilidades y
cuántos me quedan, para crear y subir de nivel a mi personaje sin hacer cuentas a mano.

**Independent Test**: Crear una ficha con raza, clase, nivel y características, añadir dos
habilidades y comprobar que los PC gastados son la suma de sus costes y que los disponibles siguen
la regla del MJ (regla 1).

**Acceptance Scenarios**:

1. **Given** una ficha con habilidades, **When** la abro, **Then** veo los PC gastados (suma de
   los costes de las habilidades compradas, según la regla 2).
2. **Given** una ficha con los datos que pide la regla 1, **When** la abro, **Then** veo los PC
   disponibles y los que me quedan.
3. **Given** que gasto más PC de los que tengo, **When** guardo, **Then** se me avisa, pero se
   guarda (la ficha es del jugador; el Máster puede permitirlo).
4. **Given** que falta un dato para calcular (por ejemplo, la clase), **When** abro la ficha,
   **Then** los PC disponibles aparecen como «pendientes» con el motivo, no como 0.

---

### User Story 2 - Maestría de las habilidades (Priority: P1)

Como jugador, quiero que la ficha me ayude a calcular la maestría al comprar una habilidad, según
la regla del MJ, para no equivocarme.

**Independent Test**: Comprar una habilidad en una ficha con sus características rellenas y
comprobar que la maestría propuesta sigue la regla 2.

**Acceptance Scenarios**:

1. **Given** una ficha con las características de una habilidad, **When** la compro, **Then** la
   aplicación propone la maestría según la regla 2 (con la tirada que haga falta, usando el tirador
   de la aplicación) y puedo aceptarla o escribir otra.
2. **Given** una habilidad comprada, **When** la vuelvo a comprar, **Then** la maestría sube según
   la regla 2 y los PC gastados también.

---

### User Story 3 - Tirar una habilidad desde la ficha (Priority: P2)

Como jugador, quiero pulsar en una habilidad de mi ficha y tirarla, para no tener que ir al tirador
y comparar a mano.

**Independent Test**: En una ficha con «Botánica» al 35 %, tirarla y ver el resultado del dado, la
maestría con la dificultad aplicada y si es éxito o fallo según la regla 3.

**Acceptance Scenarios**:

1. **Given** una habilidad con maestría, **When** pulso «Tirar», **Then** veo el dado, la cifra
   que había que sacar y el resultado según la regla 3.
2. **Given** una tirada, **When** termina, **Then** queda en el historial del tirador (F002).

### Edge Cases

- Habilidades con dado de maestría no válido en los datos («pendiente de verificar»): la maestría
  no se propone; se escribe a mano.
- Habilidades con varias características: según la regla 2.
- Fichas creadas en F004 sin los datos nuevos: siguen abriéndose; lo que no se pueda calcular sale
  como «pendiente».
- Habilidades que ya no existen en los datos: no suman coste (no se conoce) y se avisa.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La ficha DEBE mostrar los PC gastados en habilidades, calculados con los costes de
  los datos (y la regla 2 si el coste depende de algo más).
- **FR-002**: La ficha DEBE mostrar los PC disponibles y los que quedan, según la regla 1; si falta
  un dato, como «pendiente» con el motivo.
- **FR-003**: Gastar más PC de los disponibles DEBE avisarse sin impedir guardar.
- **FR-004**: Al comprar una habilidad, la aplicación DEBE proponer la maestría según la regla 2, y
  el jugador DEBE poder cambiarla.
- **FR-005**: El jugador DEBE poder tirar una habilidad desde la ficha y ver el resultado según la
  regla 3; la tirada DEBE quedar en el historial del tirador.
- **FR-006**: Cada regla DEBE tener al menos una prueba con un caso del manual, con libro y página
  en el nombre de la prueba (criterio de aceptación de F005).
- **FR-007**: Las fichas de F004 DEBEN seguir abriéndose; si el formato cambia, con migración y
  prueba (constitución V).
- **FR-008**: Sin desbordamiento a 390 px y 820 px en los dos modos.

### Key Entities

- **Ficha (Character)**, de F004; puede necesitar datos nuevos según la regla 1 (por ejemplo, PC
  ganados por nivel o ajustes del Máster).
- **Habilidad de la ficha**: de F004; puede necesitar las veces que se ha comprado (regla 2).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para cada caso de ejemplo del manual, los PC disponibles, la maestría y el resultado
  de la tirada coinciden con el manual (pruebas automáticas, una por regla y caso).
- **SC-002**: Ver cuántos PC quedan en una ficha no requiere ninguna cuenta a mano.
- **SC-003**: Tirar una habilidad desde la ficha lleva un toque y muestra el resultado en menos de
  1 s (sin dados 3D).
- **SC-004**: Ninguna ficha guardada con F004 se pierde ni deja de abrirse.

## Assumptions

- Las reglas se toman del Manual del Jugador en castellano (MJ); si discrepa del inglés o de las
  erratas, se registra en `docs/dudas.md` (constitución II).
- La aplicación ayuda a calcular, pero el jugador puede escribir otros valores (el Máster manda).
