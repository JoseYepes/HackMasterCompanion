# Research: Base del proyecto (F000)

Las versiones exactas de paquetes y acciones se fijan durante la implementación consultando la
fuente oficial (NuGet, repositorio de cada acción) y se anotan en `Directory.Packages.props` y en
los flujos. Ninguna se da aquí por sabida.

## 1. Versión de .NET

- **Decision**: .NET 10 (LTS), fijada en `global.json` con `rollForward: latestFeature`.
- **Rationale**: es la LTS vigente, soportada hasta noviembre de 2028. .NET 8 y .NET 9 quedaron sin
  soporte el 2026-11-10. La constitución exige "LTS vigente".
- **Alternatives considered**: .NET 9 (instalado en el equipo, sin soporte); .NET 11 (STS, recién
  publicado, no es LTS).
- **Fuente**: política de soporte de .NET, dotnet.microsoft.com (consultada el 2026-11-16).

## 2. Formato de la solución

- **Decision**: `HackMaster.slnx`, creado con `dotnet new sln`.
- **Rationale**: es el formato que genera el SDK actual; XML legible y con menos conflictos de fusión.
- **Alternatives considered**: `.sln` clásico (`dotnet new sln --format sln`) si alguna herramienta
  del equipo no admite `.slnx`. **Pendiente de verificar** en la implementación que el SDK 10
  instalado genera `.slnx` por defecto.

## 3. Avisos como errores y versiones fijadas

- **Decision**: `Directory.Build.props` con `TreatWarningsAsErrors`, `Nullable` y
  `ImplicitUsings`; gestión central de paquetes en `Directory.Packages.props`.
- **Rationale**: un único lugar para la política de construcción y para cada versión (FR-002, FR-009).
- **Alternatives considered**: repetir las propiedades en cada `.csproj` (duplicación).

## 4. Pruebas de ejemplo sin inventar reglas

- **Decision**: `Domain` expone `AppInfo.Name` ("HackMaster Companion"); la prueba de dominio lo
  comprueba y la prueba bUnit comprueba que la página inicial lo muestra.
- **Rationale**: da una prueba real por proyecto sin introducir ninguna regla de juego (principio I)
  y conecta dominio e interfaz con lo mínimo.
- **Alternatives considered**: una prueba vacía tipo `Assert.True(true)` (no verifica nada); una
  regla de juego real (pertenece a F001/F002 y exige fuente).

## 5. Publicación en GitHub Pages

- **Decision**: flujo `pages.yml` con las acciones oficiales de Pages (subir artefacto y desplegar),
  origen "GitHub Actions". En la publicación se reescribe `<base href>` a la ruta base, se copia
  `index.html` a `404.html` y se añade `.nojekyll`.
- **Rationale**: `404.html` hace que las direcciones internas carguen la aplicación (escenario 3 de
  la historia 3); `.nojekyll` evita que Pages ignore la carpeta `_framework`.
- **Alternatives considered**: rama `gh-pages` (más piezas y un token con permiso de escritura).
- **Acción manual del usuario**: activar Pages en el repositorio con origen "GitHub Actions".

## 6. Ruta base en un único lugar

- **Decision**: variable `PAGES_BASE_PATH: /HackMasterCompanion/` en `pages.yml`; en local la
  aplicación usa `/`.
- **Rationale**: cumple FR-007 y SC-005; renombrar el repositorio exige tocar una sola línea.
- **Alternatives considered**: fijar la ruta en `index.html` (rompe la ejecución local).

## 7. CI

- **Decision**: `ci.yml` en `pull_request` y en `push` a `main`: `restore`, `build -warnaserror`,
  `test --no-build`, `format --verify-no-changes`. `pages.yml` depende de que CI pase.
- **Rationale**: los mismos comandos que `CLAUDE.md`, de modo que local y CI coinciden.
- **Alternatives considered**: un solo flujo con todo (mezcla permisos de Pages con los de un
  pull request).

## 8. Limitación del entorno de desarrollo

El agente no dispone de `dotnet` ni de red para paquetes en su entorno. La verificación de
construcción y pruebas se hace en CI y con los comandos que el usuario ejecute en su equipo. El
agente tampoco puede hacer push hasta que la app de GitHub de Claude esté instalada en el repositorio.
