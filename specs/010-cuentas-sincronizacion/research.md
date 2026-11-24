# Investigación: F006, cuentas y datos en la nube

Condiciones comprobadas el 2026-11-24 en la documentación oficial de Microsoft (fecha de
actualización de cada página entre paréntesis). Se vuelven a comprobar antes de crear recursos
(constitución VII).

## R1. Inicio de sesión con Microsoft y Google, solo con invitación

- **Decisión**: Microsoft Entra External ID, inquilino externo, con un flujo de usuario que admite
  Google y cuenta personal de Microsoft (por OpenID Connect). La aplicación usa MSAL para
  Blazor WebAssembly y pide un token de acceso para la API. La invitación la comprueba la API: el
  correo del token tiene que estar en la lista de invitaciones; si no, responde 403 y la
  aplicación avisa y sigue sin sesión.
- **Por qué**: es el único servicio de Microsoft que da Google y cuenta Microsoft con un solo
  emisor de tokens. Las primeras 50000 personas activas al mes son gratis, y no hay fecha de fin
  (página de precios de Entra External ID; preguntas frecuentes de External ID). Hay que vincular
  el inquilino a una suscripción para la facturación, pero sin uso por encima de la cuota no se
  cobra nada. Las funciones adicionales (SMS, M2M) se pagan y no se usan.
  Proveedores admitidos: tabla «Supported features» de External ID (2026-11-18).
- **Alternativas descartadas**:
  - Autenticación integrada de Static Web Apps (plan gratuito): solo GitHub y Microsoft Entra ID.
    Google necesita un proveedor propio, que solo existe en el plan Standard, de pago (páginas
    «Plans» y «Authentication», 2026-01-23).
  - MSAL contra Microsoft y Google por separado: dos emisores de tokens, dos flujos en el cliente
    y el doble de validación en la API.

## R2. API

- **Decisión**: Azure Functions *gestionadas* por Azure Static Web Apps, plan gratuito, .NET 10
  aislado (`apiRuntime: dotnet-isolated:10.0`). La interfaz sigue en GitHub Pages. La API
  responde con CORS solo al origen de Pages y a `localhost`; las cabeceras CORS las pone el
  código de la función.
- **Por qué**: las funciones gestionadas van incluidas en el plan gratuito y no necesitan cuenta
  de almacenamiento. Las Functions propias sí necesitan una, y el almacenamiento no tiene nivel
  gratuito permanente. Límites del plan gratuito: 100 GB de tráfico al mes sin excedente posible,
  250 MB por aplicación, solo disparadores HTTP y sin identidad gestionada
  («Plans» 2026-01-23, «API support» 2026-06-02). .NET 10 aislado: «Supported languages and
  runtimes». Dejar la interfaz en Pages evita cambiar de dirección. Cambiar de dirección haría
  perder los datos del dispositivo de quien no tenga sesión, porque el almacenamiento del
  navegador va por origen.
- **Riesgo (se comprueba con una prueba al principio, T00x)**: que Static Web Apps no deje pasar
  la cabecera `Authorization` o las peticiones `OPTIONS` hasta la función.
  - Plan B: enviar el token en una cabecera propia.
  - Plan C: Azure Container Apps con consumo, que tiene una cuota gratuita mensual permanente,
    para la misma API en contenedor. Su cuota se verificaría antes de usarlo.
- **Alternativas descartadas**:
  - Mover la interfaz a Static Web Apps: cambia de origen (ver arriba).
  - App Service F1: tiene límites de CPU diaria; no aporta frente a lo anterior.

## R3. Datos

- **Decisión**: Azure Cosmos DB for NoSQL con nivel gratuito. El rendimiento es de 1000 RU/s
  manuales, compartidos por la base de datos, y nunca autoescalado por encima de 1000.
  Contenedores `userdata` (clave de partición `/userId`) e `invitations` (clave `/email`).
- **Por qué**: los primeros 1000 RU/s y 25 GB de la cuenta son gratis durante toda la vida de la
  cuenta. Hay una cuenta con nivel gratuito por suscripción, y hay que activarlo al crearla. No
  sirve con *serverless* («Free tier», 2026-04-27). Los datos ya son documentos JSON con
  `SchemaVersion`.
- **Alternativas descartadas**: la oferta gratuita de Azure SQL, porque obligaría a pasar los JSON
  a tablas o a guardarlos como texto, sin ninguna ventaja.

## R4. Sincronización

- **Decisión**: un documento por elemento:
  - una ficha (por `Id`);
  - un conjunto propio (por `Slug`);
  - el historial y las opciones del tirador (uno);
  - el modo de pantalla (uno).

  Cada documento lleva `updatedAt`, que pone el cliente, y la etiqueta de versión de Cosmos
  (`_etag`). El cliente sube con la etiqueta que conoce. Si en la nube hay otra, la API responde
  409 con la versión de la nube. El cliente se queda con la más reciente por `updatedAt` y avisa
  si se descarta la suya o la de la nube (FR-007).
  - Los borrados son marcas (`deleted: true`), para que lleguen a los demás dispositivos.
  - Lo pendiente se guarda en el dispositivo y se reintenta al volver la conexión (FR-005).
  - La fusión es lógica pura y va en `HackMaster.Domain` (TDD).
- **Por qué**: un documento por ficha hace que cambiar una ficha en el móvil y otra en el
  ordenador no choque. Con la etiqueta de versión de Cosmos no hace falta reloj fiable para
  detectar el conflicto; la hora solo se usa para elegir cuál gana.

## R5. Separación de datos por cuenta en el dispositivo (FR-009)

- **Decisión**: con sesión, las claves del dispositivo llevan el identificador de la cuenta
  (`hackmaster.u.{oid}.characters`, etc.). Sin sesión se usan las de siempre. Al cerrar sesión no
  se borra nada, pero solo se ven las claves sin sesión.
- **Por qué**: cumple US3 sin borrar datos y sin depender de la red.

## R6. Infraestructura, despliegue y secretos

- **Decisión**:
  - Bicep en `/infra`, con la etiqueta `proyecto=hackmaster` en todo: Static Web App Free, cuenta
    Cosmos con `enableFreeTier: true`, base de datos y contenedores. Los ajustes de la API
    (punto de acceso y clave de Cosmos) se rellenan con `listKeys()` al desplegar, así que la
    clave nunca pasa por el repositorio.
  - Presupuesto de la suscripción de 1 € al mes, con avisos por correo al 10 % real y al 100 %
    previsto. Se crea con Bicep antes que el resto. Los presupuestos se evalúan cada 24 horas y no
    paran el consumo («Create budgets», 2026-11-18).
  - GitHub Actions entra en Azure con OIDC (credencial federada, sin secreto). Los
    identificadores de inquilino, suscripción y cliente van en variables del repositorio: no son
    secretos. El token de despliegue de Static Web Apps se obtiene en cada ejecución.
  - El inquilino de External ID, sus flujos y el cliente OAuth de Google se crean a mano, una sola
    vez, siguiendo `quickstart.md`. No se pueden describir por completo con Bicep.
- **Configuración pública del cliente**: autoridad, identificador de cliente, ámbito y dirección de
  la API van en `wwwroot/appsettings.json`. No son secretos.

## R7. Tests

- **Decisión**:
  - Dominio: xUnit con TDD (fusión, conflictos, marcas de borrado, claves por cuenta).
  - API: tests de integración contra el emulador de Cosmos en Docker (`vnext`), también en CI como
    contenedor de servicio. Los tokens se firman con una clave de prueba generada en el test.
  - Web: bUnit con un `AuthenticationStateProvider` falso.
  - Playwright: todas las pruebas actuales sin sesión (SC-004) más las pantallas nuevas a 390 y
    820 px.
  - El inicio de sesión real con Google y Microsoft lo comprueba el usuario a mano.
