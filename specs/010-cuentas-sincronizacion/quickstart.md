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
   - La cuenta de Microsoft se federa **solo** con el emisor de cuentas personales (`consumers`),
     nunca con `common` ni `organizations`. Si no, cualquiera podría crear en su propio inquilino
     un usuario con el correo de otra persona.
   - En «HackMaster API»: `accessTokenAcceptedVersion: 2` en el manifiesto (la API solo acepta
     tokens v2) y la notificación opcional `email` en el token de acceso.
5. Crear la credencial federada de GitHub Actions para el entorno `azure`, con el sujeto
   `repo:JoseYepes/HackMasterCompanion:environment:azure`. Se le da el rol de colaborador en la
   suscripción. Las variables y los secretos del repositorio están en `infra/README.md`; los
   correos van como secretos, porque los registros de este repositorio son públicos.
6. Lanzar el flujo `azure.yml`. Despliega el presupuesto, `infra/main.bicep` y la API.
7. Rellenar `src/HackMaster.Web/wwwroot/appsettings.json` con la autoridad, el cliente, el ámbito y
   la dirección de la API (`apiUrl` en la salida del despliegue). Son valores públicos. La
   aplicación «HackMaster Web» necesita la redirección `https://joseyepes.github.io/HackMasterCompanion/authentication/login-callback`
   y la de `http://localhost:5238/`.
8. Entrar con la cuenta de administración. Sale el aviso de «no invitada» con el identificador de
   la cuenta. Ponerlo en el secreto `ADMIN_ACCOUNT_IDS` y volver a lanzar `azure.yml`. Después,
   invitar a las demás desde «Mi cuenta» → «Invitaciones».

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
