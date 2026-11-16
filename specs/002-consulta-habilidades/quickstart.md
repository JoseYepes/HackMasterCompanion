# Quickstart: validar F001

Guía para comprobar de principio a fin que F001 funciona. Los detalles del modelo están en
[data-model.md](./data-model.md) y los contratos en [contracts/](./contracts/).

## Requisitos

- SDK de .NET 10 y Git (como en F000).
- Para el paso 2: la hoja `Hackhabilidades_completo.xlsx` y la variable de entorno
  `HACKMASTER_EXCEL` con su ruta. Solo en el equipo del mantenedor.
- Para el paso 5: navegador de Playwright instalado (ver `tests/README.md`).

## 1. Construir y probar

```
dotnet restore
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
```

**Esperado**: 0 avisos; todas las pruebas pasan (las de interfaz con Playwright aparecen como
omitidas); el formato no cambia nada. Las pruebas de integridad confirman 343 habilidades, 36
conjuntos de habilidades, 23 oficiales y 5 estilos en `data/`.

## 2. Regenerar los datos (solo el mantenedor)

PowerShell:

```
dotnet run --project tools/DataImport -- "$env:HACKMASTER_EXCEL" data/
```

**Esperado**: "Importación correcta." con los recuentos anteriores y la lista de avisos; código de
salida 0. Después, `git status` no muestra cambios en `data/` ni en `docs/dudas.md` si la hoja no
ha cambiado (importación repetible).

Comprobación de errores, sobre una **copia** de la hoja: cambiar un requisito por un nombre que no
existe y volver a ejecutar con esa copia.

**Esperado**: "La importación ha fallado", con la pestaña, la fila y el nombre; código de salida 1;
`git status` sin cambios.

## 3. Comprobar que no hay texto de manual

Elegir una frase de la columna "Texto del manual" de la hoja y buscarla en el repositorio:

```
git grep -n "<frase>"
```

**Esperado**: ningún resultado.

## 4. Probar la aplicación en local

```
dotnet run --project src/HackMaster.Web
```

Abrir la dirección que indique la consola y, con el navegador a 390 px de ancho:

| Acción | Esperado |
|---|---|
| Entrar en "Habilidades" | 343 resultados |
| Escribir "botanica" | "Botánica" aparece la primera |
| Escribir una palabra que solo esté en un resumen | La habilidad aparece con el fragmento del resumen |
| Marcar los libros "MJ" y "CG" y un tipo | Solo habilidades de esos libros y ese tipo; el contador cambia |
| Pulsar "Quitar filtros" | Vuelven los 343 |
| Abrir "Herbalismo" | Requisito "Botánica" enlazado; conjuntos que la contienen |
| Volver atrás | La lista conserva búsqueda y filtros |
| Abrir "Cerrajería" | Aviso con la duda abierta |
| Entrar en "Conjuntos" y abrir "Alquimista" | 5 componentes enlazados; valores de la hoja |
| Abrir un conjunto oficial | Componentes "pendiente de verificar" |
| Entrar en "Estilos" | 5 estilos |
| Entrar en "Dudas" | Las mismas dudas que `docs/dudas.md` |
| Recargar una ficha | Carga la misma ficha |

En ningún paso debe aparecer desplazamiento horizontal. Repetir un recorrido breve a 820 px.

## 5. Pruebas de interfaz automáticas

Con la aplicación en marcha (paso 4) y su dirección en `HACKMASTER_E2E_URL`:

```
dotnet test tests/HackMaster.Web.Tests --no-build --filter "Category=E2E"
```

**Esperado**: pasan a 390 px y 820 px, incluida la comprobación de que no hay desbordamiento
horizontal.

## 6. Verificación automática y publicación

Abrir el pull request de `002-consulta-habilidades`.

**Esperado**: CI en verde, incluido el trabajo de pruebas de interfaz, en menos de 10 minutos. Tras
fusionar, https://joseyepes.github.io/HackMasterCompanion/habilidades muestra las 343 habilidades.
