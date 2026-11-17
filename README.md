# HackMaster Companion

Aplicación web para un grupo privado de HackMaster 4.ª edición, en castellano: consulta de
habilidades, conjuntos, dados y fichas de personaje. Por ahora solo existe la base del proyecto
(F000): una página vacía que se construye, se prueba y se publica.

## Requisitos

- SDK de .NET 10 (la versión queda fijada en `global.json`)
- Git

## Comandos

```
dotnet restore
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
dotnet run --project src/HackMaster.Web
```

### Regenerar los datos de juego

Los datos de `data/` y el archivo `docs/dudas.md` se generan desde la hoja de cálculo, que vive
fuera del repositorio. Con su ruta en la variable de entorno `HACKMASTER_EXCEL` (PowerShell):

```
dotnet run --project tools/DataImport -- "$env:HACKMASTER_EXCEL" data/
```

Si la hoja tiene errores (un requisito que no coincide con ninguna habilidad, un nombre repetido…),
la herramienta los lista todos con su pestaña y su fila y no modifica ningún archivo. No edites
`data/` ni `docs/dudas.md` a mano.

La construcción trata cualquier aviso como error.

## Estructura

```
src/HackMaster.Domain          reglas puras, sin dependencias externas
src/HackMaster.Web             aplicación Blazor WebAssembly
tests/HackMaster.Domain.Tests  pruebas del dominio (xUnit)
tests/HackMaster.Web.Tests     pruebas de componentes (bUnit)
tests/HackMaster.DataImport.Tests  pruebas del importador (xUnit)
data/                          datos de juego generados (no se editan a mano)
tools/DataImport               importador de datos: hoja de cálculo → data/
docs/                          producto y decisiones
specs/                         una carpeta por funcionalidad (Spec Kit)
```

Las versiones de los paquetes se fijan en `Directory.Packages.props`.

## Publicación

Cada cambio en `main` se publica en GitHub Pages:
https://joseyepes.github.io/HackMasterCompanion/

La ruta base se define en un único lugar: la variable `PAGES_BASE_PATH` de
`.github/workflows/pages.yml`.

## Contenido

Los manuales y las hojas de cálculo fuente no se guardan en este repositorio. Los principios del
proyecto están en `.specify/memory/constitution.md`.
