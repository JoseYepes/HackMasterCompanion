# Quickstart: validar F007

## 1. Construir y probar

```
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```
Esperado: 0 avisos; pasan las pruebas de contraste y colores (`ThemeContrastTests`) y del botón
(`ThemeTogglePageTests`), además de todas las anteriores.

## 2. Pruebas de navegador

Con la aplicación arrancada (`tests/README.md`):

```
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
```

Esperado: pasan `ThemeTests` (sin destello, recuerda el modo, sigue al dispositivo, cambio en
menos de 0,5 s, botón de 44 px, ninguna petición fuera de la aplicación) y
`NoHorizontalOverflowTests` en los dos modos.

## 3. A mano

1. Abrir la aplicación en el móvil y recorrer las 12 pantallas de SC-001 en modo normal: se
   parece a la maqueta A.
2. Pulsar el botón de la luna: todo pasa a la maqueta B al momento.
3. Cerrar y volver a abrir: sigue en modo noche, sin destello claro.
4. En `/dados`, con «Dados 3D», tirar `2d6+1d20` en los dos modos: cuero granate y madera oscura;
   los números de los dados se leen.
5. Medir la primera carga sin caché (herramientas del navegador, pestaña Red) y compararla con
   los 2837 KB de antes: como mucho un 10 % más (SC-005).
