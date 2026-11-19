# Quickstart: Fichas de PJ (F004)

## Comprobaciones automáticas

```powershell
dotnet build -warnaserror
dotnet test --no-build --filter "Category!=E2E"
dotnet format --verify-no-changes
```

Pruebas de navegador (ver `tests/README.md`), con el sitio en marcha:

```powershell
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"; dotnet test --no-build --filter "FullyQualifiedName~E2E.CharacterTests"
```

Resultado esperado: todo en verde.

## Comprobación a mano (390 px)

1. Abrir «Fichas»: aparece «Aún no hay fichas» y «Nueva ficha».
2. Crear «Grimbold»: raza, clase, nivel 3, FUE 15/47, PG 31, Honor 12, la habilidad «Actuar» con
   maestría 25 y 2 puntos, equipo y notas. Guardar y recargar: todo sigue igual.
3. Exportar: se descarga `grimbold.hackmaster.json`.
4. Duplicar: aparece «Grimbold (copia)». Borrarla confirmando.
5. Borrar «Grimbold», importar el archivo: vuelve idéntica. Importarlo otra vez: pregunta
   «Sustituir» o «Añadir como copia».
6. Cambiar al modo noche: todo se lee; no hay desplazamiento horizontal a 390 ni a 820 px.
