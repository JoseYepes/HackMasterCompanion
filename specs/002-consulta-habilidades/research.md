# Research: Datos y consulta de habilidades (F001)

Las versiones citadas se consultaron en NuGet el 2026-11-16. Se fijan en
`Directory.Packages.props` durante la implementación, volviendo a comprobar que siguen siendo las
últimas estables.

## 1. Lectura de la hoja de cálculo

- **Decision**: ClosedXML (0.105.1, licencia MIT), solo en `tools/DataImport` y en su proyecto de
  pruebas.
- **Rationale**: da acceso por pestaña, fila y columna con el número de fila real, que es lo que
  necesitan los mensajes de error (FR-005). Lee el valor calculado de las celdas con fórmula.
  También escribe libros, lo que permite construir en memoria las hojas de prueba: los `.xlsx` no
  se pueden versionar (`.gitignore`), así que las pruebas no pueden depender de archivos de ejemplo.
- **Alternatives considered**: ExcelDataReader 3.9.0 (más ligero, pero solo lee: las pruebas
  necesitarían otra librería o archivos binarios); DocumentFormat.OpenXml 3.5.1 (oficial de
  Microsoft, de muy bajo nivel: cadenas compartidas y referencias a mano).
- **Pendiente de verificar en la implementación**: que ClosedXML abre la hoja real sin avisos
  (tiene 4 pestañas e hipervínculos) y que la lectura de celdas con fórmula devuelve el valor
  guardado.

## 2. Dónde vive cada parte de la importación

- **Decision**: `tools/DataImport` se divide en tres pasos con una frontera clara:
  1. *Lectura* (`WorkbookReader`): ClosedXML → filas en bruto (`RawRow`: pestaña, número de fila,
     celdas como texto). Nunca lee la columna de texto de manual.
  2. *Interpretación y validación* (`Importer`): filas en bruto → modelo de `Domain` más una lista
     de errores y otra de avisos. Código puro, sin ClosedXML ni disco.
  3. *Escritura* (`DataWriter`): modelo → archivos JSON y `docs/dudas.md`.
- **Rationale**: el paso 2 concentra todas las reglas (normalización, requisitos, duplicados) y se
  prueba con filas en memoria, sin Excel. Los pasos 1 y 3 quedan triviales.
- **Alternatives considered**: poner la interpretación en `Domain` (mezcla en el dominio
  conocimiento de cómo está escrita una hoja concreta); un único `Program.cs` (no se puede probar).

## 3. Proyecto de pruebas del importador

- **Decision**: nuevo proyecto `tests/HackMaster.DataImport.Tests` (xUnit), con referencia a
  `tools/DataImport`.
- **Rationale**: el importador tiene la mayor parte de las reglas de F001 y la constitución exige
  test primero. `Domain.Tests` no debe depender de ClosedXML ni de la herramienta.
- **Alternatives considered**: probarlo desde `Domain.Tests` (arrastra ClosedXML al proyecto de
  pruebas del dominio); no probarlo y fiarse de la hoja real (incumple el principio IV y SC-007).
- **Nota**: es un proyecto de pruebas, no de producción; los proyectos de producción siguen siendo
  `Domain` y `Web` más la herramienta (principio III). Se anota en Complexity Tracking.

## 4. Formato y ubicación de los datos generados

- **Decision**: cuatro archivos JSON en `data/`, versionados: `skills.json`, `suites.json`
  (conjuntos de habilidades y oficiales, con un campo `kind`), `styles.json` y `doubts.json`. Cada
  uno lleva `schemaVersion`. Sin fecha ni hora de generación. Orden estable (por nombre
  normalizado), sangría de 2 espacios, UTF-8 sin BOM, final de línea LF.
- **Rationale**: archivos separados dan diffs legibles en los pull requests (FR-011). Sin marcas de
  tiempo y con orden fijo, importar dos veces la misma hoja da archivos idénticos (FR-010).
- **Alternatives considered**: un único `data.json` (diffs más ruidosos); un archivo por habilidad
  (343 archivos, sin beneficio).
- **Final de línea**: `.gitattributes` añade `data/*.json text eol=lf` y `docs/dudas.md text eol=lf`
  para que Windows y CI vean los mismos bytes.

## 5. Cómo llegan los datos a la aplicación

- **Decision**: los JSON de `data/` se incluyen en `HackMaster.Web` como recursos incrustados
  (`EmbeddedResource`) y se leen al arrancar con `System.Text.Json` en un servicio `GameData`
  registrado como instancia única.
- **Rationale**: no hay petición de red ni dependencia de la ruta base de GitHub Pages; los datos
  viajan con la aplicación, de modo que cada publicación es coherente consigo misma; bUnit puede
  usar los datos reales sin simular HTTP; y deja resuelto el uso sin conexión cuando llegue la PWA.
- **Alternatives considered**: servirlos como archivos estáticos y pedirlos con `HttpClient`
  (obliga a copiar o enlazar `data/` dentro de `wwwroot`, que los recursos estáticos de Blazor no
  recogen bien desde fuera del proyecto, y añade estados de carga y de error); generar código C#
  (diffs ilegibles).
- **Coste**: el tamaño de los JSON se suma a la descarga inicial. Estimación: menos de 500 KB antes
  de comprimir. Se mide en la implementación; si supera 1 MB se revisa esta decisión.

## 6. Modelo y lógica de consulta

- **Decision**: los tipos del modelo (`Skill`, `SkillSuite`, `CombatStyle`, `Doubt`…) y la lógica de
  búsqueda, filtros y orden (`SkillQuery`, `TextNormalizer`) viven en `HackMaster.Domain`, sin
  paquetes externos. El importador serializa esos mismos tipos y `Web` los deserializa.
- **Rationale**: un solo contrato de datos para quien escribe y quien lee; la búsqueda y los
  filtros son reglas puras y se prueban con xUnit sin navegador (principios III y IV).
- **Alternatives considered**: tipos duplicados en importador y aplicación (se desincronizan);
  filtros dentro de los componentes (solo se podrían probar con bUnit).
- **Nota**: `System.Text.Json` forma parte de .NET, no es un paquete; `Domain` solo declara los
  tipos y no lee archivos.

## 7. Búsqueda sin acentos ni mayúsculas

- **Decision**: `TextNormalizer.Normalize`: descompone (`NormalizationForm.FormD`), elimina las
  marcas diacríticas, pasa a minúsculas invariantes y compacta espacios. El importador guarda la
  clave ya normalizada del nombre y del resumen (`searchName`, `searchSummary`); en el navegador
  solo se normaliza lo que escribe el jugador.
- **Rationale**: una única función para importador y aplicación (FR-007); con las claves
  precalculadas, cada pulsación compara cadenas sin más trabajo (SC-003).
- **Consecuencia**: "ñ" se trata como "n" ("montanero" encuentra "Montañero"). Se acepta: en el
  móvil es frecuente escribir sin "ñ".
- **Pendiente de verificar en la implementación**: que la normalización se comporta igual dentro
  del navegador (WebAssembly) que en .NET de escritorio. Lo comprueba la prueba Playwright de la
  búsqueda "botanica".

## 8. Requisitos compuestos

- **Decision**: un requisito se guarda como una lista ordenada de segmentos que, concatenados,
  reproducen el texto de la hoja. Cada segmento es texto, una referencia a una habilidad o conjunto
  (con su `slug`), o una condición reconocida. El análisis separa por ",", " o ", " y ", ":" y
  resuelve cada parte, en este orden: habilidad o conjunto existente → condición reconocida →
  anotación "(sin verificar)" o "?" → error.
- **Condiciones reconocidas** (lista cerrada en el importador, con una prueba por patrón):
  característica mínima ("Sabiduría 14+"), competencia con arma ("Competencia …"), nivel de
  maestría ("… al 101%+", "N más al 51%+"), "Al menos N de", y "NA".
- **Rationale**: muestra el requisito tal como lo escribe la fuente, con enlaces donde hay una
  habilidad, sin interpretar la lógica "y/o" (que F001 no necesita y sería inventar semántica).
  F005 podrá añadir esa lógica sobre los mismos segmentos.
- **Alternatives considered**: un árbol lógico Y/O (más de lo que F001 necesita); guardar solo el
  texto (sin enlaces ni validación, incumple FR-005 y FR-020).

## 9. Valores dudosos ("pendiente de verificar")

- **Decision**: los campos numéricos son anulables. Cada elemento lleva `pending` (nombres de los
  campos pendientes de verificar) y `raw` (el texto original de la hoja para esos campos). El
  importador emite un aviso por cada uno y lo añade a las dudas como duda detectada.
- **Rationale**: no se inventa ni se corrige nada (principio I) y la aplicación puede mostrar la
  marca junto al dato concreto (FR-027).
- **Casos conocidos**: dado de maestría "5", "3", "2"; coste "1 R"; valores "NA" y "?"; conjunto
  "Jinete" sin valores; componentes de los conjuntos oficiales.
- **Alternatives considered**: rechazar la importación (bloquearía por datos que la fuente no
  tiene); guardar todo como texto (impide filtrar y ordenar).

## 10. Direcciones y estado de la consulta

- **Decision**: rutas en castellano: `/habilidades`, `/habilidades/{slug}`, `/conjuntos`,
  `/conjuntos/{slug}`, `/estilos`, `/estilos/{slug}`, `/dudas`. La página inicial enlaza a las
  cuatro secciones. La búsqueda, los filtros y el orden de `/habilidades` se guardan en la cadena
  de consulta de la dirección (`?q=…&tipo=…&libro=…`).
- **Rationale**: con el estado en la dirección, volver atrás desde una ficha recupera la lista tal
  como estaba (escenario 6 de la historia 2) sin guardar nada. El `slug` lo genera el importador a
  partir del nombre normalizado, es único y estable mientras no cambie el nombre.
- **Alternatives considered**: estado en memoria (se pierde al recargar); almacenamiento local
  (F001 no persiste nada del usuario).
- **Nota**: las direcciones internas ya funcionan en GitHub Pages gracias al `404.html` de F000.

## 11. Presentación

- **Decision**: CSS propio, sin librería de estilos, diseñado primero para 390 px. La lista de
  habilidades es una lista de tarjetas de una columna en móvil y una tabla a partir de 820 px. Los
  filtros se pliegan en móvil. Se dibujan los 343 elementos; la escritura en el buscador se retrasa
  unos 150 ms.
- **Rationale**: cierra la decisión que `docs/ESTADO.md` dejaba para F001; 343 elementos sencillos
  no justifican virtualización ni una librería.
- **Alternatives considered**: Bootstrap u otra librería (peso y estilos que deshacer);
  `<Virtualize>` (se adopta solo si SC-003 no se cumple).

## 12. Pruebas de interfaz con Playwright

- **Decision**: Microsoft.Playwright 1.64.1 dentro de `tests/HackMaster.Web.Tests`, en la carpeta
  `E2E/`. Un atributo propio (`E2EFactAttribute`) marca la prueba como omitida si no está definida
  la variable de entorno `HACKMASTER_E2E_URL`. CI añade un trabajo que publica la aplicación, la
  sirve en local, instala Chromium y ejecuta esas pruebas a 390 px y 820 px.
- **Rationale**: Playwright entra en F001 como estaba previsto, sin un proyecto más. Con el
  atributo, `dotnet test` sigue pasando en un equipo sin navegadores instalados, así que los
  comandos de F000 no cambian.
- **Alternatives considered**: proyecto `E2E.Tests` aparte (otro proyecto); fallar si no hay
  navegador (rompe `dotnet test` en una copia limpia).
- **Pendiente de verificar en la implementación**: el paso exacto de instalación de navegadores en
  `ubuntu-latest` y que el trabajo completo de CI sigue por debajo de 10 minutos; las acciones
  nuevas se fijan por SHA.

## 13. Integridad de los datos versionados

- **Decision**: `Domain.Tests` incluye pruebas que leen los JSON de `data/` y comprueban los
  recuentos (343, 36, 23, 5), que toda referencia tiene destino, que los `slug` son únicos y que
  ningún archivo contiene el campo de texto de manual.
- **Rationale**: CI no tiene la hoja de cálculo; estas pruebas garantizan que lo que se publica es
  coherente aunque alguien edite `data/` a mano (SC-001, SC-009, SC-010).

## 14. Limitaciones del entorno

- La hoja real solo está en el equipo del usuario (variable `HACKMASTER_EXCEL`). La importación
  real y la corrección de los nombres que no resuelven las hace el usuario, con ayuda del agente,
  en una sesión local.
- Hasta que el usuario corrija la hoja, `data/` no puede generarse completo. Las tareas de
  interfaz se desarrollan contra datos de prueba pequeños y se validan contra los reales al final.
