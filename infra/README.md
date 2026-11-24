# Infraestructura de F006

Recursos de Azure de las cuentas y la sincronización (specs/010-cuentas-sincronizacion). Solo
servicios gratuitos permanentes o con cuota gratuita (constitución VII); las condiciones se
comprobaron el 2026-11-24 (research.md) y se vuelven a comprobar antes de cada despliegue.

| Archivo | Alcance | Qué crea |
|---|---|---|
| `budget.bicep` | suscripción | Presupuesto de 1 € al mes con avisos por correo (10 % real, 100 % previsto). Va primero. |
| `main.bicep` | grupo de recursos | Static Web App Free (aloja la API), Cosmos DB con nivel gratuito, base de datos `hackmaster` (1000 RU/s compartidos) y contenedores `userdata` e `invitations`. |
| `swa-placeholder/` | — | Página mínima de la Static Web App y su `staticwebapp.config.json` (API en .NET 10 aislado); la aplicación sigue en GitHub Pages. |

Todo recurso lleva la etiqueta `proyecto=hackmaster` (lo comprueba `InfraTests`).

Lo despliega `.github/workflows/azure.yml`, que entra en Azure con OIDC (sin contraseñas ni
claves). Necesita:

- Variables del repositorio (no son secretos): `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`,
  `AZURE_CLIENT_ID`, `AZURE_RESOURCE_GROUP`, `AUTH_AUTHORITY`, `AUTH_AUDIENCE`, `ALLOWED_ORIGINS` y
  `BUDGET_START_DATE` (primer día del mes en que empieza el presupuesto, AAAA-MM-01; no se cambia
  después).
- Secretos del repositorio, para que los correos no salgan en los registros públicos:
  `BUDGET_EMAIL` y `ADMIN_ACCOUNT_IDS` (los identificadores de las cuentas de administración; la
  aplicación muestra el de cada cuenta en el aviso de «no invitada»).

Mientras falte `AZURE_CLIENT_ID`, el flujo no hace nada.

Lo que no se puede describir con Bicep (inquilino de External ID, registros de las aplicaciones,
cliente de Google y credencial federada) se prepara una vez a mano: ver
`specs/010-cuentas-sincronizacion/quickstart.md`.

El flujo usa el entorno `azure` de GitHub: la credencial federada tiene que tener el sujeto
`repo:JoseYepes/HackMasterCompanion:environment:azure`.
