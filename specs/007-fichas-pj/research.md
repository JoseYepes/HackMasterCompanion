# Research: Fichas de PJ (F004)

## 1. Dónde se guardan las fichas

- **Decision**: IndexedDB, con un módulo propio `wwwroot/js/characters.js` (abrir la base
  `hackmaster`, almacén `files`, `get` y `set` de una clave) y una clase C# `IndexedStore` con el
  mismo comportamiento que `BrowserStore`: si el navegador no deja guardar, avisa y conserva los
  datos en memoria (FR-010 y caso límite).
- **Rationale**: `PRODUCTO.md` B1 pide IndexedDB para las fichas (mucho más espacio que los ~5 MB
  de `localStorage`, que comparten conjuntos e historial). Todas las fichas van en un solo valor
  JSON con versión, igual que los conjuntos: se relee antes de escribir para no pisar otra
  pestaña (mismo patrón que `PlayerSuites`).
- **Alternatives considered**: `localStorage` con `BrowserStore` (más simple, pero contradice B1 y
  el espacio es escaso con fotos o notas largas en el futuro); un paquete de IndexedDB
  (dependencia nueva para 40 líneas); un registro de IndexedDB por ficha (más código de
  sincronización sin necesidad hoy).

## 2. Versiones del formato

- **Decision**: la versión 2 es la que escribe la aplicación. La versión 1 es la ficha del borrador
  de `PRODUCTO.md` B3, con seis características; al migrar se añade `ASP` vacío. Se migra con el
  mismo sistema que el historial (`StoredData`, paso a paso con `JsonNode`). Lo guardado
  (`hackmaster.characters`) y el archivo de una ficha comparten versión, porque contienen la misma
  ficha.
- **Rationale**: criterio de aceptación de F004 («migración v1→v2 con test») y constitución V.
- **Alternatives considered**: nacer en la versión 1 y dejar la migración para F005 (no cumple la
  aceptación de F004).

## 3. Exportar e importar

- **Decision**: exportar crea en el navegador un archivo `<nombre>.hackmaster.json` (Blob y enlace
  de descarga, en `characters.js`) con `{ schemaVersion, kind: "hackmaster.character", character }`.
  Importar usa `InputFile` (máximo 1 MB), lee el texto y lo pasa por `StoredData.ReadCharacterFile`.
  Si el identificador ya existe, la página pregunta «Sustituir» o «Añadir como copia».
- **Rationale**: no hay servidor; el archivo es JSON legible. «Idéntica» se comprueba comparando la
  ficha importada con la exportada campo a campo (registro con igualdad estructural, listas
  comparadas una a una).
- **Alternatives considered**: copiar y pegar texto (incómodo en el móvil); compartir por enlace
  (necesita servidor, F006).

## 4. Identificadores y fechas

- **Decision**: cada ficha tiene un `Id` (GUID en texto) que la página genera con `Guid.NewGuid()`;
  el dominio lo recibe ya hecho (determinista). Fechas de creación y cambio con `DateTimeOffset.Now`
  en la página, como en los conjuntos; el dominio las recibe hechas.
- **Rationale**: los nombres pueden repetirse (caso límite) y la dirección `/fichas/{id}` no debe
  cambiar al renombrar.

## 5. Valores numéricos

- **Decision**: nivel, características, PG, Honor, maestría y puntos son enteros que admiten vacío.
  La característica tiene además una parte fraccionaria opcional (0–99), que se muestra como
  `12/45`. No se aplican rangos de reglas (F005); solo se rechazan negativos donde no tienen
  sentido (nivel, características, maestría, puntos).
- **Rationale**: F004 guarda lo que escribe el jugador; las reglas son de F005. El rango exacto de
  las características y de su fracción queda pendiente de verificar con el MJ en F005.

## 6. Sin conexión

- **Decision**: como en F007, «funciona sin conexión» se comprueba como «ninguna petición fuera del
  sitio» (prueba de navegador). El trabajador de servicio sigue pendiente aparte.
