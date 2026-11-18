# Feature Specification: Creador de conjuntos y tirador 2D (F002)

**Feature Branch**: `003-creador-conjuntos-dados`

**Created**: 2026-11-18

**Status**: Draft

**Input**: User description: "F002 — Creador de conjuntos y tirador 2D. Como jugador, quiero crear conjuntos con las reglas de HJ 4.05 y lanzar fórmulas de dados. Aceptación: el cálculo coincide con los 36 conjuntos del Excel; el parser (2d6+3, varios grupos) rechaza entradas inválidas con mensajes claros; historial persistente."

## Clarifications

### Session 2026-11-18

- Q: ¿El tirador admite los dados que penetran? → A: Sí, como opción que el jugador activa
  cuando quiera (qué tiradas penetran lo indican la Guía del Combatiente y el MJ; el tirador no lo
  decide). Regla del MJ («Daño de penetración»): si un dado saca su máximo se tira otro igual y se
  suma su resultado menos 1, y se repite mientras siga saliendo el máximo.
- Q: ¿Cómo se calcula el dado de maestría de un conjunto? → A: La media de los dados de los
  componentes redondeada a la baja al dado existente (d4, d6, d8, d10, d12, d20). Zoólogo,
  Escribano, Hechicero y Cerrajero no siguen esa regla en la hoja: son excepciones que se
  registran como duda.
- Q: ¿Cuántas habilidades puede tener un conjunto? → A: Entre 2 y 5, según HJ 4.05 tal como lo
  recuerda el usuario (pendiente de verificar en el HJ, que está escaneado). En la hoja,
  Forjador de armaduras y Trovador tienen 6: se registra como duda.
- Q: ¿Dónde aparecen los conjuntos que crea el jugador? → A: Junto a los 36 conjuntos de
  habilidades, en la misma lista y sin distinguirlos: todos son conjuntos del usuario (los 36 los
  hizo él con las reglas de HJ 4.05). Se pueden usar como componente de otro conjunto. Solo se
  diferencian en que los creados en la aplicación se pueden modificar y borrar desde ella; los 36
  se cambian en la hoja. Los 23 conjuntos oficiales siguen como en F001.
- Q: ¿Qué clases pueden comprar un conjunto creado? → A: Se calcula sola: solo las clases que
  pueden comprar todas sus habilidades ("Todas" si ninguna tiene restricción).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tirar dados con una fórmula (Priority: P1)

Como jugador, quiero escribir una fórmula de dados ("2d6+3", "1d20+1d6-2") y ver el resultado
con el detalle de cada dado, para resolver tiradas en la mesa sin dados físicos.

**Why this priority**: Es útil por sí solo en cada sesión de juego y no depende de nada más.

**Independent Test**: Escribir "2d6+3", tirar y comprobar que el total es la suma de los dos dados
más 3 y que se ve el valor de cada dado; escribir "2d" y comprobar que se rechaza con un mensaje
que explica qué falta.

**Acceptance Scenarios**:

1. **Given** el tirador, **When** el jugador escribe "2d6+3" y tira, **Then** ve el total y el
   valor de cada uno de los dos dados, y el total es su suma más 3.
2. **Given** una fórmula con varios grupos ("1d20+1d6-2"), **When** tira, **Then** cada grupo se
   tira por separado, se ve el detalle por grupo y el total los combina con los signos escritos.
3. **Given** la opción de dados que penetran activada y "2d4+1", **When** un d4 saca 4, **Then** se
   tira otro d4 y se suma su resultado menos 1, se repite si vuelve a salir 4, el detalle muestra
   cada dado extra y el +1 se suma una sola vez. Con la opción desactivada no se tira ningún dado
   extra.
4. **Given** una fórmula no válida ("2d", "d", "3x6", "2d6+", "0d6", "2d0"), **When** intenta
   tirar, **Then** no se tira nada y se ve un mensaje en castellano que indica qué parte está mal
   y un ejemplo de fórmula válida.
5. **Given** una fórmula con espacios o mayúsculas ("2D6 + 3"), **When** tira, **Then** se acepta
   igual que "2d6+3".
6. **Given** una fórmula correcta, **When** el jugador vuelve a tirar, **Then** obtiene una tirada
   nueva con la misma fórmula sin volver a escribirla.

---

### User Story 2 - Crear un conjunto de habilidades (Priority: P1)

Como jugador, quiero elegir varias habilidades (o conjuntos) y ver al momento cuánto costaría el
conjunto que forman según las reglas de HJ 4.05, para decidir si me conviene comprarlo en lugar
de las habilidades sueltas.

**Why this priority**: Es la razón de ser de la funcionalidad y la base de las fichas (F004-F005).

**Independent Test**: Elegir las habilidades del conjunto "Alquimista" y comprobar que el coste en
PC, el precio, la dificultad y el dado de maestría calculados coinciden con los del conjunto
"Alquimista" de la consulta de conjuntos.

**Acceptance Scenarios**:

1. **Given** el creador de conjuntos, **When** el jugador añade habilidades buscándolas por nombre
   (sin distinguir acentos ni mayúsculas), **Then** se ven los componentes elegidos con su coste,
   precio, dificultad y dado, y los valores del conjunto se recalculan con cada cambio.
2. **Given** un conjunto en creación, **When** se ve su resumen, **Then** el coste en PC es la suma
   de los PC de los componentes menos 1, el precio es la suma de los precios multiplicada por 1,1 y
   redondeada, y la dificultad es la mayor de los componentes más 5.
3. **Given** un conjunto en creación, **When** se ve su resumen, **Then** el dado de maestría se
   calcula según la regla de FR-008, y si algún componente no tiene dado numérico se explica cómo
   afecta al cálculo.
4. **Given** un componente con un valor "pendiente de verificar" (coste, precio, dificultad o
   dado), **When** forma parte del conjunto, **Then** el valor del conjunto que depende de él se
   muestra también como "pendiente de verificar", sin inventar el dato que falta.
5. **Given** un conjunto creado, **When** el jugador le pone nombre y lo guarda, **Then** aparece en
   la lista de conjuntos de habilidades junto a los 36, con el mismo aspecto; puede abrirlo,
   cambiarlo o borrarlo, y sigue ahí tras cerrar y volver a abrir la aplicación en el mismo
   dispositivo.
6. **Given** otro conjunto (de los 36, creado en la aplicación u oficial), **When** el jugador lo
   añade como componente, **Then** cuenta con sus propios valores, como hace la hoja en los
   conjuntos que incluyen otro conjunto.

---

### User Story 3 - Historial de tiradas (Priority: P2)

Como jugador, quiero ver mis últimas tiradas aunque cierre la aplicación, para comprobar un
resultado o repetir una fórmula que uso a menudo.

**Why this priority**: Mejora el tirador, pero el tirador ya es útil sin historial.

**Independent Test**: Hacer tres tiradas, cerrar y volver a abrir la aplicación, y comprobar que
las tres aparecen en el historial en orden, con fórmula, detalle, total y hora.

**Acceptance Scenarios**:

1. **Given** varias tiradas hechas, **When** el jugador abre el historial, **Then** las ve de la
   más reciente a la más antigua con fórmula, detalle, total y fecha y hora.
2. **Given** el historial, **When** el jugador cierra y vuelve a abrir la aplicación en el mismo
   dispositivo, **Then** el historial sigue ahí.
3. **Given** una tirada del historial, **When** el jugador la elige, **Then** la fórmula se copia al
   tirador para volver a tirarla.
4. **Given** el historial, **When** el jugador lo borra y confirma, **Then** queda vacío.
5. **Given** un historial que llega al máximo de FR-016, **When** se hace una tirada más, **Then**
   se descarta la más antigua.

---

### Edge Cases

- Fórmulas extremas: muchos dados ("1000d6"), dados enormes ("1d1000000") o números enormes; se
  rechazan las que superen los límites de FR-004 con un mensaje que diga el límite.
- Fórmula vacía o solo con espacios: no se tira y se indica que hay que escribir una fórmula.
- Modificador solo ("+3") o fórmula sin dados ("5"): se rechaza porque no hay dados (FR-002).
- Conjunto con menos de 2 o más de 5 componentes (FR-010): se calcula, pero no se puede guardar.
- La misma habilidad añadida dos veces al mismo conjunto: no se permite; se avisa.
- Componente cuyo dado es "NA" o "*" (sin dado numérico): no cuenta para el dado del conjunto;
  si ningún componente tiene dado numérico, el dado del conjunto queda "pendiente de verificar".
- Componente con coste "NA" (no se compra con PC): el coste del conjunto queda "pendiente de
  verificar".
- Datos del juego actualizados tras guardar un conjunto (una habilidad cambia de precio o
  desaparece): el conjunto guardado se recalcula con los datos vigentes al abrirlo, y un componente
  que ya no existe se señala y no cuenta.
- Almacenamiento del navegador no disponible o lleno: la aplicación sigue funcionando y avisa de
  que los conjuntos y el historial no se pueden guardar.
- Datos guardados con un formato anterior: se migran al formato actual sin perderlos.

## Requirements *(mandatory)*

### Functional Requirements

**Tirador de dados**

- **FR-001**: El tirador DEBE aceptar fórmulas formadas por uno o más términos unidos por "+" o
  "-": grupos de dados "NdX" (N dados de X caras; N se puede omitir y vale 1) y números enteros.
  Ignora espacios y mayúsculas.
- **FR-002**: Una fórmula DEBE contener al menos un grupo de dados.
- **FR-003**: Una fórmula no válida NO DEBE tirarse. El mensaje DEBE estar en castellano, señalar
  la posición o el trozo erróneo e incluir un ejemplo válido.
- **FR-004**: El tirador DEBE limitar la cantidad de dados por fórmula, las caras de un dado y el
  valor de los números a unos máximos razonables para el juego (a fijar en el plan), y rechazar lo
  que los supere con un mensaje que diga el límite.
- **FR-005**: Cada dado DEBE dar un valor entre 1 y su número de caras con la misma probabilidad,
  y el resultado DEBE mostrar el valor de cada dado, el subtotal de cada grupo y el total.
- **FR-006**: El tirador DEBE tener una opción, que el jugador activa o desactiva cuando quiera,
  para que los dados penetren: cada dado que saca su máximo añade otro dado igual cuyo resultado
  menos 1 se suma, y se repite mientras siga saliendo el máximo (MJ, «Daño de penetración»). Los
  modificadores no penetran. El detalle DEBE mostrar cada dado extra. Los dados tienen al menos 2
  caras (FR-004), y la cadena tiene un límite de seguridad (a fijar en el plan) que, si se
  alcanza, se indica.
- **FR-007**: El jugador DEBE poder repetir la última tirada sin volver a escribir la fórmula.

**Creador de conjuntos**

- **FR-008**: El dado de maestría de un conjunto DEBE calcularse con la media del número de caras
  de los dados de sus componentes (como mínimo 4), redondeada a la baja al dado existente (d4, d6,
  d8, d10, d12, d20) más grande que no la supere. En la hoja, Zoólogo, Escribano y Hechicero suman
  1 a la media y Cerrajero tiene el dado escrito a mano: son las excepciones documentadas, y se
  registran como duda en la hoja.
- **FR-009**: El coste en PC, el precio y la dificultad del conjunto DEBEN calcularse como en
  US2-2: suma de PC − 1; suma de precios × 1,1 redondeada al entero más próximo; mayor dificultad
  + 5.
- **FR-010**: Un conjunto DEBE tener entre 2 y 5 componentes para poder guardarse (HJ 4.05,
  pendiente de verificar). Con otro número se sigue calculando, pero no se puede guardar y se
  explica por qué. Los conjuntos de 6 componentes de la hoja (Forjador de armaduras, Trovador) se
  siguen consultando como hasta ahora y quedan registrados como duda.
- **FR-011**: Los componentes DEBEN poder ser habilidades y conjuntos (los 36 de la hoja, los
  creados en la aplicación y los oficiales); un conjunto como componente aporta sus propios
  valores. No se permite repetir un componente ni que un conjunto se incluya a sí mismo, directa
  o indirectamente.
- **FR-012**: Las características relevantes del conjunto las DEBE elegir el jugador entre las de
  sus componentes; el requisito del conjunto DEBE mostrarse como "las de las habilidades que lo
  componen", con la lista de requisitos de cada componente.
- **FR-012b**: Las clases permitidas del conjunto DEBEN calcularse solas: las que pueden comprar
  todos sus componentes; si ningún componente tiene restricción, "Todas". Si las restricciones no
  se pueden combinar con los datos disponibles (por ejemplo, una clase concreta y un grupo de
  clases distinto, sin saber si la clase pertenece al grupo), las clases del conjunto se muestran
  como "pendiente de verificar" junto con la lista de restricciones de los componentes.
- **FR-013**: Un valor que dependa de un dato "pendiente de verificar" DEBE mostrarse como
  "pendiente de verificar" (principio I).
- **FR-014**: El jugador DEBE poder guardar, abrir, cambiar el nombre, modificar y borrar sus
  conjuntos; borrar pide confirmación. Los conjuntos creados en la aplicación DEBEN aparecer en la
  lista de conjuntos de habilidades junto a los 36 de la hoja, sin distinguirse de ellos salvo
  por poder modificarse y borrarse. Un nombre igual al de otro conjunto de habilidades no se
  permite. Borrar un conjunto que es componente de otro avisa de cuáles se verán afectados.
- **FR-015**: El cálculo del conjunto DEBE coincidir con los 36 conjuntos de habilidades de la
  hoja en coste, precio, dificultad y dado (salvo los que la aclaración de FR-008 deje como
  excepción documentada).

**Historial y guardado**

- **FR-016**: El historial DEBE guardar como máximo las 100 tiradas más recientes, con fórmula,
  detalle, total y fecha y hora, y permitir borrarlo entero con confirmación.
- **FR-017**: Los conjuntos y el historial DEBEN guardarse en el dispositivo, sin cuenta ni
  conexión, y seguir disponibles al volver a abrir la aplicación.
- **FR-018**: Lo guardado DEBE llevar versión de formato y DEBE poder migrarse a versiones nuevas
  sin perder datos (principio V).
- **FR-019**: Si no se puede guardar, la aplicación DEBE seguir funcionando y avisar al jugador.

**Interfaz**

- **FR-020**: Tirador, creador e historial DEBEN usarse en 390 px y 820 px de ancho sin
  desbordamiento horizontal, con toda la interfaz en castellano.
- **FR-021**: El tirador y el creador DEBEN tener dirección propia y aparecer en el menú.

### Key Entities *(include if feature involves data)*

- **Fórmula de dados**: texto escrito por el jugador; se interpreta como una lista de términos con
  signo (grupos de dados y números).
- **Tirada**: resultado de tirar una fórmula: valor de cada dado, subtotal por grupo, total, fecha
  y hora.
- **Conjunto del jugador**: nombre, componentes (referencias a habilidades o conjuntos de la
  consulta), características elegidas, fecha de creación y de cambio. Sus valores no se guardan: se
  calculan con los datos vigentes.
- **Historial**: lista ordenada de tiradas, con un máximo de FR-016.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para cada uno de los 36 conjuntos de habilidades de la hoja, crear un conjunto con
  los mismos componentes da el mismo coste en PC, precio, dificultad y dado de maestría (con las
  excepciones documentadas de FR-008).
- **SC-002**: El 100 % de una lista de al menos 20 fórmulas no válidas se rechaza con mensaje, y el
  100 % de una lista de al menos 20 válidas se acepta.
- **SC-003**: En 10 000 tiradas de prueba de "1d6", cada cara sale entre el 15 % y el 18,4 % de las
  veces, y ninguna tirada da un valor fuera de 1-6.
- **SC-004**: Un jugador hace una tirada desde que abre el tirador en menos de 10 segundos, y crea
  y guarda un conjunto de 4 habilidades en menos de 2 minutos.
- **SC-005**: Tras cerrar y volver a abrir la aplicación, el 100 % de los conjuntos guardados y de
  las tiradas del historial siguen disponibles.
- **SC-006**: Ninguna pantalla nueva se desborda en horizontal a 390 px ni a 820 px.

## Assumptions

- "Persistente" significa guardado en el dispositivo y navegador del jugador; ver los datos en
  otros dispositivos llega con las cuentas de F006.
- Las reglas de cálculo se toman de las fórmulas de la hoja del usuario, que las recoge de HJ 4.05;
  el HJ 4.05 disponible está escaneado y no se puede leer automáticamente. Las discrepancias entre
  conjuntos de la hoja se registran como duda.
- Los conjuntos del jugador son privados y no se comparten ni se exportan en F002 (exportar llega
  con las fichas en F004).
- El tirador es en 2D (resultados en texto y números); los dados 3D llegan en F003.
- Tirar desde una habilidad o desde la ficha queda fuera: llega con F005.
- El tamaño máximo del historial (100) es una elección razonable; se puede cambiar sin afectar al
  resto.
