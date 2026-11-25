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

Dados personalizables y sonido (F008):

- `DiceLookTests` (dominio) y `StoredDataTests`: aspecto de los dados, contraste de los números y
  historial versión 3.
- `DiceLookPanelTests` (bUnit): el panel «Aspecto de los dados» con el módulo 3D simulado.
- `E2E/DiceLookTests`: materiales, efectos al penetrar, aura, sonidos programados (el módulo los
  apunta en `heardSounds`, así que no hace falta oírlos) y calidad baja.
- `SoundFilesTests`: las grabaciones de `wwwroot/sounds/` que usa el módulo existen, están en su README
  con licencia CC0, ninguna sobra y pesan menos de 400 KB.
- `E2E/Dice3DTests`: las 1 000 tiradas comprueban además que la malla redondeada apoya en los
  mismos planos que el poliedro con el que rueda.

Fichas de PJ (F004):

- `CharacterTests` y `CharacterStorageTests` (dominio): qué impide guardar una ficha, la copia,
  exportar e importar idéntica y la migración del formato de la versión 1 a la 2.
- `PlayerCharactersTests` y `CharacterPagesTests` (bUnit): guardar, releer antes de escribir,
  duplicar, borrar, exportar e importar, con IndexedDB simulado.
- `E2E/CharacterTests`: con IndexedDB real, la ficha sigue al recargar; exportar, borrar e importar
  da el mismo archivo sin pedir nada a otros sitios; la copia es independiente; sin desbordamiento a
  390 y 820 px en los dos modos.

Fichas con reglas (F005):

- `HonorDieTests` y `SkillRulesTests` (dominio): dados de la Tabla 5C, maestría (ejemplo de Corte a
  la yugular del MJ p. 100, media a la baja, tope de 125 %), niveles y prueba de habilidad, con la
  página del MJ en el nombre de cada prueba.
- `CharacterStorageTests`: formato de fichas versión 3 (Dado de Honor y mejoras) y migraciones.
- `CharacterPagesTests` (bUnit): Dado de Honor, maestría calculada, mejoras tiradas y apuntadas,
  maestría escrita de F004 y prueba de habilidad guardada en el historial.
- `E2E/CharacterTests`: el flujo completo en el navegador, hasta ver la tirada en `/dados`.

Grimorio (F009):

- `SpellQueryTests` (dominio): búsqueda sin acentos ni mayúsculas y filtros de clase, nivel y escuela.
- `SpellImportTests` (importador): la hoja `Hackconjuros.xlsx`, campos pendientes con «?», errores y
  la orden `DataImport conjuros`.
- `GrimoirePagesTests` (bUnit) y `E2E/GrimoireTests`: lista, filtros en la dirección, ficha, reglas
  y desbordamiento.

Cuentas y sincronización (F006):

- `Sync/*Tests` (dominio): claves por cuenta, cola de cambios pendientes y reglas de fusión (gana el
  cambio más reciente; en empate, la nube).
- `HackMaster.Api.Tests`: validación del token con una clave de prueba, CORS y, contra el emulador de
  Cosmos DB, datos, aislamiento entre cuentas, invitaciones, exportar y borrar. Las pruebas del
  emulador se saltan si no está. Para ejecutarlas:

  ```
  docker run -d --name hm-cosmos -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
  $env:COSMOS_EMULATOR_ENDPOINT = "http://localhost:8081"
  dotnet test tests/HackMaster.Api.Tests
  ```
