# Feature Specification: Cuentas y datos en la nube (F006)

**Feature Branch**: `010-cuentas-sincronizacion`

**Created**: 2026-11-24

**Status**: Draft

**Input**: User description: "F006 — Backend y cuentas. Como jugador, quiero tener mi propia cuenta e iniciar sesión para ver mis datos en cualquier dispositivo: fichas, historial de tiradas, configuración de los dados (F008), conjuntos propios y modo de pantalla. Aceptación: lo creado o cambiado en un dispositivo aparece en otro con la misma cuenta; dos personas en el mismo dispositivo ven cada una sus datos; sin sesión, la aplicación sigue funcionando con los datos del dispositivo, como hasta ahora; tests de integración; despliegue repetible por CI; solo servicios gratuitos permanentes o con cuota gratuita, con alerta de presupuesto; repositorio público sin secretos."

## Clarifications

### Session 2026-11-24

- Q: ¿Con qué cuentas se inicia sesión? → A: Microsoft (personal) y Google.
- Q: ¿Quién puede crear cuenta? → A: solo las personas que invite el administrador, por correo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Iniciar sesión y guardar en la cuenta (Priority: P1)

Como jugador, quiero iniciar sesión con mi cuenta para que lo que hago (fichas, conjuntos, tiradas,
aspecto de los dados, modo de pantalla) quede guardado en ella y no solo en este dispositivo.

**Why this priority**: sin cuenta no hay nada que sincronizar; es la base de todo lo demás.

**Independent Test**: iniciar sesión, crear una ficha, cerrar el navegador, borrar los datos del
sitio, volver a entrar e iniciar sesión: la ficha sigue ahí.

**Acceptance Scenarios**:

1. **Given** la aplicación sin sesión, **When** pulso «Iniciar sesión» y completo el acceso,
   **Then** vuelvo a la misma página con la sesión iniciada y veo mi nombre de cuenta en el menú.
2. **Given** la sesión iniciada, **When** creo o cambio una ficha, un conjunto, una tirada, el aspecto
   de los dados o el modo de pantalla, **Then** el cambio queda guardado en mi cuenta sin que tenga
   que hacer nada más.
3. **Given** la sesión iniciada, **When** pulso «Cerrar sesión», **Then** la aplicación deja de
   mostrar los datos de la cuenta y vuelve a los del dispositivo.

---

### User Story 2 - Mis datos en otro dispositivo (Priority: P1)

Como jugador, quiero abrir la aplicación en el móvil y encontrar lo que hice en el ordenador.

**Why this priority**: es el motivo de la funcionalidad (criterio de aceptación del producto).

**Independent Test**: con la misma cuenta en dos navegadores, crear un conjunto en uno y verlo en el
otro tras recargar.

**Acceptance Scenarios**:

1. **Given** la misma cuenta en dos dispositivos, **When** cambio algo en uno, **Then** aparece en el
   otro al abrir la aplicación o al recargar, en menos de 10 s.
2. **Given** un dispositivo sin conexión con la sesión iniciada, **When** cambio algo, **Then** el
   cambio se guarda en el dispositivo y se sube a la cuenta en cuanto vuelve la conexión.
3. **Given** el mismo elemento cambiado en dos dispositivos antes de sincronizar, **Then** se queda el
   cambio más reciente, y la aplicación avisa de que el otro se ha descartado.

---

### User Story 3 - Varias personas en el mismo dispositivo (Priority: P2)

Como grupo de juego que comparte una tableta, queremos que cada uno vea solo sus datos.

**Independent Test**: en el mismo navegador, iniciar sesión con la cuenta A y crear una ficha;
cerrar sesión e iniciar con la cuenta B: la ficha de A no aparece.

**Acceptance Scenarios**:

1. **Given** la cuenta A con sesión cerrada, **When** inicia sesión la cuenta B, **Then** B no ve
   ningún dato de A, ni siquiera durante la carga.
2. **Given** una cuenta, **Then** nadie más puede leer ni cambiar sus datos.

---

### User Story 4 - Llevar a la cuenta lo que ya tengo en el dispositivo (Priority: P2)

Como jugador que ya usa la aplicación, quiero que mis fichas y conjuntos actuales pasen a mi cuenta
la primera vez que inicio sesión.

**Acceptance Scenarios**:

1. **Given** datos en el dispositivo y una cuenta vacía, **When** inicio sesión por primera vez,
   **Then** la aplicación me ofrece subirlos a la cuenta y, si acepto, aparecen en ella.
2. **Given** datos en el dispositivo y una cuenta que ya tiene datos, **When** inicio sesión, **Then**
   puedo elegir añadir los del dispositivo a la cuenta o dejarlos solo en el dispositivo; nunca se
   borra nada sin preguntar.

---

### User Story 5 - Mis datos son míos (Priority: P3)

Como jugador, quiero poder descargar todo lo que hay en mi cuenta y borrar la cuenta con sus datos.

**Acceptance Scenarios**:

1. **Given** la sesión iniciada, **When** pido descargar mis datos, **Then** obtengo un archivo con
   todo lo guardado en la cuenta.
2. **Given** la sesión iniciada, **When** borro la cuenta y lo confirmo, **Then** sus datos
   desaparecen de la nube; los del dispositivo se quedan.

### Edge Cases

- Sin sesión: todo funciona como ahora, solo con los datos del dispositivo (principio V).
- Sesión caducada mientras se usa: los cambios se guardan en el dispositivo y se suben al volver a
  iniciar sesión; se avisa sin interrumpir.
- El servicio en la nube no responde o agota su cuota gratuita: la aplicación sigue funcionando con
  los datos del dispositivo y avisa de que no puede sincronizar.
- Datos en un formato antiguo (`SchemaVersion` anterior) en la cuenta o en el dispositivo: se
  migran como hoy al leerlos.
- Datos de la cuenta que no se pueden leer: no se borran; se avisa y se usan los del dispositivo.
- Los datos del juego (habilidades, conjuros, estilos) no dependen de la cuenta: siguen siendo
  públicos y funcionan sin sesión.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Botones «Iniciar sesión» y «Cerrar sesión» en el menú; con sesión, se ve el nombre de
  la cuenta.
- **FR-002**: Se inicia sesión con una cuenta de Microsoft (personal: Outlook, Hotmail, Xbox) o de
  Google. La aplicación no guarda contraseñas propias.
- **FR-003**: Solo pueden crear cuenta las personas que el administrador invite (por su dirección
  de correo). Quien inicie sesión sin invitación ve un aviso de que no tiene acceso y la aplicación
  sigue funcionando sin sesión. El administrador puede retirar una invitación.
- **FR-004**: Con sesión iniciada, se guardan en la cuenta las fichas, los conjuntos propios, el
  historial de tiradas, el aspecto de los dados y el modo de pantalla.
- **FR-005**: Cada cambio se guarda primero en el dispositivo y después en la cuenta; sin conexión,
  queda pendiente y se sube al volver.
- **FR-006**: Al abrir la aplicación o recargar con sesión, se traen los cambios de la cuenta.
- **FR-007**: Si un elemento se cambió en dos sitios, gana el cambio más reciente y se avisa.
- **FR-008**: Los datos de cada cuenta solo los lee y cambia esa cuenta; el servicio lo comprueba en
  cada petición.
- **FR-009**: Los datos del dispositivo de cada cuenta y los de sin sesión están separados: al
  cambiar de cuenta no se ve nada de la anterior.
- **FR-010**: Primera sesión con datos en el dispositivo: se ofrece subirlos (US4); nunca se borra
  nada sin preguntar.
- **FR-011**: Descargar todos los datos de la cuenta y borrar la cuenta con confirmación (US5).
- **FR-012**: Sin sesión o sin servicio, la aplicación funciona igual que antes de F006.
- **FR-013**: Solo servicios en la nube gratuitos permanentes o con cuota gratuita, con una alerta
  de presupuesto antes de crear nada (constitución VII).
- **FR-014**: La infraestructura se describe en el repositorio y se despliega con la integración
  continua, de forma repetible; el repositorio no contiene secretos.
- **FR-015**: Tests de integración del servicio (guardar, leer, aislamiento entre cuentas, conflictos).
- **FR-016**: Sin desbordamiento a 390 y 820 px en los dos modos en las pantallas nuevas.

### Key Entities

- **Cuenta**: identidad del jugador; nombre visible; fecha de alta.
- **Datos de la cuenta**: los mismos archivos que hoy se guardan en el dispositivo (fichas,
  conjuntos, historial y aspecto de los dados, modo de pantalla), cada uno con su `SchemaVersion` y
  la fecha de su último cambio.
- **Cambio pendiente**: lo guardado en el dispositivo que aún no ha llegado a la cuenta.

## Success Criteria *(mandatory)*

- **SC-001**: Un cambio hecho en un dispositivo se ve en otro con la misma cuenta en menos de 10 s
  tras recargar.
- **SC-002**: Iniciar sesión por primera vez y subir los datos del dispositivo lleva menos de 1 minuto.
- **SC-003**: En las pruebas de aislamiento, ninguna cuenta puede leer ni cambiar datos de otra.
- **SC-004**: Sin sesión, todas las pruebas de navegador actuales siguen pasando sin cambios.
- **SC-005**: El coste mensual en la nube es 0 € con el uso previsto, y hay una alerta de presupuesto
  activa.
- **SC-006**: Un despliegue desde cero se hace con un solo flujo de integración continua.

## Assumptions

- Uso previsto: un grupo de juego, decenas de cuentas como mucho; los límites de los niveles
  gratuitos bastan con mucho margen.
- No se comparten datos entre cuentas (por ejemplo, el DJ viendo las fichas del grupo); queda fuera
  de F006.
- El historial de tiradas se sincroniza con el mismo límite que tiene hoy en el dispositivo.
- La sincronización es al abrir, al recargar y tras cada cambio; no hace falta ver en directo lo que
  cambia otro dispositivo abierto a la vez.
- Los datos del juego siguen publicándose como hoy y no requieren sesión.
