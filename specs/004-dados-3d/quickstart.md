# Quickstart: validar F003

## 1. Construir y probar

```
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

**Esperado**: 0 avisos y todas las pruebas en verde. Entre ellas están `DiceBoardTests` (plan de
dados, d100, límite de 20) y la migración del historial de la versión 1 a la 2.

## 2. Pruebas de navegador

Con la aplicación en marcha (ver `tests/README.md`):

```
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
```

**Esperado**: `Dice3DTests` en verde.

- 1 000 tiradas simuladas con todas las formas, y la cara de arriba coincide siempre (SC-001).
- Una tirada en pantalla termina en menos de 4 s (SC-004).
- Sin desbordamiento a 390 y 820 px (SC-006).

## 3. A mano, en el móvil

1. Abrir `/dados`. **Esperado**: «Dados 3D» desactivado; se tira como en F002.
2. Activar «Dados 3D» y tirar `2d6+3`. **Esperado**: dos dados ruedan y se paran con las caras del
   detalle; el total sale al pararse.
3. Tirar `1d100`. **Esperado**: dos d10, uno de decenas y otro de unidades, que forman el valor.
4. Tocar los dados mientras ruedan. **Esperado**: se paran al momento.
5. Tirar `30d6`. **Esperado**: sin 3D y con un aviso.
6. Medir los fotogramas por segundo con 6 dados (`6d6`), con las herramientas de desarrollo del
   navegador o un medidor en pantalla. **Esperado**: 30 o más. Anotar el modelo del móvil en
   `research.md` (SC-003).
