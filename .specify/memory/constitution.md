# Constitución de HackMaster Companion

## Principios fundamentales

### I. Contenido sin inventar

Los datos y las descripciones del juego proceden de las fuentes (libros, HackJournals, erratas,
Excel del usuario) y NO DEBEN inventarse. Lo que falte DEBE marcarse como "pendiente de
verificar". La columna `ManualText` es local del usuario y NO DEBE versionarse ni publicarse.

### II. Prioridad de fuentes de datos

El orden de prioridad es: manuales extendidos (CG, GrG, SSG, ZG, PFG, KODT…) > Manual del
Jugador > Excel del usuario; y castellano > inglés. Si dos fuentes discrepan, la duda DEBE
registrarse en `docs/dudas.md`; no se decide en silencio. Lo que falte DEBE marcarse como
"pendiente de verificar" y NO DEBE inventarse.

### III. Dominio puro y arquitectura mínima

`HackMaster.Domain` NO DEBE depender de Blazor, JS interop, almacenamiento, HTTP ni
infraestructura, y DEBE ser determinista (el azar entra por `IRandom`). Hay dos proyectos de
producción (`Domain`, `Web`) más `tools/DataImport` hasta la fase de backend. NO DEBEN crearse
capas, repositorios genéricos, MediatR, mapeadores ni interfaces "por si acaso": una abstracción
se añade cuando existe un segundo caso de uso real, con un ADR corto. SOLID es una guía, no un
ritual.

### IV. Test primero (innegociable)

En el dominio se aplica TDD: los tests DEBEN escribirse primero y verse fallar. Hay un test por
regla, con un nombre que indique la regla y su fuente. Un test NO DEBE borrarse ni debilitarse
para que pase. La cobertura orientativa en `Domain` es ≥ 90 %.

### V. Local primero

Todo DEBE funcionar sin backend ni conexión hasta la fase de Azure. Los datos del juego se leen
de JSON validado generado desde el Excel; la aplicación NO DEBE leer el Excel. Los esquemas
persistidos DEBEN llevar `SchemaVersion` y migraciones con test.

### VI. Entrega verificable

Cada objetivo DEBE tener un criterio medible y comandos de verificación. Los bucles de
implementación tienen un tope de 8 iteraciones; si no se alcanza el objetivo, se para y se
informa. Commits pequeños, rama por funcionalidad, pull request siempre, sin push a `main`, y
revisión independiente antes de fusionar.

### VII. Coste y seguridad

Solo DEBEN usarse servicios de Azure gratuitos permanentes o con cuota gratuita (el usuario no
dispone de crédito de bienvenida ni de servicios gratuitos de 12 meses). Antes de crear recursos
se DEBE confirmar con el usuario, verificar las condiciones vigentes en la documentación oficial
y tener una alerta de presupuesto. El repositorio es público y NO DEBE contener secretos.

## Restricciones técnicas

- .NET LTS vigente, C#, Blazor WebAssembly como PWA.
- Tests con xUnit, bUnit y Playwright.
- GitHub Pages para la fase inicial (repositorio público).
- Interfaz y documentación en castellano; identificadores de código en inglés.

## Gobernanza

La constitución prevalece sobre cualquier otro documento del proyecto. Cambiarla exige
justificación, número de versión y fecha en este mismo archivo, y la aprobación del usuario. El
número de versión sigue el versionado semántico: MAYOR para eliminar o redefinir principios,
MENOR para añadir principios o secciones, PARCHE para aclaraciones de redacción. El cumplimiento
se comprueba en la revisión independiente previa a cada fusión (principio VI).

**Versión**: 1.0.0 | **Ratificada**: 2026-11-16 | **Última enmienda**: 2026-11-16
