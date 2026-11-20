# Feature Specification: Fichas con reglas (F005)

**Feature Branch**: `008-fichas-reglas`

**Created**: 2026-11-19

**Status**: Draft

**Input**: User description: "F005 — Fichas con reglas. Como jugador, quiero que la ficha calcule la maestría de mis habilidades y me deje tirarlas. Aceptación: un test por regla, con casos de los manuales referenciados por libro y página en el nombre del test." (Cambiada por el usuario el 2026-11-20: antes incluía costes y PC disponibles.)

## Reglas del Manual del Jugador

La constitución (principios I y II) prohíbe inventar reglas. Las reglas se han leído en el MJ en
castellano (solo en local, nunca en el repositorio):

1. **Maestría (MJ p. 100–101 y 307)**: maestría = característica relevante (una sola vez; si hay
   varias, su media redondeada a la baja) + la suma de las tiradas del dado de maestría y del Dado
   de Honor que se han ido añadiendo (una de cada por cada compra o bloque de entrenamiento
   superado); máximo 125 %. Ejemplo del MJ p. 100: Corte a la yugular (d4, DES) con Destreza 18,
   dado de maestría 3 y Dado de Honor d12 con 8 → 29 %.
2. **Prueba de habilidad (MJ p. 100 y Glosario)**: se tira d100; con sus modificadores, debe ser
   igual o menor que la maestría para tener éxito. Un 01–05 siempre tiene éxito y un 98–00 siempre
   falla.
3. **Niveles de maestría (MJ p. 100 y 307)**: 0 % ni idea; 01–25 principiante; 26–50 aprendiz;
   51–75 avanzado; 76–100 experto; 101–125 maestro.

## Clarifications

### Session 2026-11-20

- Nota del usuario: los PC solo sirven al crear el personaje; irán en una futura creación de PJ
  (F010 en `docs/PRODUCTO.md`), no en la ficha. Con ellos sale también la regla de los magos que
  pagan el doble o la mitad en PC (MJ p. 100), que es un coste en PC.

Decididas por el agente (el usuario pidió más autonomía el 2026-11-19); el usuario puede cambiarlas:

- Q: ¿El Dado de Honor se calcula con la Tabla 5C (MJ p. 85)? → A: No en F005: la tabla tiene unas
  80 filas por 20 niveles y copiarla a mano tiene mucho riesgo de error. El jugador elige su Dado de
  Honor en la ficha (los de la tabla: de d3 a d20, con sus «+1» y «+2»). Calcularlo queda como
  mejora posterior.
- Q: ¿Qué guarda la ficha de cada habilidad? → A: Las tiradas de mejora (dado de maestría + Dado
  de Honor) por separado de la característica, para que la maestría se recalcule si cambia la
  característica. Las fichas de F004 tienen una maestría escrita a mano: se conserva como
  «maestría escrita» hasta que el jugador pase a calcularla.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Maestría calculada (Priority: P1)

Como jugador, quiero que la ficha calcule la maestría de cada habilidad con mi característica y las
mejoras que he ido tirando, para no hacer cuentas.

**Independent Test**: En una ficha con Destreza 18 y Dado de Honor d12, añadir «Corte a la yugular»
(d4, DES), apuntar una mejora de 3 (maestría) + 8 (Honor) y ver 29 % (ejemplo del MJ p. 100).

**Acceptance Scenarios**:

1. **Given** una habilidad con una característica, **When** la añado, **Then** su maestría base es
   esa característica.
2. **Given** una habilidad con varias características, **When** la añado, **Then** su base es la
   media redondeada a la baja.
3. **Given** una habilidad, **When** pulso «Tirar mejora», **Then** la aplicación tira el dado de
   maestría y mi Dado de Honor, me enseña las dos tiradas y suma el total a la maestría.
4. **Given** una mejora que tiré con dados físicos, **When** la apunto a mano, **Then** se suma igual.
5. **Given** una maestría que pasaría de 125 %, **When** se suma, **Then** se queda en 125 %.
6. **Given** que cambio una característica, **When** vuelvo a la ficha, **Then** la maestría de las
   habilidades que la usan cambia.
7. **Given** una habilidad, **Then** veo su nivel (principiante, aprendiz…).

---

### User Story 2 - Tirar una habilidad desde la ficha (Priority: P1)

Como jugador, quiero pulsar en una habilidad de mi ficha y tirarla, para no tener que ir al tirador
y comparar a mano.

**Independent Test**: En una ficha con «Botánica» al 35 %, tirarla con un modificador de −10 y ver
el d100, la cifra que había que sacar y si es éxito o fallo.

**Acceptance Scenarios**:

1. **Given** una habilidad con maestría, **When** pulso «Tirar», **Then** veo el d100, el
   modificador, el total y si es éxito o fallo según la regla 2.
2. **Given** un 01–05 o un 98–00, **When** tiro, **Then** se indica que es éxito o fallo automático.
3. **Given** una tirada, **When** termina, **Then** queda en el historial del tirador (F002).

### Edge Cases

- Habilidades con dado de maestría no válido en los datos («pendiente de verificar»): no se puede
  tirar la mejora automática; se apunta a mano.
- Sin Dado de Honor elegido: la mejora automática pide elegirlo; a mano se puede apuntar igual.
- Característica vacía: la maestría sale como «pendiente» con el motivo.
- Habilidades que ya no existen en los datos: se conserva su maestría escrita y se pueden tirar.
- Fichas de F004: siguen abriéndose (migración con prueba).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La ficha DEBE calcular la maestría de cada habilidad según la regla 1, con tope de
  125 %, y mostrar su nivel según la regla 3.
- **FR-002**: El jugador DEBE poder elegir su Dado de Honor en la ficha.
- **FR-003**: El jugador DEBE poder añadir una mejora tirándola en la aplicación (dado de maestría
  más Dado de Honor) o escribiéndola a mano, y quitar una mejora apuntada por error.
- **FR-004**: El jugador DEBE poder tirar una habilidad con un modificador y ver el resultado según
  la regla 2; la tirada DEBE quedar en el historial del tirador.
- **FR-005**: Cada regla DEBE tener al menos una prueba con un caso del manual, con libro y página
  en el nombre de la prueba.
- **FR-006**: Las fichas de F004 DEBEN seguir abriéndose sin perder datos (versión 3 del formato,
  con migración y prueba).
- **FR-007**: Sin desbordamiento a 390 px y 820 px en los dos modos.

### Key Entities

- **Ficha (Character)**: de F004, con el Dado de Honor.
- **Habilidad de la ficha**: de F004, con la lista de mejoras (dado de maestría y Dado de Honor de
  cada una) y la maestría escrita a mano de F004, si la había.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El ejemplo del MJ p. 100 (29 %) y los casos de media y tope dan lo que dice el manual
  (pruebas automáticas).
- **SC-002**: Ver la maestría de una habilidad no requiere ninguna cuenta a mano.
- **SC-003**: Tirar una habilidad desde la ficha lleva un toque y da el resultado en menos de 1 s.
- **SC-004**: Ninguna ficha guardada con F004 se pierde ni deja de abrirse.

## Assumptions

- Si el MJ en castellano discrepa del inglés o de las erratas, se registra en la pestaña «Dudas»
  de la hoja (constitución II).
- El jugador manda: puede apuntar mejoras a mano (dados físicos, decisiones del Máster).
