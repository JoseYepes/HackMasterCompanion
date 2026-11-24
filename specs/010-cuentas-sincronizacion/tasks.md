# Tasks: Cuentas y datos en la nube (F006)

**Input**: plan.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: obligatorios (principio IV).
- Cada test se escribe antes que el código y se ve fallar.
- Los nombres siguen `Regla__Fuente`, donde la fuente es el requisito de la spec (p. ej. `__F006_FR007`).

## Phase 1: Preparación

- [ ] T001 Crear `src/HackMaster.Api` (Functions aislado, `net10.0`) y `tests/HackMaster.Api.Tests`; añadirlos a la solución y fijar versiones en `Directory.Packages.props` (paquetes de plan.md).
- [ ] T002 Añadir `src/HackMaster.Api/staticwebapp.config.json` con `"platform": { "apiRuntime": "dotnet-isolated:10.0" }`.
- [ ] T003 Escribir `docs/adr/0002-sincronizacion.md`: por qué `IAccountSync`, un documento por elemento y que gane el más reciente.

## Phase 2: Base (bloquea todo lo demás)

- [ ] T004 Prueba de viabilidad (research.md R2):
  - una función `GET /api/ping` que devuelve la cabecera `Authorization` recibida y responde a `OPTIONS` con CORS;
  - desplegarla en una Static Web App Free de prueba **solo tras confirmarlo con el usuario**, y llamarla desde `http://localhost`;
  - anotar el resultado en research.md; si falla, aplicar el plan B o C antes de seguir.
- [ ] T005 [P] Tests de `AccountKeys` en `tests/HackMaster.Domain.Tests/Sync/AccountKeysTests.cs`:
  - sin sesión, las claves de siempre;
  - con sesión, `hackmaster.u.{oid}.{clave}`;
  - un `oid` vacío o con caracteres no válidos se rechaza.
- [ ] T006 [P] Tests de `SyncMerge` en `tests/HackMaster.Domain.Tests/Sync/SyncMergeTests.cs`, con las cuatro reglas de data-model.md:
  - gana el `updatedAt` mayor y, en empate, la nube;
  - las marcas de borrado;
  - una versión posterior se ignora con aviso;
  - el aviso lleva el nombre del elemento descartado.
- [ ] T007 [P] Tests de `SyncState` en `tests/HackMaster.Domain.Tests/Sync/SyncStateTests.cs`:
  - un solo pendiente por `id`;
  - lectura y escritura con `schemaVersion` 1;
  - los archivos ilegibles no se borran.
- [ ] T008 Implementar `SyncDoc`, `SyncState`, `SyncMerge` y `AccountKeys` en `src/HackMaster.Domain/Sync/`, hasta que pasen T005–T007.
- [ ] T009 Tests de `TokenValidator` en `tests/HackMaster.Api.Tests/TokenValidatorTests.cs`, con una clave de firma de prueba:
  - emisor, audiencia, caducidad y firma;
  - extracción de `oid` y del correo.
- [ ] T010 Implementar `src/HackMaster.Api/Auth/TokenValidator.cs` (con los metadatos OIDC de la autoridad configurada) y `Cors.cs` (solo el origen de Pages y `localhost`).
- [ ] T011 Crear `src/HackMaster.Api/Storage/CosmosStore.cs` y un *fixture* de tests que use el emulador de Cosmos (`COSMOS_EMULATOR_ENDPOINT`) y que se salte con un mensaje claro si no está.

## Phase 3: US1 + US3, iniciar sesión, guardar en la cuenta y que cada uno vea lo suyo (P1/P2)

**Independent Test**: iniciar sesión, crear una ficha, borrar los datos del sitio y volver a iniciar sesión: la ficha sigue ahí. Con otra cuenta no se ve.

- [ ] T012 [P] [US1] Tests de integración en `tests/HackMaster.Api.Tests/DataFunctionsTests.cs`:
  - `GET /api/me`;
  - `PUT` y `GET /api/data`: crear con `If-None-Match: *` y actualizar con `If-Match`;
  - 409 con la versión de la nube;
  - 413 por encima de 2 MB;
  - 401 sin token.
- [ ] T013 [P] [US3] Tests de aislamiento en `tests/HackMaster.Api.Tests/IsolationTests.cs` (SC-003, FR-008):
  - la cuenta A no lee ni cambia los documentos de B, aunque mande su `id`;
  - el `userId` del cuerpo se ignora.
- [ ] T014 [P] [US1] Tests de invitación en `tests/HackMaster.Api.Tests/InvitationTests.cs`:
  - un correo no invitado recibe 403 `not-invited` en todas las rutas de datos;
  - el correo se compara en minúsculas.
- [ ] T015 [US1] Implementar `MeFunction` y `DataFunctions` en `src/HackMaster.Api/Functions/`, hasta que pasen T012–T014. Si Cosmos falla, responder 503 `unavailable`.
- [ ] T016 [US1] Configurar MSAL en `src/HackMaster.Web/Program.cs` y `wwwroot/appsettings.json`: autoridad, cliente, ámbito y dirección de la API, todos públicos. Sin configuración, la aplicación funciona sin el botón de sesión.
- [ ] T017 [P] [US1] Tests bUnit en `tests/HackMaster.Web.Tests/Account/LoginDisplayTests.cs`, con un `AuthenticationStateProvider` falso: los botones y el nombre (FR-001) y el aviso de no invitado (contracts/ui.md).
- [ ] T018 [US1] Poner `src/HackMaster.Web/Account/LoginDisplay.razor` en el menú, hasta que pase T017.
- [ ] T019 [P] [US3] Tests bUnit de las claves por cuenta (FR-009):
  - con sesión, `PlayerCharacters`, `PlayerSuites`, el historial y el modo de pantalla leen y escriben en las claves de `AccountKeys`;
  - al cambiar de cuenta no se ve nada de la anterior, ni durante la carga.
- [ ] T020 [US3] Aplicar las claves por cuenta en `PlayerCharacters.cs`, `PlayerSuites.cs`, `RollHistoryUpdate.cs` y el script del modo de `wwwroot/index.html`, hasta que pase T019.
- [ ] T021 [US1] Crear `IAccountSync` y `AccountSync.cs`, que suben cada cambio después de guardarlo en el dispositivo (FR-004, FR-005), con su falso para los tests.

## Phase 4: US2, mis datos en otro dispositivo (P1)

**Independent Test**: con la misma cuenta en dos navegadores, un conjunto creado en uno aparece en el otro tras recargar.

- [ ] T022 [P] [US2] Tests bUnit en `tests/HackMaster.Web.Tests/Account/AccountSyncTests.cs`:
  - al abrir y al recargar se traen los cambios (`since`);
  - sin conexión, el cambio queda pendiente y se sube al volver;
  - un 409 aplica `SyncMerge` y muestra el aviso;
  - con 503 o 401 aparece el estado «No se puede sincronizar» y no se pierde nada.
- [ ] T023 [US2] Completar `AccountSync.cs` y el estado de sincronización de contracts/ui.md, hasta que pase T022.

## Phase 5: US4, llevar a la cuenta lo que hay en el dispositivo (P2)

- [ ] T024 [P] [US4] Tests bUnit del diálogo de primera sesión (FR-010):
  - con la cuenta vacía, se ofrece subir;
  - con datos en la cuenta, «Subir» añade sin sustituir, y gana el más reciente por `Id` o `Slug`;
  - «Dejarlos» no toca nada;
  - nunca se borra nada.
- [ ] T025 [US4] Implementar el diálogo en `src/HackMaster.Web/Account/`, hasta que pase T024.

## Phase 6: US5, mis datos son míos (P3)

- [ ] T026 [P] [US5] Tests de integración de `GET /api/export` y `DELETE /api/account`: solo se borran los datos de la cuenta y el resto sigue.
- [ ] T027 [US5] Implementar `ExportFunction` y `AccountFunction`, hasta que pase T026.
- [ ] T028 [P] [US5] Tests bUnit de `Pages/Account.razor`:
  - descargar los datos;
  - borrar solo tras escribir «BORRAR»;
  - los datos del dispositivo se quedan.
- [ ] T029 [US5] Implementar `src/HackMaster.Web/Pages/Account.razor` (`/cuenta`), hasta que pase T028.

## Phase 7: Invitaciones (FR-003)

- [ ] T030 [P] Tests de integración de `/api/invitations`:
  - solo para los correos de `ADMIN_EMAILS`;
  - invitar, listar y quitar;
  - quien pierde la invitación recibe 403 en la siguiente petición.
- [ ] T031 Implementar `InvitationFunctions`, hasta que pase T030.
- [ ] T032 [P] Tests bUnit de `Pages/Invitations.razor`; el enlace del menú solo aparece para administradores.
- [ ] T033 Implementar `src/HackMaster.Web/Pages/Invitations.razor` (`/invitaciones`), hasta que pase T032.

## Phase 8: Infraestructura y CI (FR-013, FR-014)

- [ ] T034 Crear `infra/budget.bicep` a nivel de suscripción:
  - presupuesto de 1 € al mes;
  - avisos al 10 % real y al 100 % previsto;
  - el correo como parámetro, no en el repositorio.
- [ ] T035 Crear `infra/main.bicep` e `infra/README.md`:
  - Static Web App `Free`;
  - Cosmos con `enableFreeTier: true`;
  - base de datos con 1000 RU/s manuales compartidos;
  - contenedores `userdata` e `invitations`;
  - ajustes de la API con `listKeys()`;
  - la etiqueta `proyecto=hackmaster` en todo.
- [ ] T036 En las PR, compilar el Bicep y ejecutar `what-if` en CI, sin desplegar. Añadir un test que falle si algún recurso de `infra/` no lleva la etiqueta `proyecto=hackmaster` o si la cuenta de Cosmos no tiene el nivel gratuito.
- [ ] T037 Crear `.github/workflows/azure.yml`, sin secretos en el repositorio. En `main`, por este orden:
  1. entrar en Azure con OIDC;
  2. desplegar `budget.bicep`;
  3. desplegar `main.bicep`;
  4. desplegar la API, con el token de despliegue obtenido en la ejecución.
- [ ] T038 Añadir el emulador de Cosmos como contenedor de servicio en `.github/workflows/ci.yml`, para `HackMaster.Api.Tests`.

## Phase 9: Pulido y entrega

- [ ] T039 [P] Playwright, con el servicio de cuentas falso:
  - las pruebas actuales sin sesión siguen pasando (SC-004);
  - `/cuenta` e `/invitaciones` no se desbordan a 390 ni a 820 px en los dos modos (FR-016).
- [ ] T040 Despliegue real, **tras confirmarlo con el usuario**:
  1. comprobar de nuevo las condiciones (research.md);
  2. crear el presupuesto primero;
  3. hacer la preparación única de quickstart.md.
- [ ] T041 Comprobación a mano del usuario (quickstart.md, «Comprobación a mano»); el coste del mes debe ser 0 € (SC-005).
- [ ] T042 Actualizar `docs/ESTADO.md` y `docs/PRODUCTO.md`: F006 hecho y la dirección de la API.

## Dependencies & Execution Order

- La fase 1 va antes que la fase 2. Dentro de la fase 2, T004 decide si hace falta el plan B o el C antes de T010–T015.
- Las fases 3 (US1 + US3), 4 (US2) y 5 (US4) van en ese orden. Las fases 6 y 7 dependen solo de la fase 3.
- La fase 8 se puede hacer en paralelo con las fases 4 a 7.
- T040 y T041 van al final y necesitan al usuario.

## Parallel Opportunities

- T005, T006 y T007: tests del dominio en archivos distintos.
- T012, T013 y T014: tests de la API.
- T026–T033 (US5 e invitaciones), una vez terminada la fase 3.

## Implementation Strategy

- **MVP**: fases 1 a 4, con las que ya se inicia sesión, se guarda en la cuenta y se ve en otro dispositivo.
- Después, US4, US5 e invitaciones.
- Al final, el despliegue real, que necesita al usuario.
