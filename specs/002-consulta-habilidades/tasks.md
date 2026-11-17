---

description: "Task list for F001 — Datos y consulta de habilidades"
---

# Tasks: Datos y consulta de habilidades (F001)

**Input**: Design documents from `/specs/002-consulta-habilidades/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluidas; la constitución (principio IV) exige test primero. Cada prueba se escribe y
se ve fallar antes de implementar. Nombre de prueba = regla + fuente
(`Search_Ignores_Accents_And_Case__FR013`).

**Organization**: por historia de usuario. Cada fase se ejecuta como un bucle con objetivo,
verificación, restricciones y límite de 8 iteraciones (formato de `CLAUDE.md`).

**Verificación común** de todas las fases: `dotnet build -warnaserror`, `dotnet test --no-build`,
`dotnet format --verify-no-changes`.

**Restricciones comunes**: no borrar ni debilitar pruebas existentes; no leer ni copiar la columna
"Texto del manual"; no editar `data/` ni `docs/dudas.md` a mano; no añadir paquetes fuera de los
aprobados en `plan.md`; interfaz y mensajes en castellano, identificadores en inglés.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: se puede hacer en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: historia de usuario de `spec.md` (US1…US5)

---

## Phase 1: Setup

**Objetivo**: paquetes, proyecto de pruebas del importador y archivos de datos vacíos; la solución
sigue construyendo.
**Verificación**: verificación común; `dotnet sln list` muestra seis proyectos.
**Restricciones**: versiones fijadas en `Directory.Packages.props`, comprobadas en NuGet.

- [X] T001 Añadir a `Directory.Packages.props` las versiones de `ClosedXML` y `Microsoft.Playwright` (últimas estables en NuGet; referencia de `research.md`: 0.105.1 y 1.64.1)
- [X] T002 Añadir la referencia de paquete `ClosedXML` y la referencia de proyecto a `src/HackMaster.Domain` en `tools/DataImport/DataImport.csproj`
- [X] T003 Crear `tests/HackMaster.DataImport.Tests/HackMaster.DataImport.Tests.csproj` (xUnit, mismos paquetes de prueba que `Domain.Tests`, referencia a `tools/DataImport/DataImport.csproj`) y añadirlo a `HackMaster.slnx` en la carpeta `/tests/`
- [X] T004 [P] Crear `data/skills.json`, `data/suites.json`, `data/styles.json` y `data/doubts.json` con `schemaVersion: 1` y la lista vacía, según `data-model.md` ("Archivos generados")
- [X] T005 [P] Añadir a `.gitattributes` las líneas `data/*.json text eol=lf` y `docs/dudas.md text eol=lf`
- [X] T006 Incluir `..\..\data\*.json` como `EmbeddedResource` en `src/HackMaster.Web/HackMaster.Web.csproj`

---

## Phase 2: Foundational (modelo y normalización de texto)

**Objetivo**: tipos del modelo y normalización de texto en `Domain`, compartidos por importador y
aplicación.
**Verificación**: verificación común; las pruebas de `TextNormalizerTests` pasan.
**Restricciones**: `src/HackMaster.Domain` sin referencias de paquete; tipos inmutables; sin
interfaces.

**⚠️ CRITICAL**: ninguna historia puede empezar antes de terminar esta fase.

- [X] T007 [P] Escribir en `tests/HackMaster.Domain.Tests/TextNormalizerTests.cs` las pruebas de `Normalize` (quita tildes y diéresis, minúsculas, "ñ"→"n", compacta espacios, recorta extremos; "BOTÁNICA", "botanica" y " Botánica " dan lo mismo) y de `ToSlug` (espacios y signos → "-", sin guiones repetidos ni en los extremos; "Leer/Escribir" → "leer-escribir") y comprobar que fallan
- [X] T008 Implementar `TextNormalizer.Normalize` y `TextNormalizer.ToSlug` en `src/HackMaster.Domain/TextNormalizer.cs` (`NormalizationForm.FormD`, sin marcas diacríticas, minúsculas invariantes)
- [X] T009 [P] Crear `src/HackMaster.Domain/Ability.cs` (enumeración `FUE`, `DES`, `CON`, `INT`, `SAB`, `CAR`) y `src/HackMaster.Domain/BookRef.cs` (`Acronym`, `Page` anulable)
- [X] T010 [P] Crear `src/HackMaster.Domain/Prerequisite.cs`: `Prerequisite` (`Segments`), `PrerequisiteSegment` (`Text`, `Kind`, `Target` anulable) y `PrerequisiteSegmentKind` (`Text`, `Skill`, `Suite`, `OfficialSuite`, `Condition`, `Unverified`)
- [X] T011 [P] Crear `src/HackMaster.Domain/Skill.cs` con los campos de `data-model.md` ("Skill"): `Cost`, `Price` y `Difficulty` enteros anulables; `MasteryDie` y `Summary` texto anulable; `Pending` (lista de nombres de campo) y `Raw` (diccionario campo → texto original)
- [X] T012 [P] Crear `src/HackMaster.Domain/SkillSuite.cs`: `SkillSuite`, `SuiteKind` (`Custom`, `Official`) y `SuiteComponent` (`Name`, `Kind`: `Skill`, `Suite` u `OfficialSuite`, `Target`), según `data-model.md`; el nombre es único dentro de cada `SuiteKind`, no entre los dos
- [X] T013 [P] Crear `src/HackMaster.Domain/CombatStyle.cs` (sin `Cost`) y `src/HackMaster.Domain/Doubt.cs` (`Id`, `Origin`: `Sheet` o `Import`, `Subject`, `Target` anulable con `Kind` y `Slug`, `Source`, `Question`, `Provisional`), según `data-model.md`
- [X] T014 Crear `src/HackMaster.Domain/GameDataFiles.cs` con los contenedores de archivo (`SkillsFile`, `SuitesFile`, `StylesFile`, `DoubtsFile`, cada uno con `SchemaVersion`), la constante `CurrentSchemaVersion = 1` y las opciones compartidas de `System.Text.Json` (camelCase, enumeraciones como texto, sangría de 2 espacios, sin escapar caracteres no ASCII)

**Checkpoint**: modelo y normalización listos.

---

## Phase 3: User Story 5 - Regenerar los datos desde la hoja de cálculo (Priority: P1) 🎯 MVP

**Goal**: un comando convierte la hoja en JSON validado y `docs/dudas.md`, o falla listando todos
los errores sin tocar ningún archivo.
**Independent Test**: paso 2 de `quickstart.md` (importación real y copia con un error a propósito).
**Verificación**: verificación común; las pruebas de `HackMaster.DataImport.Tests` pasan sin
necesitar la hoja real.
**Restricciones**: `Importer` y `PrerequisiteParser` sin ClosedXML ni acceso a disco; el lector no
accede a la columna D de "Descripciones"; mensajes con el formato de
`contracts/data-import-cli.md`.

### Tests (primero; deben fallar antes de implementar)

- [X] T015 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/PrerequisiteParserTests.cs`: "N"/"n"/vacío → sin segmentos; una habilidad → segmento `Skill` con `Target`; "A, B", "A o B" y "A, B o C" → habilidades enlazadas con separadores `Text`; nombre que coincide sin tildes ni mayúsculas; referencia a conjunto → `Suite`; referencia a conjunto oficial → `OfficialSuite`; nombre que existe como conjunto y como oficial ("Cartógrafo") → `Suite`; una prueba por condición reconocida ("Sabiduría 14+", "Competencia básica en el arma", "otra maniobra especial al 101%+", "2 más al 51%+", "Al menos dos de: …", "NA"); "X (sin verificar)" y "?" → `Unverified`; nombre inexistente → error; los segmentos concatenados reproducen el texto original
- [X] T016 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/ImporterSkillsTests.cs` con filas en memoria: normalización de dado ("8" y "d8" → `d8`; "NA", "na", "*"), dado "5" → nulo + `pending` + `raw` + aviso; precio "250 po" → 250; dificultad "0.1" → 10 y "-5" → −5; coste "1 R" y "NA" → nulo; características "INT, SAB" e "INT/SAB"; "ASP" → `raw` + `pending`; libro "MJ 329 / GMG 71" → dos `BookRef`; clases separadas por coma; resumen tomado de "Descripciones" y vacío → `pending`
- [X] T017 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/ImporterValidationTests.cs`: nombre duplicado dentro de un mismo tipo, sin distinguir tildes ni mayúsculas → error con ambas filas; dos nombres distintos del mismo tipo con igual `slug` → error; el mismo nombre en un conjunto de habilidades y en uno oficial NO es error; requisito inexistente → error con pestaña, fila y nombre; componente de conjunto inexistente → error; habilidad de "Descripciones" que no está en "Habilidades" → error; duda de la hoja que nombra un elemento inexistente → error; varios errores se devuelven todos a la vez; con errores no hay modelo de salida
- [X] T018 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/ImporterSuitesTests.cs`: bloque de conjunto (fila de título, fila de cabecera, componentes, filas vacías) → `SkillSuite` `Custom` con componentes resueltos; componente que es otro conjunto → `SuiteComponent` `Suite`; bloque "Conjuntos de habilidades oficiales" → `Official` sin componentes y `components` en `pending`; "?" en requisito, precio o dificultad → nulo + `pending`; conjunto sin valores ("Jinete") → cargado con `pending`; bloque "Estilos de combate" → `CombatStyle` con requisitos analizados; cada fila de la pestaña "Dudas" → una `Doubt` con un único `Target` (habilidad, conjunto, oficial o estilo); "General" en la columna A → duda general con `Target` nulo; cada aviso genera una `Doubt` de origen `Import`
- [X] T019 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/WorkbookReaderTests.cs` creando libros en memoria con ClosedXML: lee las cuatro pestañas con su número de fila real; celda con fórmula → valor calculado; falta una pestaña o una columna esperada → error de estructura; un texto centinela en la columna D de "Descripciones" no aparece en ninguna `RawRow`
- [X] T020 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/DataWriterTests.cs`: escribe los cuatro JSON y el archivo de dudas; dos escrituras del mismo modelo dan archivos idénticos byte a byte; orden por `searchName`; UTF-8 sin BOM y finales LF; `docs/dudas.md` lleva el aviso de archivo generado y separa dudas de la hoja y detectadas; si la escritura falla a medias, el destino no cambia
- [X] T021 [P] [US5] Escribir `tests/HackMaster.DataImport.Tests/CliTests.cs`: sin argumentos o con hoja inexistente → código 2 y mensaje de uso; errores de datos → código 1, todos los errores en la salida de error y ningún archivo modificado; correcto → código 0 y resumen con recuentos y avisos

### Implementation

- [X] T022 [US5] Crear `tools/DataImport/RawRow.cs` (`Sheet`, `RowNumber`, celdas por letra de columna) y `tools/DataImport/ImportMessage.cs` (pestaña, fila, elemento, mensaje, con el formato `[<pestaña>, fila <n>] «<elemento>»: <mensaje>`)
- [X] T023 [US5] Implementar `tools/DataImport/PrerequisiteParser.cs` según `research.md` §8 (orden de resolución: habilidad o conjunto → condición reconocida → anotación de duda → error)
- [X] T024 [US5] Implementar en `tools/DataImport/Importer.cs` la interpretación de "Habilidades" y "Descripciones" (reglas de `data-model.md`, "Skill") hasta pasar T016
- [X] T025 [US5] Implementar en `tools/DataImport/Importer.cs` los bloques de la pestaña "Conjuntos de habilidades" (36 conjuntos, oficiales, estilos) y la pestaña "Dudas" hasta pasar T018, con unicidad de nombre por tipo de conjunto
- [X] T026 [US5] Implementar en `tools/DataImport/Importer.cs` las validaciones de FR-005 y FR-006 (incluidos los `slug` repetidos dentro de un tipo) acumulando todos los errores, hasta pasar T017
- [X] T027 [P] [US5] Implementar `tools/DataImport/WorkbookReader.cs` con ClosedXML; la columna D de "Descripciones" no se lee
- [X] T028 [P] [US5] Implementar `tools/DataImport/DataWriter.cs`: JSON con las opciones de `GameDataFiles`, `docs/dudas.md`, escritura en carpeta temporal y movimiento final
- [X] T029 [US5] Reescribir `tools/DataImport/Program.cs` según `contracts/data-import-cli.md` (argumentos, códigos 0/1/2, resumen y errores en castellano)
- [ ] T030 [US5] Actualizar `README.md` y `CLAUDE.md` con el comando de importación real (`CLAUDE.md` requiere confirmación del usuario)

**Checkpoint**: el importador funciona y está probado sin la hoja real.

---

## Phase 4: Primera importación real (con el usuario)

**Objetivo**: `data/` y `docs/dudas.md` generados desde la hoja real y versionados.
**Verificación**: la importación termina con código 0 y los recuentos 343 / 36 / 23 / 5;
`DataIntegrityTests` pasa; repetir la importación no cambia `git status`.
**Restricciones**: la hoja la corrige el usuario, no el agente; no relajar validaciones para que
pase; cada patrón nuevo de la hoja se añade al importador con su prueba.

- [X] T031 [US5] Ejecutar `dotnet run --project tools/DataImport -- "$env:HACKMASTER_EXCEL" data/` y entregar al usuario la lista de errores con la corrección propuesta para cada fila (sesión local con el usuario)
- [ ] T032 [US5] El usuario corrige en la hoja los nombres que no resuelven y añade a la pestaña "Dudas" las dudas de `docs/PRODUCTO.md` A5 que faltan (Pellizcar; estilos de artes marciales; Maniobra Ka-Bob y Desenfunde veloz), añade como duda "General" la de los estilos de artes marciales, y divide la duda "granjero / mercader / noble" en tres filas con el nombre exacto de cada conjunto oficial; el agente le prepara el texto
- [ ] T033 [US5] Repetir la importación hasta obtener código 0; revisar con el usuario la lista de avisos; versionar `data/*.json` y `docs/dudas.md`
- [ ] T034 [US5] Escribir `tests/HackMaster.Domain.Tests/DataIntegrityTests.cs` leyendo `data/` (enlazado como contenido del proyecto de pruebas): 343 habilidades, 36 conjuntos `Custom`, 23 `Official`, 5 estilos; `slug` únicos por tipo; todo `Target` existe; ningún archivo contiene la propiedad `manualText` (SC-001, SC-009, SC-010)
- [ ] T035 [US5] Medir el tamaño de `data/*.json` y anotarlo en `research.md` §5 (revisar la decisión si supera 1 MB); medir la duración de la importación completa y anotarla (SC-008: menos de 1 minuto); validar el paso 3 de `quickstart.md` con el usuario

**Checkpoint**: datos reales en el repositorio. Las fases 5 a 8 pueden empezar antes de este punto
usando los datos de prueba de T038.

---

## Phase 5: User Story 1 - Buscar y filtrar habilidades (Priority: P1)

**Goal**: lista de habilidades con búsqueda en nombre y resumen, filtros combinables, orden y
contador, utilizable a 390 px.
**Independent Test**: filas 1 a 5 de la tabla del paso 4 de `quickstart.md`.
**Verificación**: verificación común; `SkillQueryTests` y `SkillsPageTests` pasan.
**Restricciones**: la lógica de búsqueda y filtros vive en `Domain`, no en los componentes; sin
librería de estilos; el estado de la consulta vive en la dirección (`contracts/rutas.md`).

### Tests (primero; deben fallar antes de implementar)

- [X] T036 [P] [US1] Escribir `tests/HackMaster.Domain.Tests/SkillQueryTests.cs`: texto en `searchName` o `searchSummary` sin distinguir tildes ni mayúsculas; coincidencias por nombre antes que por resumen; `Snippet` solo cuando coincide por resumen; varios valores en un filtro = basta uno; filtros distintos = todos; `classes` incluye "Todas"; `books` compara el acrónimo; rangos de precio y dificultad excluyen los valores nulos y admiten negativos; orden por nombre, PC, precio y dificultad con nulos al final; criterio vacío devuelve todo
- [X] T037 [P] [US1] Escribir `tests/HackMaster.Domain.Tests/SkillFacetsTests.cs`: las opciones de tipo, libro (acrónimos) y clase salen de los datos, sin duplicados y ordenadas (FR-017)
- [X] T038 [P] [US1] Crear `tests/HackMaster.Web.Tests/TestData.cs` con un `GameData` pequeño construido en código (unas 8 habilidades, 2 conjuntos, 1 oficial, 1 estilo, 2 dudas, con casos de requisito, `pending` y duda)
- [X] T039 [P] [US1] Escribir `tests/HackMaster.Web.Tests/GameDataTests.cs`: carga los recursos incrustados; índices por `slug`; habilidad → conjuntos que la contienen; elemento → dudas; `schemaVersion` distinto de 1 → error claro
- [X] T040 [P] [US1] Escribir `tests/HackMaster.Web.Tests/SkillsPageTests.cs` (bUnit, con `TestData`): muestra todas las habilidades y el contador (`result-count`); escribir en `search-input` filtra; marcar dos libros y un tipo combina; los filtros activos se ven sin desplegar el panel (FR-014); mensaje y botón `clear-filters` cuando no hay resultados; los parámetros de la dirección (`q`, `tipo`, `libro`, `clase`, `precioMax`, `difMin`, `orden`) restauran el estado; los parámetros no válidos se ignoran

### Implementation

- [X] T041 [US1] Implementar `SkillCriteria`, `SkillMatch`, `SkillSort` y `SkillQuery.Apply` en `src/HackMaster.Domain/SkillQuery.cs` según `data-model.md` ("SkillQuery")
- [X] T042 [P] [US1] Implementar `SkillFacets.From` en `src/HackMaster.Domain/SkillFacets.cs`
- [X] T043 [US1] Implementar `src/HackMaster.Web/GameData.cs` (lectura de los recursos incrustados, índices y relaciones inversas; constructor a partir de listas para las pruebas) y registrarlo como instancia única en `src/HackMaster.Web/Program.cs`
- [X] T044 [US1] Crear `src/HackMaster.Web/Components/SkillFilters.razor`: casillas de tipo, libro y clase, rangos de precio y dificultad, selector de orden y botón "Quitar filtros", con los `data-testid` de `contracts/rutas.md`
- [X] T045 [US1] Crear `src/HackMaster.Web/Pages/Skills.razor` en `/habilidades`: buscador con retardo de unos 150 ms, contador, lista de resultados (`skill-row`) con nombre, tipo, PC y libro, fragmento del resumen cuando procede, y estado sincronizado con la dirección
- [X] T046 [US1] Añadir la navegación principal (Habilidades, Conjuntos, Estilos, Dudas) en `src/HackMaster.Web/Layout/MainLayout.razor` y los enlaces a las secciones en `src/HackMaster.Web/Pages/Home.razor`, sin romper `Home_Shows_App_Name__F000`
- [X] T047 [US1] Escribir en `src/HackMaster.Web/wwwroot/css/app.css` los estilos de navegación, buscador, filtros plegables y lista: una columna de tarjetas a 390 px y tabla desde 820 px, sin desplazamiento horizontal (nombres largos con salto de línea)

**Checkpoint**: la consulta de habilidades funciona de forma independiente.

---

## Phase 6: User Story 2 - Ver la ficha de una habilidad (Priority: P2)

**Goal**: ficha con todos los datos, resumen, requisitos enlazados, conjuntos y dudas.
**Independent Test**: filas "Herbalismo", "Volver atrás", "Cerrajería" y "Recargar una ficha" del
paso 4 de `quickstart.md`.
**Verificación**: verificación común; `SkillDetailTests` pasa.
**Restricciones**: ningún texto inventado; lo ausente se muestra con `PendingMark`; precios sin
separador de miles; libros como acrónimo y página.

### Tests (primero; deben fallar antes de implementar)

- [ ] T048 [P] [US2] Escribir `tests/HackMaster.Web.Tests/PrerequisiteViewTests.cs`: los segmentos `Skill`, `Suite` y `OfficialSuite` son enlaces a `/habilidades/{slug}`, `/conjuntos/{slug}` y `/conjuntos/oficiales/{slug}`; `Text` y `Condition` son texto; `Unverified` lleva `pending-mark`; sin segmentos muestra "Ninguno"
- [ ] T049 [P] [US2] Escribir `tests/HackMaster.Web.Tests/SkillDetailTests.cs`: muestra todos los campos de FR-003; precio 1403 se muestra "1403"; dificultad con "%"; libro "MJ 329"; resumen ausente → "pendiente de verificar"; campo en `pending` → `pending-mark` junto a su valor original de `raw`; lista los conjuntos que la contienen con enlace; `doubt-notice` con duda, fuente y decisión provisional; `slug` inexistente → página no encontrada

### Implementation

- [ ] T050 [P] [US2] Crear `src/HackMaster.Web/Components/PendingMark.razor` (ya creado en la fase 5 para la lista; texto "pendiente de verificar", `data-testid="pending-mark"`) y `src/HackMaster.Web/Components/DoubtNotice.razor` (`data-testid="doubt-notice"`)
- [ ] T051 [P] [US2] Crear `src/HackMaster.Web/Components/PrerequisiteView.razor`
- [ ] T052 [US2] Crear `src/HackMaster.Web/Pages/SkillDetail.razor` en `/habilidades/{slug}` y enlazar cada fila de `src/HackMaster.Web/Pages/Skills.razor` con su ficha
- [ ] T053 [US2] Añadir a `src/HackMaster.Web/wwwroot/css/app.css` los estilos de ficha, marca de pendiente y aviso de duda a 390 px y 820 px

**Checkpoint**: historias 1 y 2 funcionan juntas; volver atrás conserva la consulta.

---

## Phase 7: User Story 3 - Consultar los conjuntos y los estilos (Priority: P3)

**Goal**: listas y fichas de los 36 conjuntos, los 23 oficiales y los 5 estilos de combate.
**Independent Test**: filas "Alquimista", "conjunto oficial" y "Estilos" del paso 4 de
`quickstart.md`.
**Verificación**: verificación común; `SuitesPageTests` y `StylesPageTests` pasan.
**Restricciones**: los valores se muestran tal como están en los datos; no se recalculan (F002).

### Tests (primero; deben fallar antes de implementar)

- [ ] T054 [P] [US3] Escribir `tests/HackMaster.Web.Tests/SuitesPageTests.cs`: la lista separa conjuntos de habilidades y oficiales, con nombre, PC, precio, dificultad y libro; la búsqueda por nombre ignora tildes y mayúsculas; la ficha lista los componentes con enlace a habilidad, a conjunto o a conjunto oficial; "Cartógrafo" tiene dos fichas distintas, en `/conjuntos/{slug}` y en `/conjuntos/oficiales/{slug}`; un oficial sin componentes muestra `pending-mark`; valores nulos → `pending-mark`; dudas del conjunto visibles
- [ ] T055 [P] [US3] Escribir `tests/HackMaster.Web.Tests/StylesPageTests.cs`: lista los estilos con características, dado, precio, dificultad y libro; la ficha muestra los requisitos con `PrerequisiteView`

### Implementation

- [ ] T056 [P] [US3] Crear `src/HackMaster.Web/Pages/Suites.razor` en `/conjuntos` (admite `q` en la dirección) y `src/HackMaster.Web/Pages/SuiteDetail.razor` en `/conjuntos/{slug}` y `/conjuntos/oficiales/{slug}`
- [ ] T057 [P] [US3] Crear `src/HackMaster.Web/Pages/Styles.razor` en `/estilos` y `src/HackMaster.Web/Pages/StyleDetail.razor` en `/estilos/{slug}`
- [ ] T058 [US3] Añadir a `src/HackMaster.Web/wwwroot/css/app.css` los estilos de las listas y fichas de conjuntos y estilos a 390 px y 820 px

**Checkpoint**: historias 1 a 3 funcionan.

---

## Phase 8: User Story 4 - Ver las dudas abiertas (Priority: P4)

**Goal**: página con todas las dudas, enlazadas con las fichas afectadas e iguales a
`docs/dudas.md`.
**Independent Test**: fila "Dudas" del paso 4 de `quickstart.md`.
**Verificación**: verificación común; `DoubtsPageTests` pasa.
**Restricciones**: la página no añade ni omite dudas respecto a los datos.

- [ ] T059 [P] [US4] Escribir `tests/HackMaster.Web.Tests/DoubtsPageTests.cs`: lista cada duda con elemento, fuente, duda y decisión provisional; separa las de la hoja de las detectadas; el `Target` de cada duda enlaza a su ficha (habilidad, conjunto, conjunto oficial o estilo); una duda general se muestra como "General" sin enlace
- [ ] T060 [US4] Crear `src/HackMaster.Web/Pages/Doubts.razor` en `/dudas` y sus estilos en `src/HackMaster.Web/wwwroot/css/app.css`
- [ ] T061 [US4] Añadir a `tests/HackMaster.Domain.Tests/DataIntegrityTests.cs` la comprobación de que `docs/dudas.md` contiene el `subject` de todas las dudas de `data/doubts.json` (requiere T033)

**Checkpoint**: todas las historias funcionan.

---

## Phase 9: Pruebas de interfaz, CI y entrega

**Objetivo**: flujos críticos comprobados en un navegador real a 390 px y 820 px, en local y en CI;
pull request revisado.
**Verificación**: paso 5 de `quickstart.md` en local; CI en verde en el pull request en menos de 10
minutos.
**Restricciones**: acciones fijadas por SHA; permisos mínimos; `dotnet test` debe seguir pasando en
un equipo sin navegadores instalados.

- [ ] T062 Añadir `Microsoft.Playwright` a `tests/HackMaster.Web.Tests/HackMaster.Web.Tests.csproj` y crear `tests/HackMaster.Web.Tests/E2E/E2EFactAttribute.cs` (omite la prueba si falta `HACKMASTER_E2E_URL`; categoría `E2E`) y `tests/HackMaster.Web.Tests/E2E/BrowserFixture.cs` (Chromium, tamaños 390×844 y 820×1180)
- [ ] T063 [P] Escribir `tests/HackMaster.Web.Tests/E2E/SkillsFlowTests.cs`: 343 resultados al entrar; "botanica" muestra "Botánica" la primera (comprueba la normalización en WebAssembly); combinar dos libros y un tipo; abrir una ficha, volver atrás y conservar la consulta; recargar una ficha carga la misma ficha
- [ ] T064 [P] Escribir `tests/HackMaster.Web.Tests/E2E/NoHorizontalOverflowTests.cs`: en `/`, `/habilidades` (con filtros abiertos), una ficha de habilidad, `/conjuntos`, una ficha de conjunto, `/estilos` y `/dudas`, a 390 px y 820 px, `scrollWidth` no supera `clientWidth` (FR-026, SC-006)
- [ ] T065 [P] Crear `tests/README.md` en castellano: instalar el navegador de Playwright, arrancar la aplicación, definir `HACKMASTER_E2E_URL` y ejecutar `dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"`
- [ ] T066 Añadir a `.github/workflows/ci.yml` un trabajo que publica la aplicación, la sirve en local, instala Chromium y ejecuta las pruebas `E2E`; acciones fijadas por SHA; comprobar que el total queda por debajo de 10 minutos
- [ ] T067 Medir SC-003 (resultados en menos de 0,5 s con 343 elementos) en el navegador y anotar el resultado en `research.md` §11; si no se cumple, adoptar `<Virtualize>` en `src/HackMaster.Web/Pages/Skills.razor`
- [ ] T068 Recorrer `quickstart.md` completo con el usuario, incluida la comparación de 30 habilidades y 10 conjuntos al azar con la hoja (SC-005), y registrar los resultados; anotar como dato informativo la cobertura de pruebas de `src/HackMaster.Domain` (orientativa: 90 %, principio IV)
- [ ] T069 Revisión independiente del diff por un subagente con contexto limpio (regresiones, pruebas débiles, violaciones de la constitución, restos de texto de manual) y aplicar lo que encuentre
- [ ] T070 Actualizar `docs/ESTADO.md` y `docs/PRODUCTO.md` (nombres actuales de los comandos `/speckit-…`, dudas A5 trasladadas a la hoja); abrir el pull request en castellano con qué se hizo, cómo se verificó y qué falta
- [ ] T071 Tras fusionar, validar el paso 6 de `quickstart.md` en el sitio publicado y anotarlo en el pull request

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 → Phase 2 → Phase 3 (US5) → Phase 4 (importación real, depende del usuario).
- Phase 5 (US1) depende de Phase 2; **no** depende de Phase 4 (usa `TestData`).
- Phase 6 (US2) depende de Phase 5; Phase 7 (US3) depende de Phase 6 (reutiliza `PrerequisiteView`,
  `PendingMark` y `DoubtNotice`); Phase 8 (US4) depende de Phase 6.
- Phase 9 depende de las fases 4 a 8: las pruebas de interfaz cuentan 343 resultados reales.

### Tareas que necesitan al usuario

- T031, T032, T033 y T035: la hoja real solo está en su equipo.
- T030: cambio en `CLAUDE.md`. T068: comparación con la hoja. T070 y T071: pull request y fusión.

### Within Each User Story

Pruebas escritas y en rojo → modelo y lógica → componentes y páginas → estilos.

### Parallel Opportunities

- Phase 1: T004 y T005. Phase 2: T007 y T009 a T013.
- Phase 3: T015 a T021 (un archivo de pruebas cada una); después T027 y T028.
- Phase 5: T036 a T040; después T042 junto a T041.
- Phase 6: T048 y T049; T050 y T051. Phase 7: T054 y T055; T056 y T057.
- Phase 9: T063, T064 y T065.
- Mientras el usuario corrige la hoja (T032), se avanza en las fases 5 a 8.

## Parallel Example: User Story 5

```text
# Pruebas del importador, todas en archivos distintos:
T015 PrerequisiteParserTests.cs
T016 ImporterSkillsTests.cs
T017 ImporterValidationTests.cs
T018 ImporterSuitesTests.cs
T019 WorkbookReaderTests.cs
T020 DataWriterTests.cs
T021 CliTests.cs
```

## Implementation Strategy

### MVP First

MVP = Phase 1 a 4 más Phase 5: datos reales validados y consulta de habilidades con búsqueda y
filtros. Es lo mínimo que ya sustituye a la hoja en la mesa.

### Incremental Delivery

1. Phase 1 a 3: importador probado → primer commit revisable sin datos reales.
2. Phase 4 con el usuario: datos reales en el repositorio.
3. Phase 5: consulta de habilidades. 4. Phase 6: fichas. 5. Phase 7: conjuntos y estilos.
6. Phase 8: dudas. 7. Phase 9: pruebas de interfaz, CI, revisión y pull request.

Cada paso deja la solución construyendo y con las pruebas en verde.

## Notes

- Una tarea = un commit pequeño en castellano, con alcance (`feat(import): …`, `test(domain): …`).
- Si una fase no alcanza su objetivo en 8 iteraciones, se para y se resume qué falla.
- Si cambia un requisito, primero `spec.md`, después plan y tareas.
