# Contrato de interfaz (F006)

- **Menú**: «Iniciar sesión» sin sesión. Con sesión: nombre de la cuenta, «Cerrar sesión», «Mi
  cuenta» y, si es administrador, «Invitaciones».
- **Estado de sincronización**, junto al nombre: «Sincronizado», «Cambios pendientes» (sin
  conexión) o «No se puede sincronizar» (servicio caído). Los conflictos se avisan con el aviso
  temporal que ya usa la aplicación: «Se ha quedado la versión más reciente de «{nombre}»».
- **Sin invitación**: «Esta cuenta no está invitada. Pide acceso a quien administra la aplicación.»
  y se cierra la sesión.
- **Primera sesión con datos en el dispositivo** (US4): un diálogo con «Subir a mi cuenta» y
  «Dejarlos solo en este dispositivo». Si la cuenta ya tiene datos, el texto lo dice y «Subir»
  añade sin sustituir: las fichas y los conjuntos con el mismo identificador se quedan con la
  versión más reciente.
- **Mi cuenta** (`/cuenta`): «Descargar mis datos» y «Borrar mi cuenta». Este último pide
  confirmación escribiendo «BORRAR».
- **Invitaciones** (`/invitaciones`): lista de correos con «Quitar» y un campo para «Invitar».
- Sin desbordamiento horizontal a 390 y 820 px en los modos normal y noche.
