# Contrato de interfaz (F006)

- **Cabecera**: «Iniciar sesión» sin sesión. Con sesión: el nombre de la cuenta, que lleva a
  «Mi cuenta», y «Cerrar sesión». El enlace a «Invitaciones» está en «Mi cuenta», solo para
  administradores, para que la cabecera quepa a 390 px. Sin inicio de sesión configurado, la
  cabecera es la de siempre.
- **Estado de sincronización**: un punto junto al nombre (título «Sincronizado», «Cambios
  pendientes» o «No se puede sincronizar») y, si no está todo sincronizado, una franja de aviso
  bajo la cabecera. Los conflictos se avisan en otra franja, con «Cerrar»: «Se ha quedado la
  versión más reciente de «{nombre}».».
- **Sin invitación**: «Esta cuenta no está invitada. Pide acceso a quien administra la aplicación.»
  y se cierra la sesión.
- **Primera sesión con datos en el dispositivo** (US4): una franja bajo la cabecera con «Subir a mi cuenta» y
  «Dejarlos solo en este dispositivo». Si la cuenta ya tiene datos, el texto lo dice y «Subir»
  añade sin sustituir: las fichas y los conjuntos con el mismo identificador se quedan con la
  versión más reciente.
- **Mi cuenta** (`/cuenta`): «Descargar mis datos» y «Borrar mi cuenta». Este último pide
  confirmación escribiendo «BORRAR».
- **Invitaciones** (`/invitaciones`): lista de correos con «Quitar» y un campo para «Invitar».
- Sin desbordamiento horizontal a 390 y 820 px en los modos normal y noche.
