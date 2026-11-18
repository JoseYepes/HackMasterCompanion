# Pruebas

| Proyecto | Qué prueba | Herramienta |
|---|---|---|
| `HackMaster.Domain.Tests` | Reglas del dominio: normalización de texto, búsqueda y filtros | xUnit |
| `HackMaster.DataImport.Tests` | Importador de la hoja de cálculo (con libros creados en memoria) | xUnit |
| `HackMaster.Web.Tests` | Componentes y páginas, con datos de prueba (`TestData.cs`) | bUnit |
| `HackMaster.Web.Tests/E2E` | Flujos críticos en un navegador real, a 390 px y 820 px | Playwright |

Los valores de `TestData.cs` son inventados: solo sirven para las pruebas y no son datos del juego.

## Pruebas habituales

```
dotnet build -warnaserror
dotnet test --no-build
```

Las pruebas de navegador (`E2E`) aparecen como **omitidas** mientras no esté definida la variable
de entorno `HACKMASTER_E2E_URL`. Es lo esperado: así `dotnet test` funciona en cualquier equipo.

## Pruebas de navegador (Playwright)

Necesitan los datos reales en `data/` (por ejemplo, esperan 343 habilidades).

1. Construir y, solo la primera vez, instalar Chromium para Playwright (descarga unos 150 MB;
   ya instalado ocupa unos 700 MB en `%LOCALAPPDATA%\ms-playwright`):

   ```
   dotnet build tests/HackMaster.Web.Tests
   pwsh tests/HackMaster.Web.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
   ```

   En Windows sin PowerShell 7 se puede usar `powershell` en lugar de `pwsh`.

2. Arrancar la aplicación en otra terminal:

   ```
   dotnet run --project src/HackMaster.Web --launch-profile http
   ```

3. Indicar dónde está la aplicación y ejecutar solo las pruebas de navegador:

   ```
   $env:HACKMASTER_E2E_URL = "http://localhost:5238/"
   dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
   ```

   En bash: `HACKMASTER_E2E_URL=http://localhost:5238/ dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"`.

En GitHub, el trabajo `e2e` de `.github/workflows/ci.yml` hace estos mismos pasos en cada pull request.

Las pruebas comprueban, entre otras cosas, que ninguna pantalla se desborda en horizontal a 390 px
ni a 820 px (FR-026) y que la búsqueda, los filtros y el botón de volver atrás funcionan con los
datos reales.

Las pruebas de los dados 3D (`Dice3DTests`) dibujan con WebGL por software (SwiftShader), porque el
equipo de GitHub no tiene tarjeta gráfica; el navegador se abre con `--enable-unsafe-swiftshader`.
Si después de recompilar la aplicación falla la carga de `js/dice3d.js`, reinicia la aplicación.

Estilo de fantasía heroica (F007):

- `ThemeContrastTests` (sin navegador) lee `wwwroot/css/app.css` y comprueba que cada color de los
  dos modos se lee sobre su fondo (contraste de WCAG 2.1) y que ninguna regla usa colores sueltos.
- `ThemeTests` (navegador) comprueba que las letras son propias y no se pide nada a otros sitios,
  que el modo guardado se aplica antes de pintar, que se recuerda, que sin elección se sigue al
  dispositivo y que la mesa de dados cambia con el modo.
- `NoHorizontalOverflowTests` recorre las pantallas en modo normal y en modo noche.
