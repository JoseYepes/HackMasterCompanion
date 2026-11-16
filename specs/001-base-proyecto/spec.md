# Feature Specification: Base del proyecto (F000)

**Feature Branch**: `001-base-proyecto`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "F000 — Base del proyecto. Como mantenedor, quiero una base que compile, pase tests en CI y se publique, para trabajar con seguridad. Aceptación: compila sin avisos; el test de ejemplo pasa en CI; la página vacía se publica en el sitio público del proyecto con la ruta base correcta."

## Clarifications

### Session 2026-11-16

- Q: ¿Nombre del repositorio? → A: `JoseYepes/HackMasterCompanion` (público); la raíz del repositorio es la carpeta local `HackMasterApp`.
- Q: ¿Nombre de la solución? → A: `HackMaster`.
- Q: ¿Ruta base del sitio público? → A: `/HackMasterCompanion/`.
- Q: ¿Versión de la plataforma? → A: .NET 10 (LTS vigente, soportado hasta noviembre de 2028); .NET 9 quedó sin soporte el 2026-11-10.
- Q: ¿Librería para IndexedDB? → A: Se decide en F004 con un ADR; F000 no persiste nada.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Construir y probar en local (Priority: P1)

Como mantenedor, quiero descargar el proyecto y, con unos pocos comandos documentados, construirlo
y ejecutar sus pruebas, para saber en minutos si mi copia está sana antes de tocar nada.

**Why this priority**: Sin una base que se construya y se pruebe de forma repetible no se puede
aplicar "test primero" ni fiarse de ningún cambio posterior. Es el cimiento de todo lo demás.

**Independent Test**: En una copia limpia del repositorio, seguir los comandos documentados y
comprobar que la construcción termina sin avisos y que la prueba de ejemplo pasa.

**Acceptance Scenarios**:

1. **Given** una copia limpia del repositorio y las herramientas indicadas en la documentación,
   **When** el mantenedor ejecuta el comando de construcción, **Then** termina sin errores y sin
   ningún aviso.
2. **Given** el proyecto construido, **When** el mantenedor ejecuta las pruebas, **Then** existe al
   menos una prueba de ejemplo por cada parte del proyecto que admite pruebas y todas pasan.
3. **Given** un cambio que introduce un aviso de construcción, **When** se construye, **Then** la
   construcción falla e indica el aviso.

---

### User Story 2 - Verificación automática de cada cambio (Priority: P2)

Como mantenedor, quiero que cada cambio propuesto se construya y se pruebe automáticamente antes de
fusionarse, para que la rama principal nunca quede rota.

**Why this priority**: Convierte la comprobación local en una garantía compartida y permite el
flujo de rama por funcionalidad y pull request que exige la constitución.

**Independent Test**: Abrir un cambio propuesto con una prueba que falla a propósito y comprobar
que la verificación automática lo marca como fallido; corregirlo y comprobar que pasa.

**Acceptance Scenarios**:

1. **Given** un cambio propuesto contra la rama principal, **When** se publica, **Then** se ejecutan
   automáticamente la construcción sin avisos y las pruebas, y el resultado queda visible en el
   propio cambio.
2. **Given** un cambio propuesto con una prueba que falla o con un aviso de construcción, **When**
   termina la verificación, **Then** el cambio queda marcado como fallido.
3. **Given** un cambio propuesto correcto, **When** termina la verificación, **Then** queda marcado
   como válido en menos de 10 minutos.

---

### User Story 3 - Página vacía publicada (Priority: P3)

Como mantenedor, quiero que la aplicación, todavía vacía, se publique sola en el sitio público del
proyecto cada vez que se actualiza la rama principal, para comprobar desde el primer día que la
entrega funciona de extremo a extremo.

**Why this priority**: Detecta pronto los problemas de publicación (sobre todo de rutas), que son
caros de descubrir cuando ya hay funcionalidad encima.

**Independent Test**: Tras fusionar en la rama principal, abrir la dirección pública del proyecto
en un navegador y comprobar que la página carga completa, sin recursos rotos.

**Acceptance Scenarios**:

1. **Given** una fusión en la rama principal con la verificación en verde, **When** termina la
   publicación, **Then** la dirección pública del proyecto muestra la página inicial vacía con el
   nombre de la aplicación.
2. **Given** la página publicada bajo una subruta del sitio, **When** se carga, **Then** todos sus
   recursos (estilos, código, iconos) se resuelven bajo esa subruta y ninguno devuelve error.
3. **Given** la página publicada, **When** el visitante recarga o abre directamente una dirección
   interna de la aplicación, **Then** la aplicación carga en lugar de mostrar un error de página no
   encontrada.
4. **Given** una verificación fallida en la rama principal, **When** se intenta publicar, **Then**
   no se publica nada y la versión anterior sigue disponible.

---

### Edge Cases

- ¿Qué ocurre si el repositorio se renombra o se sirve desde otra subruta? La ruta base debe poder
  cambiarse en un único lugar documentado.
- ¿Qué ocurre si un mantenedor tiene una versión distinta de las herramientas? La versión esperada
  queda fijada en el repositorio y el fallo es explícito.
- ¿Qué ocurre si alguien intenta añadir manuales u hojas de cálculo fuente al repositorio? Los
  formatos de documento fuente quedan excluidos del control de versiones por defecto.
- ¿Qué ocurre si la publicación falla a medias? La versión publicada anterior permanece intacta.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El proyecto DEBE construirse desde una copia limpia con comandos documentados, sin
  pasos manuales adicionales.
- **FR-002**: La construcción DEBE tratar cualquier aviso como error.
- **FR-003**: El proyecto DEBE incluir al menos una prueba de ejemplo por cada parte que admite
  pruebas, ejecutable con un único comando documentado.
- **FR-004**: El proyecto DEBE separar las reglas del juego de la interfaz, de modo que las reglas
  se puedan probar sin interfaz, y DEBE reservar un lugar para la herramienta de importación de
  datos, conforme al principio III de la constitución.
- **FR-005**: Cada cambio propuesto contra la rama principal DEBE verificarse automáticamente
  (construcción sin avisos, pruebas y comprobación de formato) y mostrar el resultado en el cambio.
- **FR-006**: Cada actualización de la rama principal con verificación correcta DEBE publicar la
  aplicación en el sitio público del proyecto sin intervención manual.
- **FR-007**: La aplicación publicada DEBE funcionar bajo la subruta del sitio público, con la ruta
  base definida en un único lugar.
- **FR-008**: La página inicial DEBE mostrar el nombre de la aplicación, en castellano, y ningún
  contenido de juego.
- **FR-009**: El repositorio DEBE fijar las versiones de sus herramientas y dependencias.
- **FR-010**: El repositorio DEBE excluir por defecto del control de versiones los documentos
  fuente (manuales, hojas de cálculo) y los secretos.
- **FR-011**: La documentación DEBE recoger los comandos de construcción, prueba, formato y
  ejecución local, en castellano.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un mantenedor nuevo pasa de una copia limpia a construcción y pruebas en verde en
  menos de 10 minutos siguiendo solo la documentación.
- **SC-002**: La construcción completa produce 0 avisos.
- **SC-003**: El 100 % de los cambios propuestos reciben un veredicto automático, en menos de
  10 minutos.
- **SC-004**: Tras actualizar la rama principal, la dirección pública muestra la versión nueva en
  menos de 15 minutos, con 0 recursos rotos al cargar la página.
- **SC-005**: Cambiar la ruta base requiere modificar un único lugar.

## Assumptions

- El repositorio es público y el sitio público del proyecto se sirve bajo una subruta con el
  nombre del repositorio.
- F000 no incluye datos de juego, persistencia, instalación como aplicación ni funcionamiento sin
  conexión; llegan con las funcionalidades posteriores.
- Las pruebas de interfaz de extremo a extremo no forman parte de F000; se incorporan cuando haya
  flujos que probar.
- El mantenedor es una sola persona; la revisión independiente previa a la fusión puede hacerla un
  revisor automático con contexto limpio.
- El primer commit de la rama principal es la excepción necesaria a "siempre pull request", porque
  un repositorio vacío no admite pull requests.
