---

description: "Task list for F000 — Base del proyecto"
---

# Tasks: Base del proyecto (F000)

**Input**: Design documents from `/specs/001-base-proyecto/`

**Prerequisites**: plan.md, spec.md, research.md, quickstart.md

**Tests**: incluidas; la constitución (principio IV) exige test primero.

**Organization**: por historia de usuario. Cada fase se ejecuta como un bucle con objetivo,
verificación, restricciones y límite de 8 iteraciones (formato de `CLAUDE.md`).

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

**Objetivo**: repositorio con rama de trabajo y política de construcción común.
**Verificación**: `git status` limpio en la rama `001-base-proyecto`; `dotnet --version` muestra 10.x.
**Restricciones**: no tocar `docs/`, `.specify/` ni `.claude/`.

- [X] T001 Commit inicial en `main` con documentación y Spec Kit (`CLAUDE.md`, `docs/`, `.specify/`, `.claude/`, `specs/`), sin código; lo ejecuta el usuario
- [X] T002 Crear la rama `001-base-proyecto` desde `main`
- [X] T003 [P] Crear `.gitignore` (salida de .NET, IDE, secretos y documentos fuente: `*.pdf`, `*.doc`, `*.docx`, `*.xls`, `*.xlsx`)
- [X] T004 [P] Crear `global.json` fijando el SDK de .NET 10 con `rollForward: latestFeature`
- [X] T005 [P] Crear `Directory.Build.props` (`TreatWarningsAsErrors`, `Nullable`, `ImplicitUsings`) y `Directory.Packages.props` (gestión central de versiones)
- [X] T006 [P] Crear `.editorconfig` con las reglas de formato de C# que comprueba `dotnet format`

---

## Phase 2: Foundational

**Objetivo**: solución con los cinco proyectos vacíos que construye sin avisos.
**Verificación**: `dotnet restore` y `dotnet build -warnaserror` terminan con 0 avisos.
**Restricciones**: `Domain` sin referencias de paquete; sin proyectos adicionales.

- [X] T007 Crear `HackMaster.slnx` en la raíz
- [X] T008 [P] Crear la biblioteca `src/HackMaster.Domain/HackMaster.Domain.csproj`, sin dependencias
- [X] T009 [P] Crear la aplicación Blazor WebAssembly autónoma `src/HackMaster.Web/HackMaster.Web.csproj`, sin PWA, con referencia a `Domain`, y retirar las páginas de ejemplo de la plantilla
- [X] T010 [P] Crear `tests/HackMaster.Domain.Tests/HackMaster.Domain.Tests.csproj` (xUnit) con referencia a `Domain`
- [X] T011 [P] Crear `tests/HackMaster.Web.Tests/HackMaster.Web.Tests.csproj` (bUnit + xUnit) con referencia a `Web`
- [X] T012 [P] Crear el esqueleto de consola `tools/DataImport/DataImport.csproj` con `tools/DataImport/Program.cs`, que informa de que la importación llega en F001 y termina con código distinto de cero
- [X] T013 Añadir los cinco proyectos a `HackMaster.slnx` y fijar las versiones de paquetes en `Directory.Packages.props`, verificadas en NuGet

**Checkpoint**: la solución construye sin avisos.

---

## Phase 3: User Story 1 - Construir y probar en local (Priority: P1) 🎯 MVP

**Goal**: construcción sin avisos y dos pruebas de ejemplo en verde con comandos documentados.
**Independent Test**: pasos 1 y 2 de `quickstart.md` en una copia limpia.
**Verificación**: `dotnet build -warnaserror`, `dotnet test --no-build`, `dotnet format --verify-no-changes`.
**Restricciones**: no debilitar ni borrar pruebas; ninguna regla de juego.

### Tests (primero; deben fallar antes de implementar)

- [X] T014 [P] [US1] Escribir `AppInfo_Name_Is_HackMaster_Companion__PRODUCTO` en `tests/HackMaster.Domain.Tests/AppInfoTests.cs` y comprobar que falla
- [X] T015 [P] [US1] Escribir `Home_Shows_App_Name__F000` en `tests/HackMaster.Web.Tests/HomePageTests.cs` (bUnit) y comprobar que falla

### Implementation

- [X] T016 [US1] Implementar `AppInfo.Name` en `src/HackMaster.Domain/AppInfo.cs`
- [X] T017 [US1] Implementar la página inicial en `src/HackMaster.Web/Pages/Home.razor`, que muestra `AppInfo.Name` en castellano y ningún contenido de juego; título e idioma `es` en `src/HackMaster.Web/wwwroot/index.html`
- [X] T018 [US1] Crear `README.md` en castellano con requisitos y comandos de construcción, prueba, formato y ejecución local
- [ ] T019 [US1] Actualizar `CLAUDE.md`: comandos de Spec Kit con guion (`/speckit-…`) y ruta del Excel fuera del repositorio; requiere confirmación del usuario

**Checkpoint**: historia 1 verificable en local.

---

## Phase 4: User Story 2 - Verificación automática de cada cambio (Priority: P2)

**Goal**: cada pull request recibe un veredicto automático.
**Independent Test**: paso 3 de `quickstart.md`.
**Verificación**: el flujo "CI" termina en verde en el pull request de F000, en menos de 10 minutos.
**Restricciones**: acciones con versión fijada; permisos mínimos (`contents: read`).

- [X] T020 [US2] Crear `.github/workflows/ci.yml`: en `pull_request` y `push` a `main`, instalar el SDK según `global.json` y ejecutar `restore`, `build -warnaserror`, `test --no-build` y `format --verify-no-changes`
- [ ] T021 [US2] Abrir el pull request de `001-base-proyecto` y comprobar que CI pasa; comprobar en un commit de prueba, después revertido, que una prueba rota lo deja en rojo

**Checkpoint**: historias 1 y 2 verificables.

---

## Phase 5: User Story 3 - Página vacía publicada (Priority: P3)

**Goal**: la aplicación vacía se publica sola desde `main` bajo `/HackMasterCompanion/`.
**Independent Test**: paso 4 de `quickstart.md`.
**Verificación**: la dirección pública carga la página sin recursos rotos; una ruta interna carga la aplicación.
**Restricciones**: ruta base en un único lugar; sin secretos; no publicar si CI falla.

- [X] T022 [US3] Crear `.github/workflows/pages.yml`: tras CI correcto en `main`, `dotnet publish` de `src/HackMaster.Web`, reescribir `<base href>` con `PAGES_BASE_PATH`, copiar `index.html` a `404.html`, añadir `.nojekyll`, subir el artefacto y desplegar en Pages
- [ ] T023 [US3] Activar GitHub Pages con origen "GitHub Actions" en la configuración del repositorio; lo ejecuta el usuario
- [ ] T024 [US3] Tras fusionar, validar el paso 4 de `quickstart.md` y anotar el resultado en el pull request

---

## Phase 6: Polish

- [ ] T025 Revisión independiente del diff por un subagente con contexto limpio (regresiones, pruebas débiles, violaciones de la constitución) y aplicar lo que encuentre
- [ ] T026 Completar la descripción del pull request en castellano: qué se hizo, cómo se verificó, qué falta
- [ ] T027 Recorrer `quickstart.md` completo y registrar los resultados

## Dependencies

- Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US3) → Phase 6.
- US2 necesita la solución y las pruebas de US1; US3 necesita el flujo de CI de US2.
- T021 y T024 necesitan permiso de push o que el usuario haga el push.

## Parallel Example

- Phase 1: T003 a T006 en paralelo (archivos distintos).
- Phase 2: T008 a T012 en paralelo; T013 al final.
- Phase 3: T014 y T015 en paralelo.

## Implementation Strategy

MVP = Phase 1 a 3 (historia 1). Después, historia 2 y luego historia 3, cada una con su verificación
antes de pasar a la siguiente.
