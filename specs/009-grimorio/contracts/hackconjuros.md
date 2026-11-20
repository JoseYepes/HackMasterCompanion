# Contrato: hoja de conjuros y orden de importación (F009)

## Orden

```
dotnet run --project tools/DataImport -- conjuros <Hackconjuros.xlsx> <carpeta-datos>
```

Lo habitual es guardar la ruta de la hoja en la variable de entorno `HACKMASTER_SPELLS_EXCEL` (como
`HACKMASTER_EXCEL` para las habilidades) y usar `data/` como carpeta. Solo escribe
`spells.json`; los demás archivos de datos no se tocan. Códigos de salida como la importación de
habilidades: 0 correcto, 1 errores de datos (no se escribe nada), 2 uso o estructura.

## Pestaña «Conjuros» (cabecera en la fila 1)

| Col. | Cabecera | Obligatorio | Notas |
|---|---|---|---|
| A | Nombre | sí | único por clase |
| B | Clase | sí | «Mago» o «Clérigo» (sin importar acentos ni mayúsculas) |
| C | Nivel | sí | mago 0–9 (0 = trucos); clérigo 1–7 |
| D | Escuela o esfera | no | varias separadas por comas |
| E | Reversible | no | nombre de la versión inversa |
| F–K | Alcance, Componentes, Duración, Tiempo de lanzamiento, Área de efecto, Tirada de salvación | no | texto como en el MJ |
| L | Libro | sí | «MJ 175»; varios con «;» |
| M | Resumen propio | no (aviso) | nunca el texto del manual |

Una celda con «?» marca el dato como pendiente de verificar.

## Pestaña «Reglas»

| Col. | Cabecera | Obligatorio |
|---|---|---|
| A | Regla | sí |
| B | Resumen propio | sí |
| C | Libro | sí («MJ 124»; varios con «;») |
