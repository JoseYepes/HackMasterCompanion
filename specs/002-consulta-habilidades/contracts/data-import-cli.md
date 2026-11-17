# Contrato: herramienta de importación (`tools/DataImport`)

## Uso

```
dotnet run --project tools/DataImport -- <hoja.xlsx> <carpeta-datos> [<archivo-dudas>]
```

| Argumento | Obligatorio | Valor habitual |
|---|---|---|
| `<hoja.xlsx>` | sí | `"%HACKMASTER_EXCEL%"` (PowerShell: `$env:HACKMASTER_EXCEL`) |
| `<carpeta-datos>` | sí | `data/` |
| `<archivo-dudas>` | no | `docs/dudas.md` |

## Códigos de salida

| Código | Significado | Efecto en disco |
|---|---|---|
| 0 | Importación correcta (puede haber avisos) | Los cuatro JSON y el archivo de dudas quedan actualizados |
| 1 | Errores de datos (FR-005, FR-006) | Ningún archivo cambia |
| 2 | Uso incorrecto, hoja inexistente o ilegible, pestaña o columna que falta | Ningún archivo cambia |

Los archivos se escriben primero en una carpeta temporal y se mueven al destino solo si todo ha
ido bien.

## Salida por consola (en castellano)

Correcta (salida estándar):

```
Importación correcta.
  Habilidades: 343
  Conjuntos de habilidades: 36
  Conjuntos oficiales: 23
  Estilos de combate: 5
  Dudas: 3 de la hoja, 12 detectadas
Avisos (12):
  [Habilidades, fila 87] «Liderazgo básico»: dado de maestría «5» no es un dado existente; queda pendiente de verificar.
  ...
```

Con errores (salida de error), **todos** los errores, no solo el primero:

```
La importación ha fallado: 6 errores. No se ha modificado ningún archivo.
  [Habilidades, fila 152] «Sigilo urbano»: el requisito «Disfraz» no coincide con ninguna habilidad ni conjunto.
  [Conjuntos de habilidades, fila 263] «Jinete»: el componente «saber animalx» no existe.
  [Habilidades, filas 40 y 212] nombre duplicado: «Leer/Escribir» y «Leer/escribir».
```

Formato de cada línea: `[<pestaña>, fila <n>] «<elemento>»: <mensaje>`. Los recuentos y las filas
del ejemplo son ilustrativos.

## Garantías

- Repetir la importación con la misma hoja produce archivos idénticos byte a byte.
- La herramienta no lee la columna "Texto del manual" y ningún archivo generado contiene su
  contenido.
- La herramienta no modifica la hoja de cálculo.
