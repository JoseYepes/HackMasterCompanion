# Quickstart: validar F000

## Requisitos

- SDK de .NET 10 (`dotnet --version` debe mostrar 10.x)
- Git

## 1. Construir y probar en local (historia 1)

```
dotnet restore
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

Resultado esperado: 0 avisos, 0 errores; 2 pruebas superadas (una de dominio, una de interfaz);
`format` sin cambios pendientes.

## 2. Ejecutar la aplicación en local

```
dotnet run --project src/HackMaster.Web
```

Resultado esperado: la dirección local que indique la consola muestra una página con el título
"HackMaster Companion" y nada más.

## 3. Verificación automática (historia 2)

Abrir un pull request contra `main`. Resultado esperado: el flujo "CI" aparece en el pull request y
termina en verde en menos de 10 minutos. Una prueba rota o un aviso lo dejan en rojo.

## 4. Publicación (historia 3)

Requisito único: en GitHub, Settings → Pages → Source: "GitHub Actions".

Tras fusionar en `main`, abrir `https://joseyepes.github.io/HackMasterCompanion/`. Resultado esperado:

- la página muestra "HackMaster Companion";
- la consola del navegador no registra recursos con error 404;
- `https://joseyepes.github.io/HackMasterCompanion/una-ruta-cualquiera` carga la aplicación y no la
  página de error de GitHub.
