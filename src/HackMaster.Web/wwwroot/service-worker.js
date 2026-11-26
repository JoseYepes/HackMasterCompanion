// Trabajador de servicio de desarrollo (F013 FR-011): no guarda nada, así los cambios se ven al
// recargar. El que se publica es service-worker.published.js. No escucha «fetch»: uno vacío obliga al
// navegador a pasar cada petición por aquí y retrasa la recarga (lo vio la CI).
