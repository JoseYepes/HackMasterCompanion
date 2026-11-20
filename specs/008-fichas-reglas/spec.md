# Feature Specification: Fichas con reglas (F005)

**Feature Branch**: `008-fichas-reglas`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "F005 — Fichas con reglas. Como jugador, quiero costes, PC disponibles y tiradas de habilidad desde la ficha. Aceptación: un test por regla, con casos de los manuales referenciados por libro y página en el nombre del test."

## Reglas del Manual del Jugador

La constitución (principios I y II) prohíbe inventar reglas. La aplicación ya tiene, por habilidad,
su coste en PC, sus características, su dado de maestría y su dificultad del curso (de la hoja).
Las reglas generales se han leído en el MJ en castellano (`D:\…\Manuales de jugador`, solo en
local):

1. **PC (MJ p. 75 y 80; cap. 2 y 3)**: los PC se usan **al crear el personaje**. Salen de la raza
   («Puntos de Construcción bonificados», cap. 2) y de la clase («Bonificador a los Puntos de
   Construcción», cap. 3; por ejemplo, guerrero 10 PC, p. 42), y se pueden ganar más con defectos
   y peculiaridades (2 PC menor y 4 PC mayor si se eligen; según la Tabla 6A si se tiran) y
   sacrificando características (2 PC por punto, sin bajar de 3 ni de los mínimos de raza o clase).
   Se gastan en habilidades, talentos y competencias y en repetir tiradas de las tablas de
   historial (1 PC cada una). Los que sobran se cambian por 25 po o 25 puntos fraccionales cada uno;
   los no cambiados se pierden. Al subir de nivel las habilidades se aprenden entrenando (cap. 11),
   no con PC.
2. **Maestría (MJ p. 100–101 y 307)**: maestría = característica relevante (una sola vez; si hay
   varias, su media redondeada a la baja) + una tirada del dado de maestría y una del Dado de Honor
   **por cada vez que se paga el coste en PC** de la habilidad; máximo 125 %. Los magos (salvo los
   magos de batalla) pagan el doble por las habilidades de combate y la mitad por las académicas.
   El Dado de Honor depende del Honor y del nivel (Tabla 5C, p. 85).
3. **Prueba de habilidad (MJ p. 100 y Glosario)**: se tira d100 contra la maestría; la tirada,
   con sus modificadores, debe ser igual o menor que la maestría. Un 01–05 siempre tiene éxito y
   un 98–00 siempre falla. La «dificultad» de la hoja es la del curso de entrenamiento (p. 307), no
   un modificador de la prueba.

## Clarifications

### Session 2026-11-20

Decididas por el agente (el usuario pidió más autonomía el 2026-11-19); el usuario puede cambiarlas.

- Q: ¿La aplicación busca los PC de la raza y de la clase? → A: No en F005: raza y clase son texto
  libre desde F004 y las cifras están repartidas por los capítulos 2 y 3. El jugador escribe en la
  ficha los PC de raza, de clase, de defectos y peculiaridades, de características sacrificadas y
  los gastados en repetir tiradas; la aplicación suma, resta lo gastado en habilidades y dice cuántos
  quedan. Elegir raza y clase de una lista con sus PC queda como mejora posterior.
- Q: ¿El Dado de Honor se calcula con la Tabla 5C? → A: No en F005: la tabla tiene unas 80 filas
  por 20 niveles y copiarla a mano tiene mucho riesgo de error. El jugador elige su Dado de Honor
  en la ficha (de d3 a d20, con los «+1» y «+2» de la tabla). Calcularlo queda como mejora
  posterior.
- Q: ¿Qué habilidades cuentan como «de combate» para los magos? → A: Las de tipo «Procedimiento de
  combate» de la hoja; «Académicas» son las académicas. Si «Artes marciales» también cuenta como
  combate queda **pendiente de verificar** (se avisa en la ficha).
- Q: ¿Cómo sabe la aplicación que el personaje es mago? → A: Una casilla en la ficha, «Paga como
  mago», que el jugador marca (los magos de batalla no la marcan).

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

- **Ficha (Character)**, de F004, con datos nuevos: PC de raza, de clase, de defectos y
  peculiaridades, de características sacrificadas y gastados en repetir tiradas; Dado de Honor;
  «Paga como mago».
- **Habilidad de la ficha**: de F004, con las veces que se ha pagado su coste (regla 2).

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
