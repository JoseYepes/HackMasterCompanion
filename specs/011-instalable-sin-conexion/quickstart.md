# Quickstart: comprobar F013

## Pruebas habituales

```
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

Las pruebas del sitio publicado (`Category=Published`) se omiten si no está definida
`HACKMASTER_PUBLISHED_DIR`.

## Sitio publicado con su ruta base, sin conexión

```
dotnet publish src/HackMaster.Web -c Release -o publish
bash tools/pages/apply-base-path.sh publish/wwwroot /HackMasterCompanion/
$env:HACKMASTER_PUBLISHED_DIR = "$PWD/publish/wwwroot"
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=Published"
```

Comprueban, a 390 y 820 px: que tras la primera visita todas las pantallas abren sin conexión, también
recargando; que la caché no tiene nada de `authentication/` ni de otros sitios y que esas direcciones
llegan a la red; que el navegador no da errores de instalabilidad; y que con una versión nueva sale el
aviso, «Actualizar» la carga y lo guardado sigue igual.

## A mano (usuario)

1. Abrir https://joseyepes.github.io/HackMasterCompanion/ en el móvil con conexión y esperar unos
   segundos.
2. Android: pulsar «Instalar» en el inicio. iPhone: «Compartir → Añadir a pantalla de inicio».
3. Poner el móvil en modo avión y abrir la aplicación desde su icono: debe abrir en menos de 3 s
   (SC-002) y dejar consultar, tirar dados y abrir fichas.

## Volver a hacer los iconos

```
powershell -File tools/Icons/New-AppIcons.ps1
```
