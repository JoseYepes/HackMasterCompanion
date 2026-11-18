# Quickstart: validar F002

Guía para comprobar que F002 funciona. Los detalles están en [data-model.md](./data-model.md) y en
[contracts/rutas.md](./contracts/rutas.md).

## Requisitos

- SDK de .NET de `global.json`.
- Para las pruebas de navegador, Chromium de Playwright (ver `tests/README.md`).

## 1. Construir y probar

```
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

**Esperado**: 0 avisos y todas las pruebas en verde, entre ellas:

- `SuiteRulesMatchSheetTests`: los 36 conjuntos de `data/` recalculados coinciden, salvo las
  excepciones con nombre (SC-001).
- Las listas de fórmulas válidas y no válidas (SC-002).
- La uniformidad de 1d6 (SC-003).

## 2. Tirador

Arrancar la aplicación (`dotnet run --project src/HackMaster.Web --launch-profile http`) y abrir
`/dados`:

1. Escribir `2d6+3` y tirar. **Esperado**: dos dados entre 1 y 6, y el total es su suma más 3.
2. Escribir `2d` y tirar. **Esperado**: no se tira; mensaje que señala el error y un ejemplo.
3. Activar "Los dados penetran" y tirar `10d4` varias veces. **Esperado**: cuando sale un 4
   aparece un dado extra marcado, que suma su valor menos 1.
4. Recargar la página. **Esperado**: el historial sigue ahí.

## 3. Creador

1. En `/conjuntos`, pulsar "Crear conjunto". Añadir las 5 habilidades de Alquimista. **Esperado**:
   16 PC, 1403 po, dificultad 15 y d8, los mismos que la ficha de Alquimista.
2. Ponerle nombre «Prueba», elegir INT y guardar. **Esperado**: aparece en `/conjuntos` junto a los
   36, con el mismo aspecto.
3. Recargar. **Esperado**: sigue ahí. Editarlo, quitar componentes hasta dejar 1. **Esperado**: no
   deja guardar y explica que hacen falta entre 2 y 5.
4. Borrarlo y confirmar. **Esperado**: desaparece de la lista.

## 4. Pruebas de navegador

```
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
```

**Esperado**: todas en verde, también las nuevas de `/dados` y del creador a 390 px y 820 px.
