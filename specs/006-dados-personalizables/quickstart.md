# Quickstart: validar F008

## 1. Construir y probar

```
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

Esperado: 0 avisos; pasan `DiceLookTests`, las pruebas nuevas de `StoredDataTests` y
`DiceBoardTests`, y `DiceLookPanelTests`.

## 2. Pruebas de navegador

Con la aplicación arrancada (`tests/README.md`):

```
$env:HACKMASTER_E2E_URL = "http://localhost:5238/"
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
```

Esperado: las 1 000 tiradas siguen con la cara correcta y los dados redondeados apoyados en su
cara; sonidos limitados y en silencio cuando toca; efecto y aura en las penetraciones; calidad baja
sin texturas; tirada en menos de 4 s; nada nuevo se descarga sin 3D.

## 3. A mano, en el móvil

1. En `/dados`, con «Dados 3D», abrir «Aspecto de los dados» y probar los 11 materiales en la vista
   previa.
2. Elegir colores con poco contraste: aparece el aviso y la propuesta de números legibles.
3. Activar «Los dados penetran» y tirar `6d6` hasta ver una penetración: efecto, sonido y aura.
4. Probar fuego, sangre y «ninguno»; silenciar; calidad baja.
5. Comprobar que la animación va fluida en calidad alta (SC-003) y que los dados gustan (SC-006).
