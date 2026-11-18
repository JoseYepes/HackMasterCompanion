# Contrato: direcciones nuevas de F002

Se suman a las de F001 (`specs/002-consulta-habilidades/contracts/rutas.md`). Todas cargan al
abrirse directamente o al recargar.

| Dirección | Página | Contenido |
|---|---|---|
| `/dados` | Tirador | Campo de fórmula, opción "Los dados penetran", botones Tirar y Repetir, resultado con el detalle e historial de las últimas tiradas |
| `/conjuntos/nuevo` | Creador | Buscador de habilidades y conjuntos, componentes elegidos, resumen calculado, nombre, características y Guardar |
| `/conjuntos/{slug}/editar` | Creador | El mismo, cargado con un conjunto del jugador; también Borrar |
| `/conjuntos` | Lista de conjuntos (F001) | Los conjuntos del jugador aparecen con los 36 de la hoja, ordenados y buscables igual; botón "Crear conjunto" |
| `/conjuntos/{slug}` | Ficha de conjunto (F001) | También para los del jugador, con los valores calculados; si es del jugador, botones Editar y Borrar |

Las direcciones no pueden chocar con un conjunto: "nuevo" y "oficiales" se reservan y no se
admiten como `slug` de un conjunto del jugador. `/conjuntos/{slug}/editar` con un `slug` que no es del jugador
muestra la página "no encontrada". La navegación principal suma "Dados".

## Parámetros

| Dirección | Parámetro | Uso |
|---|---|---|
| `/dados` | `f` | Fórmula inicial (`/dados?f=2d6%2B3`); si no es válida se muestra con su error, sin tirar |

## Elementos que las pruebas de interfaz pueden localizar

`dice-formula`, `dice-penetrating`, `dice-roll`, `dice-repeat`, `dice-error`, `dice-total`,
`dice-detail`, `roll-history`, `history-row`, `history-clear`, `suite-search`,
`suite-component`, `suite-summary`, `suite-name`, `suite-save`, `suite-delete`,
`suite-cannot-save`, `storage-warning`.
