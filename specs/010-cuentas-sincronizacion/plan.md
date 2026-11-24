# Implementation Plan: Cuentas y datos en la nube (F006)

**Branch**: `010-cuentas-sincronizacion` | **Date**: 2026-11-24 | **Spec**: [spec.md](spec.md)

## Summary

El jugador inicia sesión con Microsoft o Google, si el administrador lo ha invitado, y sus fichas,
conjuntos, tiradas, aspecto de los dados y modo de pantalla se guardan en su cuenta y llegan a sus
otros dispositivos.

- **Interfaz**: sigue en GitHub Pages.
- **Inicio de sesión**: Entra External ID, con MSAL en el cliente.
- **API**: funciones gestionadas de Static Web Apps (plan gratuito).
- **Datos**: Cosmos DB con nivel gratuito.
- **Fusión**: los cambios se guardan primero en el dispositivo y se suben después. Al fusionar se
  queda la versión más reciente, por documento.

Detalle en [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (LTS); API en Azure Functions 4.x, modelo aislado.

**Primary Dependencies** (nuevas, versiones fijadas en `Directory.Packages.props`):
- `Microsoft.Authentication.WebAssembly.Msal` (Web): inicio de sesión y tokens.
- `Microsoft.Azure.Functions.Worker`, `.Sdk` y `.Extensions.Http` (API).
- `Microsoft.Azure.Cosmos` (API).
- `Microsoft.IdentityModel.JsonWebTokens` y `Microsoft.IdentityModel.Protocols.OpenIdConnect` (API):
  validación del token.
- Ninguna en `Domain`.

**Storage**: Cosmos DB for NoSQL (nube); IndexedDB y localStorage (dispositivo, como hoy).

**Testing**:
- xUnit (dominio y API con emulador de Cosmos);
- bUnit (Web);
- Playwright (390 y 820 px).

**Target Platform**: navegador (PWA en GitHub Pages) y Azure Static Web Apps Free.

**Project Type**: aplicación web con API.

**Performance Goals**: un cambio llega a otro dispositivo en menos de 10 s tras recargar (SC-001).

**Constraints**:
- 0 € al mes; todo funciona sin sesión y sin conexión (principio V).
- Sin secretos en el repositorio.
- Funciones gestionadas: solo HTTP y sin identidad gestionada.

**Scale/Scope**: un grupo de juego, decenas de cuentas. Las cuotas gratuitas (50000 personas
activas, 1000 RU/s, 25 GB, 100 GB de tráfico) sobran.

## Constitution Check

| Principio | Cumple | Cómo |
|---|---|---|
| I–II Contenido | Sí | No se sube contenido de manuales; solo los datos del jugador. |
| III Arquitectura mínima | Sí, con nota | Un proyecto nuevo de producción, `HackMaster.Api`. La constitución permite más proyectos en la fase de backend. Una sola abstracción nueva en Web, `IAccountSync`, con dos casos reales: la API y el falso de los tests. Se registra en un ADR corto. |
| IV Test primero | Sí | `SyncMerge` y las claves por cuenta con TDD en `Domain`; la API con tests de integración antes del código. |
| V Local primero | Sí | Sin sesión, nada cambia. Con sesión, todo se guarda primero en el dispositivo. `SyncState` y `Doc` llevan `schemaVersion` y migraciones con test. |
| VI Entrega verificable | Sí | quickstart.md; bucles de 8 iteraciones; PR con revisión independiente. |
| VII Coste y seguridad | Sí | Solo servicios con cuota gratuita permanente, verificados el 2026-11-24. Presupuesto creado antes que el resto. Antes de crear recursos se confirma con el usuario. Despliegue con OIDC; la clave de Cosmos solo vive en Azure. |

Revisado tras el diseño: sin cambios.

## Project Structure

### Documentation

```text
specs/010-cuentas-sincronizacion/
├── plan.md, research.md, data-model.md, quickstart.md
├── contracts/api.md, contracts/ui.md
└── tasks.md   (lo genera /speckit-tasks)
```

### Source Code

```text
src/HackMaster.Domain/
  Sync/SyncDoc.cs, SyncState.cs, SyncMerge.cs, AccountKeys.cs   # puro, con TDD
src/HackMaster.Web/
  Account/   AccountSync.cs (IAccountSync), SyncStatus, LoginDisplay.razor
  Pages/     Account.razor (/cuenta), Invitations.razor (/invitaciones)
  wwwroot/appsettings.json                                      # configuración pública
  PlayerCharacters.cs, PlayerSuites.cs, RollHistoryUpdate.cs     # claves por cuenta y aviso de cambio
src/HackMaster.Api/                                              # nuevo
  Functions/ MeFunction.cs, DataFunctions.cs, ExportFunction.cs, AccountFunction.cs, InvitationFunctions.cs
  Auth/      TokenValidator.cs, Cors.cs
  Storage/   CosmosStore.cs
  staticwebapp.config.json (apiRuntime dotnet-isolated:10.0)
tests/HackMaster.Domain.Tests/Sync/
tests/HackMaster.Web.Tests/Account/
tests/HackMaster.Api.Tests/                                      # nuevo, integración con emulador
infra/
  budget.bicep        # suscripción: presupuesto 1 €, avisos
  main.bicep          # grupo de recursos: Static Web App Free, Cosmos (free tier), contenedores
  README.md
.github/workflows/azure.yml                                      # infraestructura + API
docs/adr/0001-sincronizacion.md
```

**Structure Decision**:
- La interfaz no cambia de proyecto ni de dirección.
- La API es un proyecto nuevo que despliega Static Web Apps.
- La lógica de fusión va en `Domain`, para probarla sin red.

## Fases de implementación (resumen para /speckit-tasks)

0. **Prueba de viabilidad**: una función mínima en Static Web Apps Free, llamada desde otro origen
   con `Authorization` y `OPTIONS`. Si falla, se aplican los planes B o C de research.md R2 antes
   de seguir.
1. **Dominio**: `SyncMerge`, `SyncState` y `AccountKeys`, con TDD.
2. **API**: validación del token, invitaciones, datos, exportar y borrar, con integración contra el
   emulador.
3. **Web**:
   - US1: iniciar y cerrar sesión, y guardar.
   - US3: claves por cuenta.
   - US2: sincronización, pendientes y avisos.
   - US4: subir los datos del dispositivo.
   - US5: Mi cuenta.
   - Invitaciones.
4. **Infraestructura y CI**: Bicep, `azure.yml` y emulador en `ci.yml`.
5. **Despliegue real**: solo tras confirmarlo con el usuario. Presupuesto primero, después la
   preparación única de quickstart.md. Por último, la comprobación a mano.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Tercer proyecto de producción (`HackMaster.Api`) | La fase de backend lo prevé (principio III) | Sin API no hay datos compartidos entre dispositivos |
