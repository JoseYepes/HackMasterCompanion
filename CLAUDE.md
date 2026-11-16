# CLAUDE.md — HackMaster Companion

Proyecto con **GitHub Spec Kit** y **Claude Code**. Antes de cualquier tarea lee `.specify/memory/constitution.md` (principios, prevalecen sobre todo lo demás) y `docs/PRODUCTO.md`. Si algo no está claro o se contradice, **pregunta**; no improvises.

## Proyecto en una línea
PWA en .NET (Blazor WebAssembly) para consultar habilidades de HackMaster 4e en castellano, crear conjuntos, tirar dados (2D/3D) y gestionar fichas de PJ. Local primero; backend en Azure solo en F006.

## Idioma
Interfaz, documentación, commits y comentarios de dominio en **castellano**. Identificadores de código en **inglés**. Nombres de habilidades en castellano; el original inglés entre paréntesis solo cuando lo traduje yo. Precios sin separador de miles. Libros como acrónimo + página (`MJ` = Manual del Jugador en castellano).

## Política de contenido
El contenido no se inventa: los datos y descripciones proceden de las fuentes (manuales, HackJournals, erratas, Excel). `ManualText` es local, nunca se versiona. Si falta información de un manual, marca "pendiente de verificar" y regístralo en `docs/dudas.md`; no lo inventes.

## Comandos
```
dotnet restore
dotnet build -warnaserror
dotnet test --no-build
dotnet format --verify-no-changes
dotnet run --project tools/DataImport -- data/Hackhabilidades_completo.xlsx data/
dotnet run --project src/HackMaster.Web
```
Los tests de interfaz (Playwright) se documentan en `tests/README.md`.

## Flujo con Spec Kit (una funcionalidad = un ciclo)
1. `/speckit.specify` con la funcionalidad de `docs/PRODUCTO.md` (qué y por qué, sin tecnología).
2. `/speckit.clarify`: resuelve ambigüedades conmigo y actualiza la spec.
3. `/speckit.plan`: decisiones técnicas según la parte B de `docs/PRODUCTO.md` y la constitución.
4. `/speckit.tasks`: tareas pequeñas, con los tests **antes** del código.
5. `/speckit.analyze`: comprueba coherencia spec ↔ plan ↔ tareas ↔ constitución. No sigas si hay incoherencias.
6. `/speckit.implement` **en bucles**: por cada grupo de tareas define
   - *Objetivo*: estado final verificable,
   - *Verificación*: comandos exactos,
   - *Restricciones*: qué no se toca (datos fuente, tests existentes, versiones fijadas),
   - *Límite*: 8 iteraciones; si no se alcanza, para y resume qué falla y qué probaste.
   Ciclo: evaluar estado → siguiente paso → ejecutar → verificar → repetir o terminar.
7. **Revisión**: revisa tu diff buscando regresiones, tests débiles y violaciones de la constitución. Pide una revisión independiente (subagente revisor con contexto limpio o persona) antes de fusionar.
8. **Entrega**: rama por funcionalidad, commits pequeños, pull request.

Si cambia un requisito, actualiza primero `spec.md` y regenera plan y tareas; no corrijas solo el código.

## Subagentes (usar con criterio)
Un agente principal por defecto. Lanza subagentes solo para (a) revisión independiente de un diff, (b) tareas independientes y paralelizables sin archivos compartidos, o (c) exploraciones que llenarían el contexto de ruido. No crees un agente por capa.

## Tests
xUnit en `Domain.Tests`, bUnit en `Web.Tests`, Playwright para flujos críticos (390 px y 820 px, sin desbordamiento horizontal). Nombre del test = regla + fuente: `Suite_Price_Is_Sum_Times_1_1_Rounded__HJ405`. Nunca borres ni debilites un test; si es incorrecto, explícalo en el commit.

## Git
Ramas `feat/…`, `fix/…`, `docs/…`, `chore/…` (Spec Kit puede crear ramas numeradas por funcionalidad; acéptalo). Commits en castellano, imperativo, con alcance: `feat(domain): calcular precio de conjuntos`. **Nunca push a `main` ni push forzado**; siempre pull request. Sin secretos en el repositorio (es público): `dotnet user-secrets` y variables de entorno.

## Azure (F006)
Solo servicios gratuitos permanentes o con cuota gratuita; sin crédito de bienvenida ni servicios de 12 meses. Antes de crear nada: confirmar conmigo, verificar condiciones vigentes en la documentación oficial y comprobar la alerta de presupuesto. Bicep en `/infra`, etiqueta `proyecto=hackmaster` en todo recurso.

## Límites
No instales paquetes sin justificarlos; fija versiones. No edites `data/` a mano: se regenera con `DataImport`. No amplíes el alcance, ni cambies dependencias o arquitectura sin consultarme. Un párrafo de pregunta vale más que una hora de código en la dirección equivocada.
