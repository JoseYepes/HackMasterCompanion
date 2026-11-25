# ADR 0001: Sincronización de los datos del jugador con su cuenta (F006)

- **Estado**: aceptada, 2026-11-24.
- **Contexto**: F006 guarda en la cuenta lo que hoy solo vive en el dispositivo, sin que la aplicación deje de funcionar sin sesión ni sin conexión (principio V).
- **Decisión**:
  1. Un documento por elemento (cada ficha, cada conjunto, el historial y el modo de pantalla), con la hora del cambio y la versión de la nube. Si un mismo documento cambia en dos sitios, se queda el más reciente y se avisa; si empatan, gana la nube.
  2. Todo se guarda primero en el dispositivo; la cola de cambios pendientes vive en el dispositivo, separada por cuenta.
  3. Una única abstracción nueva en la interfaz, `IAccountSync`, con dos casos reales: la API y el falso de los tests (principio III).
  4. Las reglas de fusión van en `HackMaster.Domain.Sync`, puras y con test.
- **Consecuencias**: dos cambios en fichas distintas nunca chocan. Si se edita la misma ficha en dos sitios sin conexión, se pierde la edición más antigua, con aviso. No se ve en directo lo que cambia otro dispositivo abierto a la vez (fuera del alcance de la spec).
