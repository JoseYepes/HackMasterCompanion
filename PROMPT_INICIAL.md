# Prompt inicial (Claude Code en tu PC)

## Antes de lanzarlo (lo haces tú)
1. Repositorio clonado, con `docs/PRODUCTO.md` y `CLAUDE.md` en la raíz/`docs`, y `CONSTITUCION_BORRADOR.md` en `docs/`.
2. Spec Kit instalado según su README oficial, y en la raíz del repositorio: `specify init --here --ai claude` (nombre y opciones del comando: confirma en el README de `github/spec-kit`, porque pueden haber cambiado). Debe crear `.specify/` y los comandos `/speckit.*` en `.claude/commands/`.
3. .NET SDK (LTS), Git y Node (para Playwright) instalados. Comprueba con `dotnet --version`, `git --version`, `node --version`.
4. Abre Claude Code en la carpeta del repositorio.

## Prompt (pégalo en Claude Code)

Eres el agente de desarrollo de **HackMaster Companion** (PWA en .NET / Blazor WASM para un grupo privado de HackMaster 4.ª edición, en castellano). Trabajamos con **GitHub Spec Kit**.

Lee `CLAUDE.md`, `docs/PRODUCTO.md` y `docs/CONSTITUCION_BORRADOR.md`. Son vinculantes. Si hay contradicciones o faltan datos, dímelo antes de seguir.

**Paso 1: comprobaciones (sin cambiar nada).** Verifica que `.specify/` y los comandos `/speckit.*` existen, que `dotnet`, `git` y `node` funcionan y en qué rama estamos. Dime qué versión de .NET LTS hay instalada. Lista cualquier problema.

**Paso 2: constitución.** Ejecuta `/speckit.constitution` usando como base `docs/CONSTITUCION_BORRADOR.md` sin añadir principios nuevos. Enséñame el resultado y **espera mi aprobación**.

**Paso 3: funcionalidad F000 (Base del proyecto).**
- `/speckit.specify` con la descripción de F000 de `docs/PRODUCTO.md` (solo qué y por qué).
- `/speckit.clarify`: hazme las preguntas que hagan falta. Pregúntame como mínimo: nombre del repositorio y de la solución, ruta base de GitHub Pages, versión de .NET y librería para IndexedDB.
- `/speckit.plan` con la parte B de `docs/PRODUCTO.md`.
- `/speckit.tasks` y `/speckit.analyze`.
- **Detente y enséñame spec, plan, tareas y el resultado de analyze. No implementes hasta que yo diga "adelante".**

**Paso 4 (solo tras mi "adelante"): `/speckit.implement` de F000**, tarea a tarea, cada grupo como un bucle con objetivo, verificación, restricciones y límite de 8 iteraciones (formato en `CLAUDE.md`). Rama de trabajo, commits pequeños, pull request preparado con descripción en castellano (qué se hizo, cómo se verificó, qué falta). **Sin push a `main`.** Antes de dar F000 por terminada, pide una revisión independiente de tu diff a un subagente revisor con contexto limpio y aplica lo que encuentre.

**Criterio de F000 terminada:** `dotnet build -warnaserror` sin avisos, test de ejemplo pasando en CI y página vacía publicada en Pages con la ruta base correcta.

**Reglas permanentes:** no inventes datos de reglas; tests primero y nunca debilitados; dos proyectos de producción más `tools/DataImport`; confirma conmigo cualquier cambio de alcance, dependencias o arquitectura; no crees recursos de Azure.

Al terminar cada sesión, resumen corto: qué se hizo, comandos de verificación y resultado, pendiente, y qué decisión necesitas de mí.
