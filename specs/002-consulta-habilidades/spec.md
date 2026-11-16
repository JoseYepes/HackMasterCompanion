# Feature Specification: Datos y consulta de habilidades (F001)

**Feature Branch**: `002-consulta-habilidades`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "F001 — Datos y consulta de habilidades. Como jugador, quiero buscar y filtrar habilidades, ver su ficha, descripciones, conjuntos y dudas. Aceptación: 343 habilidades, 36 conjuntos de habilidades y 23 conjuntos oficiales cargados; búsqueda insensible a acentos y mayúsculas; filtros por tipo, libro, clase, precio y dificultad; sin desbordamiento horizontal a 390 px y 820 px; el importador falla si un requisito no resuelve a una habilidad. Los datos pueden cambiar: deben poder regenerarse desde la hoja de cálculo fuente, que vive fuera del repositorio. El texto literal de los manuales (ManualText) no se publica ni se versiona; sí los datos estructurados y los resúmenes propios. Las dudas abiertas se muestran y se registran en docs/dudas.md."

## Clarifications

### Session 2026-11-16

- Q: ¿Entran en F001 los 5 estilos de combate que la hoja contiene junto a los conjuntos? → A: Sí;
  se importan y se consultan en F001 como una sección más.
- Q: ¿Qué hace el importador con un requisito que nombra una habilidad que no existe con ese
  nombre (hoy hay unos 6 casos en la hoja, como "Disfraz" por "Disfrazarse")? → A: Falla siempre;
  el mantenedor corrige los nombres en la hoja. No hay lista de excepciones.
- Q: ¿Cuál es el único lugar donde se escriben y mantienen las dudas abiertas? → A: La pestaña
  "Dudas" de la hoja. `docs/dudas.md` y la página de dudas se generan en la importación.
- Q: ¿En qué texto busca el buscador de habilidades? → A: En el nombre y en el resumen propio;
  las coincidencias por nombre aparecen primero.
- Q: ¿Se pueden marcar varios valores dentro de un mismo filtro (por ejemplo dos libros)? → A: Sí;
  dentro de un filtro basta con cumplir uno, y entre filtros distintos hay que cumplirlos todos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Buscar y filtrar habilidades (Priority: P1)

Como jugador, en la mesa y con el móvil, quiero escribir parte del nombre de una habilidad y
acotar la lista por tipo, libro, clase, precio y dificultad, para encontrar en segundos la
habilidad que necesito sin abrir el manual ni la hoja de cálculo.

**Why this priority**: Es el uso principal de la aplicación y lo que sustituye a la web estática
y al Excel. Sin lista, búsqueda y filtros no hay nada que consultar.

**Independent Test**: Con los datos cargados, buscar "botanica" (sin tilde) y comprobar que
aparece "Botánica"; aplicar filtros y comprobar que el número de resultados coincide con el
recuento hecho a mano sobre la hoja de cálculo.

**Acceptance Scenarios**:

1. **Given** la aplicación recién abierta, **When** el jugador entra en la consulta, **Then** ve
   la lista de las 343 habilidades con su nombre, tipo, coste en PC y libro, y el número total de
   resultados.
2. **Given** la lista completa, **When** escribe "botanica", "BOTÁNICA" o "botán", **Then** la
   lista muestra "Botánica" en los tres casos, antes que cualquier habilidad que solo mencione la
   palabra en su resumen.
3. **Given** la lista completa, **When** elige el tipo "Artes marciales", **Then** solo quedan las
   habilidades de ese tipo y el contador muestra cuántas son.
4. **Given** un filtro de tipo activo, **When** añade un filtro de libro y un precio máximo,
   **Then** solo quedan las habilidades que cumplen todas las condiciones a la vez.
5. **Given** el filtro de clase "Grupo de ladrones", **When** se aplica, **Then** aparecen las
   habilidades permitidas a ese grupo, incluidas las permitidas a "Todas".
6. **Given** una búsqueda sin coincidencias, **When** se muestra la lista, **Then** aparece un
   mensaje que lo indica y una forma visible de quitar la búsqueda y los filtros.
7. **Given** una pantalla de 390 px o de 820 px de ancho, **When** el jugador usa la lista, la
   búsqueda y los filtros, **Then** no hay desplazamiento horizontal en ningún momento.
8. **Given** una palabra que aparece en el resumen de una habilidad pero no en su nombre, **When**
   el jugador la escribe, **Then** esa habilidad aparece en los resultados con el fragmento del
   resumen que contiene la palabra.
9. **Given** el filtro de libro, **When** el jugador marca "MJ" y "CG", **Then** aparecen las
   habilidades de cualquiera de los dos libros; **When** además marca un tipo, **Then** solo
   quedan las de esos libros que son de ese tipo.

---

### User Story 2 - Ver la ficha de una habilidad (Priority: P2)

Como jugador, quiero abrir una habilidad y ver todos sus datos, su descripción resumida, sus
requisitos, los conjuntos en los que aparece y las dudas abiertas que la afectan, para saber qué
hace, cuánto cuesta y de dónde sale el dato.

**Why this priority**: La lista dice qué habilidades existen; la ficha es lo que el jugador
necesita leer para decidir. Depende de la historia 1 para llegar a ella.

**Independent Test**: Abrir la ficha de una habilidad con requisito, conjunto y duda conocidos
(por ejemplo "Herbalismo" y "Cerrajería") y comparar cada dato con la hoja de cálculo.

**Acceptance Scenarios**:

1. **Given** la lista, **When** el jugador elige una habilidad, **Then** ve su nombre, tipo,
   coste en PC, características relevantes, dado de maestría, requisitos, precio (sin separador
   de miles), dificultad, libro (acrónimo y página) y clases permitidas.
2. **Given** una habilidad con resumen propio, **When** se abre su ficha, **Then** se muestra el
   resumen; **Given** una sin resumen, **Then** se indica "pendiente de verificar" y no se
   muestra ningún texto inventado.
3. **Given** una habilidad cuyo requisito es otra habilidad, **When** se abre su ficha, **Then**
   el requisito es un enlace a la ficha de esa habilidad.
4. **Given** una habilidad que forma parte de uno o más conjuntos, **When** se abre su ficha,
   **Then** se listan esos conjuntos con enlace a cada uno.
5. **Given** una habilidad con una duda abierta, **When** se abre su ficha, **Then** se ve un
   aviso con la duda, su fuente y lo que se hizo provisionalmente.
6. **Given** una ficha abierta desde una lista filtrada, **When** el jugador vuelve atrás,
   **Then** recupera la búsqueda y los filtros que tenía.
7. **Given** la dirección de una ficha, **When** se abre directamente o se recarga, **Then** carga
   esa ficha.

---

### User Story 3 - Consultar los conjuntos de habilidades (Priority: P3)

Como jugador, quiero ver los conjuntos de habilidades (los 36 definidos con las reglas de
HJ 4.05 y los 23 oficiales), con sus valores y, cuando se conocen, las habilidades que los
componen, para comparar comprar un conjunto con comprar sus habilidades sueltas.

**Why this priority**: Aporta valor propio, pero se apoya en la ficha de habilidad y es la base
del creador de conjuntos de F002.

**Independent Test**: Abrir el conjunto "Alquimista" y comprobar componentes y valores contra la
hoja de cálculo; abrir un conjunto oficial y comprobar valores y libro.

**Acceptance Scenarios**:

1. **Given** la consulta de conjuntos, **When** el jugador entra, **Then** ve los 36 conjuntos de
   habilidades y los 23 oficiales, distinguidos entre sí, con nombre, coste en PC, precio,
   dificultad y libro.
2. **Given** un conjunto con componentes conocidos, **When** se abre, **Then** se listan sus
   habilidades con enlace a la ficha de cada una.
3. **Given** un conjunto que incluye otro conjunto como componente, **When** se abre, **Then** ese
   componente enlaza al conjunto correspondiente.
4. **Given** un conjunto oficial del que la fuente no detalla los componentes, o un conjunto con
   valores sin fuente ("?"), **When** se abre, **Then** lo que falta aparece como "pendiente de
   verificar".
5. **Given** la lista de conjuntos, **When** el jugador busca por nombre, **Then** la búsqueda se
   comporta igual que la de habilidades (sin distinguir acentos ni mayúsculas).
6. **Given** la consulta de estilos de combate, **When** el jugador entra, **Then** ve los 5
   estilos con sus características, dado de maestría, requisitos, precio, dificultad y libro, y
   los requisitos que son habilidades enlazan a su ficha.

---

### User Story 4 - Ver las dudas abiertas (Priority: P4)

Como jugador o director de juego, quiero una página con todas las dudas abiertas sobre los datos
(qué se duda, de qué fuente, qué se hizo mientras tanto), para saber qué datos no son firmes y
poder resolverlos con el manual delante.

**Why this priority**: Da transparencia sobre la fiabilidad de los datos (principios I y II),
pero la consulta funciona sin ella.

**Independent Test**: Comparar la página de dudas con `docs/dudas.md` y comprobar que cada duda
enlaza con la habilidad o el conjunto afectado.

**Acceptance Scenarios**:

1. **Given** la página de dudas, **When** se abre, **Then** lista cada duda con el elemento
   afectado, la fuente, la duda y la decisión provisional.
2. **Given** una duda sobre una habilidad o un conjunto existente, **When** el jugador la pulsa,
   **Then** llega a la ficha correspondiente.
3. **Given** `docs/dudas.md`, **When** se compara con la página, **Then** contienen las mismas
   dudas.

---

### User Story 5 - Regenerar los datos desde la hoja de cálculo (Priority: P1)

Como mantenedor, quiero regenerar con un solo comando los datos de la aplicación a partir de mi
hoja de cálculo, y que el proceso se detenga con un mensaje claro si los datos son incoherentes,
para poder corregir la hoja y republicar sin tocar los datos a mano.

**Why this priority**: Sin datos no hay consulta; y sin validación, los errores de la hoja llegan
a la mesa de juego. Es requisito previo de todas las demás historias.

**Independent Test**: Ejecutar la importación sobre la hoja real y comprobar los recuentos;
ejecutarla sobre copias con un error introducido a propósito y comprobar que falla indicando
dónde.

**Acceptance Scenarios**:

1. **Given** la hoja de cálculo fuente actual, **When** el mantenedor ejecuta la importación,
   **Then** se generan los datos de 343 habilidades, 36 conjuntos de habilidades, 23 conjuntos
   oficiales y 5 estilos de combate, y se muestra un resumen con esos recuentos.
2. **Given** una hoja en la que un requisito nombra una habilidad que no existe, **When** se
   importa, **Then** el proceso falla, no deja datos a medias e indica la pestaña, la fila y el
   nombre que no se encuentra.
3. **Given** una hoja en la que un componente de un conjunto no existe como habilidad ni como
   conjunto, **When** se importa, **Then** el proceso falla con el mismo detalle.
4. **Given** una hoja con dos habilidades de igual nombre (sin distinguir acentos ni mayúsculas),
   **When** se importa, **Then** el proceso falla e indica las filas duplicadas.
5. **Given** datos escritos de formas distintas pero equivalentes (dado "d8" y "8", precio
   "250 po" y "250", requisito "n" y "N"), **When** se importa, **Then** se guardan en una única
   forma normalizada.
6. **Given** un valor que no puede interpretarse o que falta sin que la fuente lo aporte, **When**
   se importa, **Then** el dato queda marcado como "pendiente de verificar", aparece en el resumen
   de avisos y no se sustituye por un valor inventado.
7. **Given** la misma hoja importada dos veces, **When** se comparan los resultados, **Then** son
   idénticos.
8. **Given** los datos generados, **When** se revisan, **Then** no contienen ningún texto literal
   de manual (`ManualText`).

---

### Edge Cases

- Requisitos compuestos o condicionales ("A, B", "A o B", "Al menos dos de: …", "Sabiduría 14+",
  "otra maniobra especial al 101 %+"): cada nombre de habilidad mencionado debe resolverse y
  enlazarse; las condiciones que no son habilidades se conservan como texto.
- Requisitos anotados como "(sin verificar)" o "?": se muestran como "pendiente de verificar" y
  se registran como duda; no bloquean la importación.
- Valores de dado de maestría que no corresponden a un dado existente (la hoja contiene "5", "3"
  y "2"), "NA" y "*": no se corrigen en silencio; se conservan, se marcan y se registran como duda.
- Un conjunto con datos incompletos en la hoja (por ejemplo "Jinete", sin coste, precio ni
  dificultad): se carga con lo que hay y lo demás queda "pendiente de verificar".
- Habilidades con precio, dificultad o PC "NA", o coste con anotación ("1 R"): se muestran tal
  como indica la fuente y quedan fuera de los filtros numéricos que no les aplican.
- Dificultades negativas (−5 %) y dificultades escritas como fracción (0,1 en lugar de 10 %).
- La misma habilidad escrita con distinta grafía en pestañas diferentes ("Leer/Escribir" y
  "Leer/escribir"; "saber animal" y "Saber animal"): se considera la misma.
- Nombres largos (más de 60 caracteres) y listas largas de clases en pantallas de 390 px.
- La hoja de cálculo no está, la ruta no está configurada o falta una pestaña: la importación
  falla con un mensaje que dice qué falta y cómo indicarlo.
- La aplicación se abre sin conexión después de una primera visita: fuera del alcance de F001
  (ver Assumptions).

## Requirements *(mandatory)*

### Functional Requirements

**Datos e importación**

- **FR-001**: El mantenedor DEBE poder regenerar todos los datos de juego de la aplicación desde
  la hoja de cálculo fuente con un único comando documentado, sin editar los datos a mano.
- **FR-002**: La hoja de cálculo fuente DEBE permanecer fuera del repositorio; su ubicación se
  indica en la configuración del equipo del mantenedor y no se escribe en el repositorio.
- **FR-003**: Los datos generados DEBEN incluir, para cada habilidad: nombre, tipo, coste en PC,
  características relevantes, dado de maestría, requisitos, precio, dificultad, libro con página,
  clases permitidas y resumen propio.
- **FR-004**: Los datos generados NO DEBEN incluir el texto literal de los manuales, y ese texto
  NO DEBE aparecer en la aplicación publicada ni en el repositorio.
- **FR-005**: La importación DEBE fallar, sin dejar datos parciales, cuando un nombre de habilidad
  citado en un requisito o como componente de un conjunto no se corresponde con ninguna habilidad
  o conjunto existente, e indicar pestaña, fila y nombre. Cada parte de un requisito compuesto
  DEBE ser una habilidad o conjunto existente, o una condición reconocida que no es una habilidad
  (característica mínima, competencia con un arma, nivel de maestría, "al menos N de"); cualquier
  otra cosa es un error. No existe lista de excepciones: los nombres se corrigen en la hoja. La
  única salvedad es un requisito anotado en la hoja como "(sin verificar)" o "?", que se trata
  como duda (FR-009). La importación DEBE informar de todos los errores de una vez, no solo del
  primero.
- **FR-006**: La importación DEBE fallar cuando hay nombres de habilidad o de conjunto duplicados,
  cuando falta una pestaña o una columna esperada, o cuando una habilidad de la pestaña de
  descripciones no existe en la de habilidades.
- **FR-007**: La comparación de nombres en la importación y en la búsqueda NO DEBE distinguir
  acentos, mayúsculas ni espacios sobrantes.
- **FR-008**: La importación DEBE normalizar las grafías equivalentes de un mismo valor (dados,
  precios con unidad, indicadores de sí/no, dificultad en fracción o en porcentaje).
- **FR-009**: Un valor ausente o no interpretable NO DEBE inventarse ni corregirse en silencio:
  DEBE quedar marcado como "pendiente de verificar" y aparecer en el resumen de la importación.
- **FR-010**: La importación DEBE mostrar al terminar un resumen con los recuentos por tipo de
  elemento y la lista de avisos, y DEBE producir el mismo resultado si se repite con la misma hoja.
- **FR-011**: Los datos generados DEBEN quedar versionados en el repositorio, de modo que cada
  cambio de datos sea revisable antes de publicarse.

**Consulta de habilidades**

- **FR-012**: Los jugadores DEBEN poder ver la lista completa de habilidades y el número de
  resultados que muestra en cada momento.
- **FR-013**: Los jugadores DEBEN poder buscar con coincidencia parcial en el nombre y en el
  resumen propio, sin distinguir acentos ni mayúsculas, y ver los resultados mientras escriben.
  Mientras hay texto de búsqueda, las habilidades que coinciden por nombre DEBEN aparecer antes
  que las que solo coinciden por resumen, y cada resultado que solo coincide por resumen DEBE
  mostrar el fragmento donde está la coincidencia.
- **FR-014**: Los jugadores DEBEN poder filtrar por tipo, libro, clase permitida, precio y
  dificultad, combinar varios filtros con la búsqueda, ver qué filtros están activos y quitarlos
  todos con una sola acción. En los filtros de tipo, libro y clase se pueden marcar varios valores
  a la vez: dentro de un mismo filtro basta con cumplir uno de los valores marcados; entre filtros
  distintos hay que cumplirlos todos.
- **FR-015**: El filtro de clase DEBE incluir las habilidades permitidas a "Todas" además de las
  permitidas expresamente a la clase o grupo elegido.
- **FR-016**: Los filtros de precio y de dificultad DEBEN funcionar por rango (mínimo y máximo);
  las habilidades sin valor numérico quedan excluidas mientras ese filtro esté activo.
- **FR-017**: Las opciones de cada filtro DEBEN proceder de los datos cargados, no de una lista
  fija.
- **FR-018**: La lista DEBE poder ordenarse al menos por nombre (por defecto), coste en PC, precio
  y dificultad.

**Fichas, conjuntos y dudas**

- **FR-019**: Cada habilidad DEBE tener una ficha con todos los datos de FR-003, los conjuntos en
  los que aparece y las dudas abiertas que la afectan, con dirección propia que se pueda abrir
  directamente.
- **FR-020**: Los requisitos y componentes que son habilidades o conjuntos DEBEN mostrarse como
  enlaces a su ficha; las condiciones que no lo son se muestran como texto.
- **FR-021**: Los jugadores DEBEN poder consultar los 36 conjuntos de habilidades y los 23
  conjuntos oficiales, distinguidos entre sí, con sus valores tal como figuran en la fuente y sus
  componentes cuando la fuente los detalla.
- **FR-022**: Los jugadores DEBEN poder consultar todas las dudas abiertas, con el elemento
  afectado, la fuente, la duda y la decisión provisional, enlazadas con las fichas afectadas.
- **FR-023**: La pestaña "Dudas" de la hoja es el único lugar donde el mantenedor escribe dudas.
  La importación DEBE generar `docs/dudas.md` y los datos de la página de dudas a partir de esa
  pestaña más las dudas que ella misma detecte (FR-009), distinguiendo unas de otras;
  `docs/dudas.md` no se edita a mano. Una duda de la pestaña que nombra un elemento inexistente
  es un error de referencia (FR-005).
- **FR-024**: Los jugadores DEBEN poder consultar los 5 estilos de combate de la hoja como una
  sección propia, con sus valores, requisitos y libro tal como figuran en la fuente, y con las
  mismas reglas de búsqueda, enlaces y marcas que los conjuntos. La importación los carga y valida
  igual que el resto de elementos.

**Presentación**

- **FR-025**: Toda la interfaz DEBE estar en castellano; los precios se muestran sin separador de
  miles y los libros como acrónimo y página.
- **FR-026**: Todas las pantallas de F001 DEBEN poder usarse a 390 px y a 820 px de ancho sin
  desplazamiento horizontal.
- **FR-027**: Todo dato marcado como "pendiente de verificar" DEBE verse como tal allí donde se
  muestre.

### Key Entities *(include if feature involves data)*

- **Habilidad**: lo que un personaje puede aprender. Nombre único, tipo (12 tipos en la hoja
  actual), coste en PC, una o varias características relevantes, dado de maestría, requisitos,
  precio en po, dificultad en %, libro y página, clases o grupos permitidos, resumen propio.
- **Requisito**: condición para aprender una habilidad. Puede ser ninguna, una o varias
  habilidades (todas o alguna de ellas) y condiciones que no son habilidades (característica
  mínima, competencia con un arma, nivel de maestría).
- **Conjunto de habilidades**: agrupación con nombre definida con las reglas de HJ 4.05; tiene
  componentes (habilidades u otros conjuntos) y valores propios de coste, característica, precio,
  dificultad y clases.
- **Conjunto oficial**: conjunto publicado en los manuales; tiene valores y libro, y puede no
  tener componentes detallados en la fuente.
- **Estilo de combate**: forma de luchar que se aprende como una habilidad; tiene características
  relevantes, dado de maestría, requisitos (habilidades y condiciones), precio, dificultad y
  libro. No tiene coste en PC en la fuente ("NA").
- **Duda**: discrepancia o laguna en las fuentes. Elemento afectado, fuente, descripción y
  decisión provisional.
- **Marca "pendiente de verificar"**: indica que un dato concreto falta o no es firme.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Tras importar la hoja actual, la aplicación muestra exactamente 343 habilidades, 36
  conjuntos de habilidades, 23 conjuntos oficiales y 5 estilos de combate.
- **SC-002**: Un jugador encuentra y abre la ficha de una habilidad cuyo nombre conoce en menos de
  10 segundos desde que abre la aplicación en el móvil.
- **SC-003**: Los resultados de búsqueda y filtros se actualizan en menos de medio segundo tras
  cada cambio, con los 343 elementos cargados, en un móvil medio.
- **SC-004**: El 100 % de las búsquedas de un nombre escrito sin tildes o con otras mayúsculas
  devuelve la habilidad correspondiente.
- **SC-005**: En una muestra de 30 habilidades y 10 conjuntos elegidos al azar, todos los datos
  mostrados coinciden con la hoja de cálculo.
- **SC-006**: Ninguna pantalla de F001 presenta desplazamiento horizontal a 390 px ni a 820 px.
- **SC-007**: Cada uno de los errores de datos de FR-005 y FR-006, introducido a propósito en una
  copia de la hoja, detiene la importación con un mensaje que permite localizar la fila en menos
  de un minuto.
- **SC-008**: La regeneración completa de los datos termina en menos de un minuto en el equipo del
  mantenedor.
- **SC-009**: Una búsqueda automática en el repositorio y en el sitio publicado no encuentra
  ningún texto de la columna de texto de manual.
- **SC-010**: Todo requisito o componente que nombra una habilidad o un conjunto existente es un
  enlace que lleva a su ficha (0 enlaces rotos).

## Assumptions

- **Hoja fuente**: `Hackhabilidades_completo.xlsx`, con las pestañas "Habilidades",
  "Conjuntos de habilidades", "Dudas" y "Descripciones". Observado el 2026-11-16: los 36 conjuntos
  de habilidades, los 23 conjuntos oficiales y los 5 estilos de combate están todos en la pestaña
  "Conjuntos de habilidades", como bloques sucesivos; no hay pestañas separadas.
- **Valores de los conjuntos**: en F001 se muestran tal como figuran en la hoja. Recalcularlos con
  las reglas de HJ 4.05 y comprobar que coinciden pertenece a F002.
- **Conjuntos oficiales sin componentes**: la hoja solo da sus valores y el libro; se muestran
  así y los componentes quedan "pendiente de verificar".
- **Requisitos en los conjuntos**: la pestaña de conjuntos solo indica si un componente tiene
  requisito ("S"/"N"); el requisito concreto se toma de la pestaña de habilidades.
- **Qué detiene la importación**: los errores de estructura y de referencias (FR-005, FR-006). Los
  valores dudosos o ausentes no la detienen: se marcan y se avisan (FR-009).
- **Dudas de `docs/PRODUCTO.md`**: las de la sección A5 que no están en la pestaña "Dudas"
  (Pellizcar; estilos de artes marciales; Maniobra Ka-Bob y Desenfunde veloz) las añade el
  mantenedor a la hoja una vez; el agente le prepara el texto.
- **Primera importación**: la hoja actual contiene requisitos que no resuelven ("Disfraz",
  "Mapa mental", "Religión", "Herramientas de carpintería", "Trabajar la madera (Carpintería)",
  "Primeros auxilios", entre otros). El mantenedor los corrige en la hoja, con la lista que le da
  el importador, antes de que F001 pueda cumplir SC-001.
- **Filtro de clase**: las opciones son los grupos y clases que aparecen en la hoja ("Grupo de
  guerreros", "Bárbaro (clase)"…). No se deduce que una clase pertenece a un grupo, porque esa
  relación no está en la hoja.
- **Sin conexión**: el uso sin conexión (aplicación instalable) no forma parte de F001; sigue
  aplazado como tarde a F004, según `docs/ESTADO.md`. F001 no requiere servidor.
- **Sin cuentas ni edición**: la consulta es de solo lectura y no guarda nada del usuario. El
  estado de búsqueda y filtros se conserva al navegar dentro de la aplicación, no entre visitas.
- **Idioma de los nombres**: se muestran como están en la hoja, incluido el original inglés entre
  paréntesis cuando lo lleva.
- **Texto literal de manuales en la aplicación**: no entra en F001; es una decisión pendiente
  recogida en `docs/ESTADO.md`.
- **Dependencias**: la base de F000 (construcción, pruebas, verificación automática y publicación)
  está terminada y en uso.
