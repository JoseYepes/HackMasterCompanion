# Estado del proyecto

> Nota de traspaso entre sesiones. Recoge lo que no está en la constitución, en `PRODUCTO.md` ni en
> las specs: decisiones tomadas de palabra, hallazgos y lo que queda por decidir. Actualízala al
> terminar cada sesión.

Última actualización: 2026-11-19

## Dónde estamos

- **F000 (Base del proyecto): terminada.** 28 de 28 tareas cerradas (`specs/001-base-proyecto/`).
  Pull requests #1, #2 y #3 fusionados. Sitio publicado en
  https://joseyepes.github.io/HackMasterCompanion/
- **F001 (Datos y consulta de habilidades): terminada y publicada.** Pull request #5 fusionado el
  2026-11-17; CI en verde, incluidas las 8 pruebas de navegador. Sitio publicado comprobado a
  tamaño de móvil (T071). Hechas 69 de 71 tareas. **Pospuestas por el usuario** (no bloquean):
  T035 (paso 3 de `quickstart.md`: buscar con `git grep` una frase del texto de manual) y T068
  (cotejar a mano unas filas con el Excel; un programa ya comparó 30 habilidades y 10 conjuntos al
  azar sin diferencias).
- **F002 (Creador de conjuntos y tirador 2D): terminada y publicada.** Pull request #6 fusionado el
  2026-11-18; CI en verde con las 10 pruebas de navegador; sitio publicado comprobado (T032).
  Queda T028 (dudas en la hoja, del usuario).
- **F003 (Dados 3D): terminada y publicada.** Pull request #7 fusionado el 2026-11-18 (a88806b);
  CI en verde, incluidas las pruebas de navegador del 3D con WebGL por software. Sitio publicado
  comprobado (T021): 7 tipos de dado con las caras del detalle, 3,7 s. T018: el usuario lo probó en
  su Samsung Galaxy (modelo desconocido) y «se ven muy bien»; fotogramas no medidos
  (`specs/004-dados-3d/research.md` §8). Todas las tareas cerradas.
- **F007 (Estilo de fantasía heroica): terminada y publicada.** Pull request #8 fusionado el
  2026-11-18 (9482acb); CI en verde; sitio publicado comprobado en los dos modos; el usuario lo
  probó en su móvil y «va muy bien» (T020). Se hizo antes de F004 por decisión del usuario. Rama
  `005-estilo-fantasia`, `specs/005-estilo-fantasia/`. Elegido sobre tres maquetas: «A con modo
  noche B» (pergamino y tinta; noche de taberna). Aclarado: mismas letras en los dos modos; un solo
  botón ☀/☾ que la primera vez sigue al dispositivo; mesa de dados de cuero granate (normal) y
  madera (noche). Letras Cinzel y EB Garamond (Fontsource 5.3.0, OFL) descargadas con permiso del
  usuario y comprobadas por SHA-256. **Hallazgo**: la aplicación aún no es instalable ni funciona
  sin conexión (no hay trabajador de servicio); SC-004 de F007 se ajustó a «sin peticiones
  externas». **Implementado (2026-11-18)**: T001–T017 hechas. Letras en `wwwroot/fonts/` (no en
  `lib/`, que vigila la prueba de F003 de no descargar el 3D); colores solo en variables de dos
  modos; script en el `<head>` de `index.html` que aplica el modo antes de pintar; botón
  `ThemeToggle`; mesa de cuero o madera solo con CSS. En el análisis se descartó la clase
  `ThemePreference` (nadie la usaría; constitución III). Al verlo en el móvil se cambió la letra de
  los nombres de habilidades, conjuntos y estilos a Garamond: en Cinzel (todo mayúsculas) los
  nombres largos se partían mal. Descarga inicial +3,7 %. Las pruebas de navegador nuevas no se
  vieron fallar antes de implementar (sí las unitarias y las de bUnit). **Revisión independiente
  (T018)**: corregido que el foco no se veía en la barra (granate sobre granate; ahora dorado), que
  los dados extra rojos casi desaparecían sobre el cuero (ahora dorados; la prueba lee los colores
  de `dice3d.js`), que el botón podía romper la página sin el script del modo, y ampliada la
  prueba de colores sueltos a todas las funciones y nombres de color de CSS. **Sin corregir**
  (menor): las pestañas no se avisan del cambio de modo; las demás lo toman al recargar.
  Pruebas: 556 unitarias y 27 de navegador.
- **Pendiente sin funcionalidad asignada**: hacer la aplicación instalable y que funcione sin
  conexión (PWA: trabajador de servicio y manifiesto), que pide `PRODUCTO.md` A1 y A2.
- **F008 (Dados personalizables y sonido)**, pedida por el usuario el 2026-11-18. Rama
  `006-dados-personalizables`, `specs/006-dados-personalizables/`. Aclarado con el usuario sobre un
  prototipo 3D: 11 materiales (los 8 del prototipo más madera, piedra y cristal), un aspecto para
  todos los dados, aura en los dados extra, sonido activado al empezar, efecto al penetrar
  («explotar») con explosión por defecto, fuego, sangre o ninguno. **Implementado (2026-11-18)**:
  aristas redondeadas sin cambiar la física (las caras quedan en los planos del poliedro);
  materiales, texturas y sonidos generados con código, sin descargas; panel «Aspecto de los
  dados» con vista previa; historial versión 3 con el aspecto; `BoardDie.Parent` para lanzar el
  efecto en el dado que penetró; toda la preparación de la tirada cuenta para los 4 s. Sin 3D la
  descarga crece un 0,25 % (SC-005 se ajustó de «no crece» a «menos de un 1 %»). Las pruebas de
  navegador se vieron fallar antes de implementar, salvo la de calidad baja (se hizo con los
  materiales). **Sin comprobar a ojo**: los efectos en movimiento (el panel del navegador no deja
  capturarlos); los verá el usuario. **Revisión independiente (T021)**, con prueba para cada
  corrección: el panel podía deshacer un instante la elección mientras la página guardaba (y
  perderla al pulsar rápido); un golpe sonaba varias veces (una por esquina y dos por choque entre
  dados); los sonidos seguían al cortar la tirada (ahora cada tirada tiene su salida de audio, que se
  apaga, con compresor); un d100 que penetra daba dos efectos; la vista previa podía quedar viva si
  se cerraba el panel mientras cargaba; el audio se activaba aunque el sonido estuviera apagado;
  el aura no destacaba en dados dorados (ahora es azul en esos); los números reflejaban el
  entorno con calidad baja; la caché de texturas no tenía tope; `shownEffects` dice ahora si el
  efecto llegó a verse. La animación máxima baja de 3,6 a 3,3 s: con lo que hace la página antes y
  después, una tirada llegó a 4,007 s. Fusionada (pull request #10). **Sonido grabado (2026-11-19)**:
  el usuario la probó en su móvil y dijo que los sonidos eran «demasiado cutres». Con su permiso se
  descargaron grabaciones CC0 (Kenney: Impact Sounds y Casino Audio; OpenGameArt: explosión,
  fuego y sangre), 38 archivos y 326 KB en `wwwroot/sounds/` con su README de licencias. Cada
  material tiene sus golpes (research §3) y solo se descargan al tirar con sonido. **Sin comprobar
  de oído**: Claude no puede escuchar; lo comprueba el usuario (T027). Ojo: los de piedra
  (`impactMining`) duran casi 1 s; si suenan largos, cambiarlos. **Revisión independiente**, con prueba para cada
  corrección: la tirada esperaba hasta 0,8 s a la descarga (podía pasar de 4 s; ahora no espera); con
  calidad baja la penetración no sonaba; lo que fallaba al descargar no se volvía a pedir; los últimos
  golpes se cortaban de golpe al acabar. Queda por comprobar en el móvil del usuario: si su navegador
  decodifica `.ogg` (los iPhone antiguos no). El usuario dijo que «no es del todo realista»: los dados giraban poco
  (el d4, 0,9 vueltas de campana; ahora entre 2,4 y 3,2, saliendo rodando) y el sonido apenas cambiaba con
  la fuerza (ahora el volumen varía unas 25 veces, los golpes flojos suenan apagados y el compresor
  ya no los iguala); research §3 y §3 bis. Pull request #11 fusionado el 2026-11-19 con la CI en
  verde. **Falta**: la prueba del usuario en su móvil (T023 y T027).
- **Forma de trabajar (2026-11-19)**: el usuario pidió más autonomía: el agente decide dentro de
  cada fase o tarea y le pregunta solo al terminarla. Siguen necesitando su permiso las descargas,
  las dependencias nuevas, ampliar el alcance, cambiar `CLAUDE.md` y crear recursos en Azure.
- **F004 (Fichas de PJ)**, empezada el 2026-11-19. Rama `007-fichas-pj`, `specs/007-fichas-pj/`.
  Aclaraciones decididas por el agente (el usuario puede cambiarlas): siete características (las
  seis de `PRODUCTO.md` B3 más Aspecto, que ya usaba la aplicación); raza, clase y alineamiento en
  texto libre (no hay listas en los datos y no se inventan); la versión 1 del formato es la del
  borrador B3 y la 2 añade Aspecto; «sin conexión» se comprueba como en F007 (ninguna petición fuera
  del sitio; el trabajador de servicio sigue pendiente); al importar una ficha que ya existe se
  pregunta si sustituirla o añadirla como copia. Guardado en IndexedDB con un módulo propio
  (`wwwroot/js/characters.js`), sin paquetes, como pide B1. **Implementado**: T001–T015; páginas
  `/fichas` y `/fichas/{id}`. Pruebas del dominio y de las páginas vistas fallar antes del código
  (las del servicio `PlayerCharacters` se compilaron junto con él). La migración 1 → 2 apenas hace
  nada: una característica que falta ya se lee vacía; la prueba que obliga al paso de migración es
  la de valores `null`. Los rangos de las características y de su fracción quedan pendientes de
  verificar con el MJ en F005. Ojo: en una pasada completa con las pruebas de navegador a la vez
  fallaron dos pruebas bUnit de los dados 3D que dependen del tiempo
  (`The_History_Does_Not_Show_The_Result_Before_The_Dice_Stop__FR005` y
  `Turning_3D_Off_And_On_Before_The_First_Load_Ends_Still_Starts_The_New_Board__FR011`); solas y
  sin navegador pasan siempre (5 de 5). **Falta**: revisión independiente, pull request y la prueba del
  usuario (T017–T019).
- **Orden acordado (2026-11-18)**: F008 → F004 → F005 → F006. Los números son identificadores, no
  el orden: F007 y F008 se añadieron después y tomaron el siguiente número libre para no renumerar
  las demás. **F006 ampliada** a petición del usuario: cuentas propias para guardar en la nube
  fichas, historial de tiradas, configuración de dados, conjuntos y modo de pantalla; sin sesión,
  todo sigue funcionando en el dispositivo.
- Historia de F003: rama `004-dados-3d`, spec en `specs/004-dados-3d/`.
  Aclarado con el usuario: d100 como dos d10; como mucho 20 dados en pantalla; los dados 3D
  empiezan desactivados. Plan (2026-11-18): three.js 0.187.0 + cannon-es 0.20.0 copiadas en
  `wwwroot/lib/`, módulo propio `dice3d.js` que fuerza la cara del dominio, sin `IDiceRenderer`
  (constitución III). El usuario aprobó las dos dependencias (opción A, frente a hacer la física en
  C# o renunciar al 3D) y añadió la **bandeja de dados** (US4: cada clic suma un dado a la fórmula).
- **Implementación de F003 (2026-11-18)**: fases 1 a 5 y la bandeja hechas; 1 000 tiradas simuladas
  con la cara correcta y el dado plano; 17 pruebas de navegador; descarga inicial sin 3D +0,35 %.
  **Revisión independiente (T019)**: corregido que el historial mostraba el total antes de pararse
  los dados; que apagar el 3D mientras cargaba lo dejaba muerto hasta recargar; dados que acababan
  inclinados (8,5 %; ahora se asientan); la simulación cuenta para los 4 s; contexto WebGL liberado;
  «1 2» en la bandeja; versiones separadas de los formatos guardados (conjuntos 1, historial 2) para
  que una versión anterior abierta en otra pestaña no rechace los conjuntos; pruebas reforzadas.
  **Sin corregir** (menores): girar el móvil durante una tirada puede dejar algún dado fuera de
  cámara; la simulación ocupa el hilo principal (unos 0,6 s con 20 d20 en el ordenador).
  **Pendiente**: fotogramas por segundo en el móvil del usuario (T018), con la web publicada.
- Historia de F002: rama `003-creador-conjuntos-dados`,
  `specs/003-creador-conjuntos-dados/spec.md` escrita el 2026-11-18. Resuelto con el usuario: los
  dados que penetran son una opción que el jugador activa (regla del MJ, «Daño de penetración»), y
  el dado de maestría de un conjunto es la media redondeada a la baja a un dado real; un conjunto
  tiene entre 2 y 5 habilidades (según recuerda el usuario del HJ 4.05). **Tareas del usuario**:
  añadir a la pestaña "Dudas" de la hoja dos dudas: el dado de Zoólogo, Escribano y Hechicero,
  que no siguen esa regla, y Forjador de armaduras y Trovador, que tienen 6 habilidades (T028). Aclarado después (`/speckit-clarify`): los conjuntos del jugador van en la misma
  lista que los 36, sin distinguirse, y sus clases permitidas se calculan solas.
- **Plan de F002 (2026-11-18)**, detalle en `research.md`: analizador de fórmulas propio;
  `IRandom` con `Random.Shared`; `localStorage` por `IJSRuntime` sin paquetes (IndexedDB sigue en
  F004); los valores de los conjuntos del jugador se calculan al mostrarlos; una prueba recalcula
  los 36 conjuntos de `data/`. Siguiente paso: `/speckit-tasks`.
- **Calculador de conjuntos (2026-11-18)**: recalcula los 36 conjuntos de la hoja; coinciden salvo
  excepciones con nombre (`SuiteRulesMatchSheetTests`). Cerrajero coincide con la regla aunque su
  dado esté escrito a mano. **Tareas del usuario en la hoja**: (1) en la tabla de Actor, borrar
  «C86=» del principio de la fórmula de PC de «Imitar voces» (fila 85, columna C), que hoy vale 0;
  (2) Mantenimiento: sus 3 habilidades tienen dado «*» y la regla no da dado; dejarlo como en el
  manual o borrarlo. Al corregirlas, quitar sus excepciones de la prueba (falla si sobran).
- **Implementación de F002 (2026-11-18)**: fases 1 a 5 hechas y probadas en el navegador a
  390 px (tirador con penetración, historial, creador; Alquimista rehecho da 16 PC, 1403 po, 15 %
  y d8). 10 pruebas de navegador en verde. SC-004 medido: unos 3 s tirar y 4 s crear y guardar un
  conjunto, sin contar lo que tarda una persona.
- **Revisión independiente de F002 (T030, 2026-11-18)**: corregido:
  - la página de dados podía guardar antes de cargar el historial;
  - dos pestañas se borraban lo guardado: ahora se relee antes de escribir;
  - un conjunto oficial (sin clases en los datos) daba «Todas»: ahora queda pendiente;
  - no se avisaba si los conjuntos guardados no se podían leer;
  - reutilizar el nombre de un conjunto borrado podía cerrar un ciclo;
  - espacios y mensajes del analizador; `?f=` no válido sin error; azar real sin prueba; datos
    guardados con nulos.
  **Sin corregir** (menores): si una reimportación añade a la hoja un conjunto con el mismo slug
  que uno del jugador, los dos comparten dirección sin aviso.
- **Nota**: en esta sesión la app no encontraba los comandos `/speckit-…` como skills; se
  siguieron sus instrucciones leyendo `.claude/skills/*/SKILL.md`. El HJ 4.05 que tiene el usuario está escaneado: las reglas se toman de las fórmulas
  de la hoja (PC = suma − 1; precio = suma × 1,1 redondeado; dificultad = mayor + 5).
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
