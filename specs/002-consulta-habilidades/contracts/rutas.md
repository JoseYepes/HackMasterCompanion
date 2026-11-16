# Contrato: direcciones de la aplicación

Todas las direcciones son relativas a la ruta base (`/` en local, `/HackMasterCompanion/`
publicada) y deben cargar al abrirse directamente o al recargar.

| Dirección | Página | Contenido |
|---|---|---|
| `/` | Inicio | Nombre de la aplicación y enlaces a las cuatro secciones |
| `/habilidades` | Lista de habilidades | Búsqueda, filtros, orden y contador de resultados |
| `/habilidades/{slug}` | Ficha de habilidad | Datos, resumen, requisitos, conjuntos y dudas |
| `/conjuntos` | Lista de conjuntos | 36 conjuntos de habilidades y 23 oficiales, distinguidos; búsqueda por nombre |
| `/conjuntos/{slug}` | Ficha de conjunto | Valores, componentes y dudas |
| `/estilos` | Estilos de combate | Los 5 estilos |
| `/estilos/{slug}` | Ficha de estilo | Valores, requisitos y dudas |
| `/dudas` | Dudas abiertas | Todas las dudas, enlazadas con las fichas afectadas |

Un `slug` inexistente muestra la página "no encontrada" de F000.

## Parámetros de `/habilidades`

El estado de la consulta vive en la dirección, de modo que volver atrás lo recupera.

| Parámetro | Valores | Ejemplo |
|---|---|---|
| `q` | texto de búsqueda | `q=botanica` |
| `tipo` | uno o varios tipos (parámetro repetido) | `tipo=Artes%20marciales&tipo=Arcanas` |
| `libro` | uno o varios acrónimos | `libro=MJ&libro=CG` |
| `clase` | uno o varios grupos o clases | `clase=Grupo%20de%20ladrones` |
| `precioMin`, `precioMax` | enteros (po) | `precioMax=500` |
| `difMin`, `difMax` | enteros (%) | `difMin=10` |
| `orden` | `nombre` (por defecto), `pc`, `precio`, `dificultad`; prefijo `-` para descendente | `orden=-precio` |

Los parámetros desconocidos o con valores no válidos se ignoran sin error. `/conjuntos` admite `q`.

## Elementos que las pruebas de interfaz pueden localizar

Atributos `data-testid` estables: `search-input`, `result-count`, `skill-row`, `filter-type`,
`filter-book`, `filter-class`, `clear-filters`, `pending-mark`, `doubt-notice`.
