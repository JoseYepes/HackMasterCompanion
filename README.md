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

La construcción trata cualquier aviso como error.

## Estructura

```
src/HackMaster.Domain          reglas puras, sin dependencias externas
src/HackMaster.Web             aplicación Blazor WebAssembly
tests/HackMaster.Domain.Tests  pruebas del dominio (xUnit)
tests/HackMaster.Web.Tests     pruebas de componentes (bUnit)
tools/DataImport               importador de datos (llega con F001)
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
