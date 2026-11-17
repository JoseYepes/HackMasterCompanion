# Estado del proyecto

> Nota de traspaso entre sesiones. Recoge lo que no está en la constitución, en `PRODUCTO.md` ni en
> las specs: decisiones tomadas de palabra, hallazgos y lo que queda por decidir. Actualízala al
> terminar cada sesión.

Última actualización: 2026-11-16

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
  `Domain`, importador completo en `tools/DataImport` y 147 pruebas en verde. Falta de T030 el
  retoque de `CLAUDE.md` (requiere confirmación del usuario). Siguiente: fase 4 con el usuario.
- **Ensayo del importador con la hoja real (2026-11-17, sin escribir en el repositorio)**: lee las
  cuatro pestañas y falla con 16 errores, todos de datos: 11 requisitos de "Habilidades" (filas
  48, 49, 50, 117, 173, 215, 262 ×2, 278, 282, 293), 3 de "Conjuntos de habilidades" (filas 245 ×2
  y 258) y 2 dudas (filas 2 "Justa" y 3 "granjero / mercader / noble") que no coinciden con ningún
  nombre. Repetir el comando del README da la lista exacta.
- **Tarea del usuario antes de la primera importación**: corregir en el Excel los requisitos que
  no coinciden con ninguna habilidad (el importador falla siempre y los lista) y pasar a la
  pestaña "Dudas" las dudas de `PRODUCTO.md` A5 que faltan. El Excel es la única fuente de dudas;
  `docs/dudas.md` se genera. Cada fila de "Dudas" nombra un solo elemento con su nombre exacto: la
  duda de "granjero / mercader / noble" hay que dividirla en tres filas.
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
