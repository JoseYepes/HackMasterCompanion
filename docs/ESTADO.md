# Estado del proyecto

> Nota de traspaso entre sesiones. Recoge lo que no está en la constitución, en `PRODUCTO.md` ni en
> las specs: decisiones tomadas de palabra, hallazgos y lo que queda por decidir. Actualízala al
> terminar cada sesión.

Última actualización: 2026-11-17

## Dónde estamos

- **F000 (Base del proyecto): terminada.** 28 de 28 tareas cerradas (`specs/001-base-proyecto/`).
  Pull requests #1, #2 y #3 fusionados. Sitio publicado en
  https://joseyepes.github.io/HackMasterCompanion/
- **En curso: F001 (Datos y consulta de habilidades).** Spec escrita y validada en
  `specs/002-consulta-habilidades/` (rama `002-consulta-habilidades`), con las aclaraciones de
  `/speckit-clarify` incorporadas, y plan técnico escrito (`plan.md`, `research.md`,
  `data-model.md`, `contracts/`, `quickstart.md`) y 71 tareas en `tasks.md`. Siguiente paso:
  `/speckit-analyze` y después `/speckit-implement` por fases.
- **Decisiones del plan de F001** (detalle en `research.md`): ClosedXML para leer la hoja; JSON en
  `data/` incrustado en la aplicación; modelo y búsqueda en `Domain`; proyecto de pruebas nuevo
  `tests/HackMaster.DataImport.Tests`; Playwright dentro de `Web.Tests`; CSS propio sin librería.
- **Implementación de F001**: fases 1 a 3 hechas (T001 a T029): modelo y normalización en
  `Domain`, importador completo en `tools/DataImport`. La fase 4 (primera importación real) espera a que el
  usuario corrija el Excel, decidido el 2026-11-17: mientras, se avanza con las pantallas.
- **Fase 5 hecha (T036 a T047, 2026-11-17)**: búsqueda y filtros en `Domain` (`SkillQuery`,
  `SkillFacets`), carga de datos (`GameData`), página `/habilidades` con la consulta en la
  dirección, navegación y CSS propio. Probado con datos de prueba (`tests/HackMaster.Web.Tests/
  TestData.cs`, valores inventados solo para las pruebas); en el navegador, la página sin datos no
  desborda a 390 ni a 820 px.
- **Fase 6 hecha (T048 a T053, 2026-11-17)**: ficha `/habilidades/{slug}` con todos los campos,
  marca de pendiente junto al valor original, requisitos enlazados (`PrerequisiteView`), conjuntos
  que la incluyen y aviso de dudas (`DoubtNotice`); un `slug` inexistente muestra "Página no
  encontrada". Sin datos reales aún no se ha visto en el navegador: queda para cuando se importe
  el Excel.
- **Fase 7 hecha (T054 a T058, 2026-11-17)**: `/conjuntos` (conjuntos de habilidades y oficiales
  por separado, búsqueda por nombre en la dirección), fichas `/conjuntos/{slug}` y
  `/conjuntos/oficiales/{slug}` (dos páginas que comparten `SuiteView`) con componentes enlazados,
  `/estilos` y `/estilos/{slug}`.
- **Fase 8 hecha (T059 y T060, 2026-11-17)**: `/dudas` separa las dudas de la hoja de las
  detectadas al importar; cada una enlaza con su ficha y las generales salen como "General" sin
  enlace. T061 espera a la primera importación real (T033). 239 pruebas en verde. Siguiente:
  fase 9 (Playwright, CI y entrega), que necesita los datos reales para varias tareas.
- **Fase 9 empezada (2026-11-17)**: Playwright incorporado a `Web.Tests` (T062) con las pruebas de
  desbordamiento y de flujo de habilidades escritas (T063, T064) pero **sin ejecutar**: necesitan
  los datos reales y Chromium (unos 150 MB, pendiente de que el usuario autorice la descarga).
  Instrucciones en `tests/README.md` (T065). El trabajo de CI para estas pruebas (T066) se añade
  cuando haya datos, porque antes fallaría. `CLAUDE.md` retocado con permiso del usuario
  (T030 cerrada): quitada la frase "El importador llega con F001" y Playwright remite a
  `tests/README.md`.
- **Botones en el móvil**: la pregunta con botones del final de cada turno solo se ve en la app de
  escritorio; desde el móvil (control remoto activado el 2026-11-17) el usuario responde por texto.
- **Ensayo del importador con la hoja real (2026-11-17, sin escribir en el repositorio)**: lee las
  cuatro pestañas y falla con 15 errores, todos de datos: 11 requisitos de "Habilidades" (filas
  48, 49, 50, 117, 173, 215, 262 ×2, 278, 282, 293), 2 de "Conjuntos de habilidades" (fila 245) y
  2 dudas (filas 2 "Justa" y 3 "granjero / mercader / noble") que no coinciden con ningún
  nombre. Repetir el comando del README da la lista exacta. El 2026-11-17 se entregó al usuario
  la corrección propuesta para cada fila (T031); quedan a su criterio "Religión" (filas 278 y 293:
  ¿general o específica?) y "Trabajar la madera (Carpintería)" (fila 282).
- **Dudas generales (decidido el 2026-11-17)**: una fila con "General" en la columna A de la
  pestaña "Dudas" registra una duda sin elemento concreto; se muestra sin enlace. Sirve para la
  duda de `PRODUCTO.md` sobre los estilos de artes marciales.
- **Corrección del Excel (T032), 2026-11-17**: a petición del usuario, el agente hizo los 12
  cambios mecánicos (nombres exactos en requisitos, errata "Coserser", conjunto de primeros
  auxilios, "Justa (Jousting)", la duda de granjero / mercader / noble dividida en tres filas y las
  cuatro dudas de `PRODUCTO.md` A5, incluida la "General" de los estilos de artes marciales). Antes
  guardó una copia de seguridad junto al original (`Hackhabilidades_completo.copia-2026-11-17.xlsx`).
  Después el usuario pidió resolver con el Manual del Jugador los tres requisitos que quedaban:
  - Filas 278 ("Religión (Especifica)") y 293 ("Saber divino"): "Religión" pasa a
    `Religión (general)`. En el MJ la ficha de la habilidad general se titula "Religión" (p. 315)
    y la tabla la llama "Religión (general)"; la específica amplía la general.
  - Fila 282 ("Reparación de escudos de madera", MJ 331): "Trabajar la madera (Carpintería)" pasa a
    `Carpintería`, la habilidad del MJ (p. 322) para trabajar la madera. **Interpretación del
    agente**: el usuario puede revisarla.
- **Fallo del importador corregido (2026-11-17)**: la hoja guarda el dado de maestría como número
  con el formato `\dGeneral` (se ve "d6") y ClosedXML lo leía como fecha, lo que dejaba 337 dados
  "pendientes de verificar". Ahora se lee el número.
- **Excel recalculado (2026-11-17)**: guardar con ClosedXML borró los resultados de las fórmulas
  (no las fórmulas). Al recalcular con LibreOffice se vio que los resultados del original ya
  estaban desactualizados (495 distintos: costes, precios y dificultades de conjuntos, dados,
  libros de los componentes). El usuario aceptó la versión recalculada. Lección: **no guardar el
  Excel con ClosedXML**; si hay que editarlo, recalcular y guardar con LibreOffice y comparar.
  También se quitó la «R» del coste de Birlar: en el MJ significa «restringida a algunas clases»,
  y eso ya está en «Clases permitidas».
- **Avisos revisados con el usuario (T033, 2026-11-17)**: el importador acepta ahora «ASP»
  (Aspecto) como característica y «NA» como «no aplica» (sin marca de pendiente). Los 41 avisos que
  quedan se aceptan tal cual: 8 dados inexistentes («5», «3», «2»), 4 requisitos sin verificar y 6
  campos de granjero / mercader / noble (dudas ya registradas) y los 23 conjuntos oficiales sin
  componentes.
- **Primera importación real (2026-11-17)**: `data/*.json` y `docs/dudas.md` generados (343
  habilidades, 36 conjuntos, 23 oficiales, 5 estilos; 9 dudas de la hoja y 41 detectadas; unos 4 s).
  Ningún archivo contiene `manualText`.
- **Bloque de comprobaciones (2026-11-17)**: `DataIntegrityTests` lee `data/` y `docs/dudas.md`
  (T034, T061; se comprobó que fallan con datos estropeados). Datos: 501 KB; importación: 2,4 s y
  repetible (T035, falta el paso 3 de `quickstart.md` con el usuario). Búsqueda: 169-231 ms con
  los datos reales (T067).
- **Pruebas de navegador (2026-11-17)**: con permiso del usuario se instaló Chromium para
  Playwright (unos 700 MB en disco). Las 6 pruebas `E2E` pasan con los datos reales en unos 24 s
  (T063, T064). Se añadió el trabajo `e2e` al CI (T066), que arranca la aplicación con `dotnet run`;
  no se ha visto aún en GitHub.
- **Revisión independiente (T069, 2026-11-17)**: sin restos de texto de manual, rutas personales ni
  versiones sin fijar. Corregido: un nombre desconocido seguido de "50%+" pasaba la importación
  como condición (ahora solo "… al 51%+" lo es); la comparación de direcciones en Habilidades y
  Conjuntos fallaba con tildes o espacios (no se pudo reproducir un efecto visible; quedan dos
  pruebas de navegador de vigilancia). Se renombraron 5 pruebas que citaban mal la regla.
  **Hallazgos menores sin corregir** (latentes con los datos actuales): un emoji en un resumen haría
  fallar el fragmento de la búsqueda (`SkillQuery`); `DataWriter` no es atómico si falla a mitad de
  la copia; una dificultad de 100 % en la hoja se leería como 1 %; la prueba de desbordamiento no
  visita nombres muy largos ni conjuntos oficiales; "Competencia …" mal escrita pasaría como
  condición.
- **Pendiente del usuario para más adelante**: los 23 conjuntos oficiales no tienen componentes en
  el Excel; el usuario cree que puede ser un error suyo al pasarlos del manual. Revisarlos con la
  fuente.
- El Excel es la única fuente de dudas; `docs/dudas.md` se genera. Cada fila de "Dudas" nombra un
  solo elemento con su nombre exacto, o dice "General".
- **`/speckit-analyze` pasado el 2026-11-16**: 9 hallazgos (2 altos), todos corregidos en los
  documentos. El más relevante: "Cartógrafo" existe como conjunto de habilidades y como conjunto
  oficial, así que los nombres son únicos por tipo y los oficiales tienen dirección propia
  (`/conjuntos/oficiales/{slug}`).

## Decisiones tomadas el 2026-11-16

- **Constitución v1.0.0 aprobada.** El principio I pasó de prohibir el texto literal de manuales a
  "Contenido sin inventar". La prohibición de texto literal se eliminó de todo el proyecto por
  decisión del usuario.
- **`ManualText` sigue siendo local**: no se versiona ni se publica. El repositorio y la web son
  públicos; van la aplicación, los datos estructurados y los resúmenes propios. Los manuales y el
  Excel se quedan en el equipo del usuario.
- **Pasajes de manuales**: el usuario los aportará más adelante en un documento aparte. Mostrar
  texto literal dentro de la aplicación sin publicarlo exigiría cargarlo desde un archivo local;
  es una funcionalidad a decidir, no está en ninguna spec.
- **.NET 10 (LTS)**, solución `HackMaster.slnx`, ruta base `/HackMasterCompanion/`.
- **IndexedDB**: la librería se decide en F004 con un ADR.
- **PWA y Playwright aplazados** (justificación en `specs/001-base-proyecto/plan.md`). Playwright
  debería entrar en F001, cuando haya interfaz; la PWA, como tarde en F004.
- **Sin Bootstrap**: se retiró el de la plantilla. Hay un CSS mínimo propio. Si se quiere una
  librería de estilos, se decide en F001.
- **Commit inicial en `main`**: excepción única a "siempre pull request".

## Alcance acordado para F001

- Entran las 343 habilidades, los 36 conjuntos de habilidades, **los 23 conjuntos oficiales y los
  5 estilos de combate** (estos últimos, decidido el 2026-11-16 al escribir la spec).
- Los datos pueden cambiar en el futuro: el importador debe poder regenerarlos desde el Excel.

## El Excel fuente

- Archivo: `Hackhabilidades_completo.xlsx`, fuera del repositorio. Su ruta está en la variable de
  entorno `HACKMASTER_EXCEL` del equipo del usuario (ya definida).
- Estructura observada el 2026-11-16 (solo cabeceras y recuentos):
  - `Habilidades`: 343 filas más cabecera. Columnas: habilidad, tipo, PC, característica relevante,
    dado de maestría, requisito, precio, dificultad, libro, clases permitidas.
  - `Conjuntos de habilidades`: 179 filas **sin fila de cabecera**; la primera fila ya es un dato.
  - `Dudas`: 3 dudas más cabecera. `PRODUCTO.md` (A5) lista 5: hay dos que no están en el Excel.
  - `Descripciones`: 343 filas más cabecera, con el resumen propio y el texto del manual
    (`ManualText`, que no debe salir del equipo).
- Los 23 conjuntos oficiales y los 5 estilos de combate están en la pestaña
  `Conjuntos de habilidades`, como bloques al final de los 36 conjuntos (cada bloque con su propia
  fila de título y de cabecera); no hay pestañas separadas.
- Anomalías observadas que el importador debe tratar (detalle en la spec de F001): 47 requisitos
  compuestos o condicionales ("A o B", "Sabiduría 14+"…); dados de maestría "5", "3" y "2";
  grafías mezcladas ("d8"/"8", "250 po", dificultad "0.1" por 10 %); el conjunto "Jinete" sin
  coste, precio ni dificultad; dos conjuntos que incluyen otro conjunto como componente; los
  conjuntos oficiales no detallan componentes.
- Existe un `Hackhabilidades.xlsx` antiguo (2025, unas 259 habilidades). No usarlo.

## Pendiente menor

- `docs/PRODUCTO.md` aún usa los nombres antiguos de los comandos (`/speckit.specify`,
  `/speckit.plan`) y menciona `.claude/commands`; lo actual es `/speckit-…` y `.claude/skills`.
- T021 de F000: no se comprobó en GitHub que una prueba rota deja el CI en rojo, por decisión del
  usuario; se observó en local.

## Cómo se trabajó y qué conviene saber

- Los scripts de Spec Kit instalados son de PowerShell (`.specify/scripts/powershell/`). En F000
  no se pudieron ejecutar y las plantillas se rellenaron a mano tras comprobar que eran las
  originales. En una sesión local en Windows deben ejecutarse con normalidad.
- El CI (`.github/workflows/ci.yml`) construye, prueba, comprueba formato y hace una publicación de
  prueba. `pages.yml` lo reutiliza y publica desde `main`. Las acciones están fijadas por SHA.
- El usuario no es desarrollador profesional: conviene explicar los términos (merge, CI, variable
  de entorno) y dar los pasos de uno en uno.
