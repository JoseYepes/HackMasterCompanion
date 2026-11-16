 # Borrador de constitución — HackMaster Companion

> Texto de entrada para `/speckit.constitution`. El agente debe convertirlo en `.specify/memory/constitution.md` conservando el sentido, sin añadir principios nuevos sin consultar.

## Principios

### I. Contenido sin inventar
Los datos y las descripciones del juego proceden de las fuentes (libros, HackJournals, erratas, Excel del usuario) y **no se inventan**. Lo que falte se marca "pendiente de verificar". La columna `ManualText` es local del usuario y no se versiona ni se publica.

### II. Prioridad de fuentes de datos
Manuales extendidos (CG, GrG, SSG, ZG, PFG, KODT…) > Manual del Jugador > Excel del usuario. Castellano > inglés. Si dos fuentes discrepan, la duda se registra en `docs/dudas.md`; no se decide en silencio. Lo que falte se marca "pendiente de verificar" y nunca se inventa.

### III. Dominio puro y arquitectura mínima
`HackMaster.Domain` no depende de Blazor, JS interop, almacenamiento, HTTP ni infraestructura; es determinista (el azar entra por `IRandom`). Hay **dos proyectos de producción** (`Domain`, `Web`) más `tools/DataImport` hasta la fase de backend. No se crean capas, repositorios genéricos, MediatR, mapeadores ni interfaces "por si acaso": una abstracción se añade cuando existe un segundo caso de uso real, con un ADR corto. SOLID como guía, no como ritual.

### IV. Test primero (innegociable)
TDD en el dominio: los tests se escriben primero y se ven fallar. Un test por regla, con nombre que indique la regla y su fuente. Nunca se borra ni se debilita un test para que pase. Cobertura orientativa ≥ 90 % en `Domain`.

### V. Local primero
Todo funciona sin backend ni conexión hasta la fase de Azure. Los datos del juego se leen de JSON validado generado desde el Excel; la app no lee el Excel. Los esquemas persistidos llevan `SchemaVersion` y migraciones con test.

### VI. Entrega verificable
Cada objetivo tiene un criterio medible y comandos de verificación. Los bucles de implementación tienen tope de 8 iteraciones; si no se alcanza, se para y se informa. Commits pequeños, rama por funcionalidad, pull request siempre, sin push a `main`, y revisión independiente antes de fusionar.

### VII. Coste y seguridad
Solo servicios gratuitos permanentes o con cuota gratuita de Azure (el usuario no dispone de crédito de bienvenida ni de servicios gratuitos de 12 meses). Antes de crear recursos: confirmar con el usuario, verificar condiciones vigentes en la documentación oficial y tener alerta de presupuesto. Nada de secretos en el repositorio (público).

## Restricciones técnicas
.NET LTS vigente, C#, Blazor WebAssembly como PWA, xUnit + bUnit + Playwright, GitHub Pages para la fase inicial (repositorio público), idioma de la interfaz y documentación: castellano; identificadores de código: inglés.

## Gobernanza
La constitución prevalece sobre cualquier otro documento. Cambiarla exige justificación, número de versión y fecha en el propio archivo, y la aprobación del usuario.
