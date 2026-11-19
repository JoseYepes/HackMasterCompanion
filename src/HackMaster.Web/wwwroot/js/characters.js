// Fichas de PJ (F004): guardado en IndexedDB y descarga del archivo de una ficha.
// Contrato: specs/007-fichas-pj/contracts/character-file.md. Sin dependencias.

const DATABASE = 'hackmaster';
const STORE = 'files';

let opening = null;

/** Abre (y crea la primera vez) la base del dispositivo; se reutiliza mientras la página siga abierta. */
function open() {
  opening ??= new Promise((resolve, reject) => {
    const request = indexedDB.open(DATABASE, 1);
    request.onupgradeneeded = () => request.result.createObjectStore(STORE);
    request.onsuccess = () => {
      const database = request.result;
      // Si el navegador cierra la conexión (datos del sitio borrados, otra versión de la base),
      // la siguiente operación vuelve a abrirla.
      database.onversionchange = () => {
        database.close();
        opening = null;
      };
      database.onclose = () => {
        opening = null;
      };
      resolve(database);
    };
    request.onerror = () => reject(request.error);
    request.onblocked = () => reject(new Error('La base de datos del dispositivo está bloqueada por otra pestaña.'));
  }).catch(error => {
    opening = null;
    throw error;
  });
  return opening;
}

/**
 * Ejecuta una petición sobre el almacén y espera a que termine la transacción. Si la conexión ya
 * estaba cerrada (InvalidStateError), la vuelve a abrir una vez.
 */
async function run(mode, action) {
  try {
    return await runOnce(mode, action);
  } catch (error) {
    if (error?.name !== 'InvalidStateError') {
      throw error;
    }

    opening = null;
    return runOnce(mode, action);
  }
}

async function runOnce(mode, action) {
  const database = await open();
  return new Promise((resolve, reject) => {
    const transaction = database.transaction(STORE, mode);
    const request = action(transaction.objectStore(STORE));
    transaction.oncomplete = () => resolve(request.result);
    transaction.onerror = () => reject(transaction.error);
    transaction.onabort = () => reject(transaction.error ?? new Error('No se pudo guardar.'));
  });
}

/** Texto guardado con esa clave, o null. */
export async function get(key) {
  const value = await run('readonly', store => store.get(key));
  return typeof value === 'string' ? value : null;
}

/** Guarda el texto con esa clave; si el navegador no deja, la promesa falla. */
export async function set(key, value) {
  await run('readwrite', store => store.put(value, key));
}

/** Descarga el texto como un archivo JSON con ese nombre. */
export function download(fileName, text) {
  const url = URL.createObjectURL(new Blob([text], { type: 'application/json' }));
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
