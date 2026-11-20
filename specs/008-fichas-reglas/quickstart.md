# Quickstart: Fichas con reglas (F005)

```powershell
dotnet build -warnaserror
dotnet test --no-build --filter "Category!=E2E"
dotnet format --verify-no-changes
```

Pruebas de navegador (ver `tests/README.md`), con el sitio en marcha:

```powershell
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"; dotnet test --no-build --filter "FullyQualifiedName~E2E.CharacterTests"
```

## Comprobación a mano (390 px)

1. Abrir una ficha con DES 18 y Dado de Honor d12.
2. Añadir «Corte a la yugular» (o cualquier habilidad de DES): maestría base 18, «principiante».
3. «Apuntar mejora» 3 + 8: maestría 29 %, «aprendiz».
4. «Tirar mejora»: se ven las dos tiradas y la maestría sube.
5. «Tirar» con −10: se ve el d100, el total y éxito o fallo; en `/dados` aparece en el historial.
6. Una ficha de F004 con maestría escrita la conserva.
