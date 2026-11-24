# Guía de comprobación: F006

## Preparación única (con el usuario; no la hace el CI)

1. Comprobar de nuevo las condiciones gratuitas (research.md) y crear el presupuesto:
   `infra/budget.bicep`, a nivel de suscripción, antes que nada.
2. Crear el inquilino externo de Entra External ID y vincularlo a la suscripción.
3. Registrar dos aplicaciones en él:
   - «HackMaster API», que expone el ámbito `data`;
   - «HackMaster Web», una SPA con las direcciones de redirección de Pages y de `localhost`.
4. Crear un cliente OAuth de Google (consola de Google Cloud, sin facturación). Añadir Google y
   «cuenta de Microsoft» como proveedores del flujo de registro e inicio de sesión.
5. Crear la credencial federada de GitHub Actions y las variables del repositorio
   `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` y `AZURE_CLIENT_ID`. Ningún secreto.
6. Lanzar el flujo `azure.yml`: despliega `infra/main.bicep` y la API.
7. Poner el correo del administrador en `ADMIN_EMAILS` y la primera invitación.

## Comprobaciones automáticas

```
dotnet build -warnaserror
dotnet test --no-build                       # dominio, Web y API
docker run -d -p 8081:8081 mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview
dotnet test tests/HackMaster.Api.Tests       # integración contra el emulador
```

Resultados esperados:
- Aislamiento: el token de la cuenta A nunca lee ni cambia los documentos de B (SC-003).
- Conflicto: devuelve 409 y gana el `updatedAt` mayor.
- Sin invitación: devuelve 403.
- E2E (`HACKMASTER_E2E_URL`): las pruebas actuales pasan sin sesión (SC-004).

## Comprobación a mano (usuario, en el móvil y el ordenador)

1. Iniciar sesión con Google en el ordenador, crear una ficha; en el móvil, iniciar sesión con la
   misma cuenta y recargar: la ficha aparece en menos de 10 s (SC-001).
2. Modo avión en el móvil, cambiar la ficha, quitar el modo avión: el cambio llega al ordenador.
3. Cerrar sesión e iniciar con otra cuenta (Microsoft): no se ve la ficha anterior.
4. Iniciar sesión con una cuenta no invitada: aviso, y la aplicación sigue sin sesión.
5. En el portal de Azure, Cost Management: el coste del mes es 0 € y el presupuesto existe.
