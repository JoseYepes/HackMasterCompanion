# Research: Dados 3D (F003)

Decisiones técnicas del plan. Formato: decisión, por qué, alternativas descartadas. Datos de npm y
jsDelivr consultados el 2026-11-18.

## 1. Librería 3D

- **Decision**: **three.js 0.187.0** (gráficos) y **cannon-es 0.20.0** (física), las dos con
  licencia MIT, más un módulo JavaScript propio y pequeño (`wwwroot/js/dice3d.js`) que crea los
  dados, los tira y hace que cada uno se pare en la cara que manda el tirador.
- **Rationale**:
  - Es la combinación que propone `docs/PRODUCTO.md` (B1).
  - three.js está mantenida: última versión de octubre de 2026.
  - Permite la técnica de §2 para que el dado muestre el valor del dominio (FR-001).
  - cannon-es no tiene versiones desde 2022, pero es estable y solo se usa para la física de
    unos pocos cuerpos.
- **Alternatives considered**:
  - `@3d-dice/dice-box` 1.1.4 (Babylon.js, 2024): el resultado lo decide su física; no hay forma
    documentada de forzar las caras. Sin eso no se cumple FR-001.
  - `@3d-dice/dice-box-threejs` 0.0.12: admite resultados forzados (`2d6@3,4`), pero lleva sin
    publicar desde 2022 y depende de three 0.143, cuatro años más vieja.
  - Animación hecha a mano sin física: no cumple «rodar con física».

## 2. Cómo se fuerza la cara

- **Decision**: para cada tirada, el módulo:
  1. simula la física sin dibujar, en un paso fijo, hasta que todos los dados se paran;
  2. mira qué cara quedó arriba en cada dado;
  3. cambia los números del dado para que esa cara lleve el valor que manda el tirador;
  4. reproduce la simulación dibujándola.

  Como la física es determinista con la misma semilla y el mismo paso, el dado se para igual y
  muestra el valor pedido.
- **Rationale**: es la técnica habitual de los tiradores con resultado predeterminado; el azar
  sigue siendo del dominio (principio III), y la física solo decide cómo rueda.
- **Alternatives considered**: girar el dado al final hasta la cara buena (se nota el salto).

## 3. Cómo llega a la aplicación

- **Decision**: copiar los archivos ya compilados de las dos librerías en
  `src/HackMaster.Web/wwwroot/lib/` (`three.module.js` 643 KB, `three.core.js` 1440 KB,
  `cannon-es.js` 338 KB), con su licencia y versión anotadas.
  - Se cargan con `import()` dinámico solo cuando el jugador activa los dados 3D (FR-016,
    SC-005).
  - Sin npm ni paso de compilación de JavaScript en el proyecto.
  - Comprimidos por GitHub Pages ocupan unos 500 KB.
- **Rationale**: funciona sin conexión cuando llegue la PWA (principio V) y no añade
  herramientas; el tamaño solo lo paga quien activa el 3D.
- **Alternatives considered**:
  - Cargarlas desde un CDN: más ligero en el repositorio, pero no funcionaría sin conexión y
    depende de un tercero.
  - npm con un empaquetador: añade Node.js y un paso de compilación para dos archivos.
- **Pendiente**: descargar los archivos exige permiso del usuario (se pide antes de implementar).

## 4. Qué decide qué

- **Decision**: en `Domain`, una función pura `DiceBoard.Plan(RollResult)` traduce la tirada a
  la lista de dados que se dibujan:
  - forma y valor de cada dado;
  - los extra por penetración, en orden;
  - el d100, como dos d10 (decenas y unidades);
  - los dados sin forma (d2, d3, d1000…) aparte;
  - y si la tirada cabe en 20 dados.

  El componente Blazor `DiceBoardView` pasa ese plan al módulo JavaScript y espera a que termine.
- **Rationale**: las reglas (d100, límite de 20, qué se dibuja) se prueban con TDD en el
  dominio; el JavaScript solo dibuja.
- **Alternatives considered**: decidirlo en JavaScript (sin pruebas en .NET y con reglas
  repartidas).

## 5. Abstracción `IDiceRenderer`

- **Decision**: **no** se crea. `PRODUCTO.md` (B1) la proponía, pero la constitución (principio III)
  pide una abstracción solo con un segundo caso de uso real. El modo sin 3D no la necesita: es el
  tirador de F002 sin el componente `DiceBoard`. bUnit simula el JavaScript sin interfaz.
- **Rationale**: la constitución prevalece sobre `PRODUCTO.md`.

## 6. Pruebas

- **Decision**:
  - **xUnit**: `DiceBoardTests`, con el plan de dados (d100, límite, penetración, dados sin forma).
  - **bUnit**: `DicePageTests`.
    - opción «Dados 3D» guardada y desactivada por defecto;
    - el resultado de texto aparece al terminar;
    - fallo del módulo → modo sin 3D con aviso.
  - **Playwright**:
    - `Dice3DTests` pide al módulo, sin dibujar, la cara de arriba de cada dado en 1 000
      tiradas de todas las formas y comprueba que coincide con el valor pedido (SC-001);
    - comprueba también que una tirada en pantalla termina en menos de 4 s (SC-004) y que no
      hay desbordamiento (SC-006).
  - **A mano**: fotogramas por segundo en el móvil del usuario (SC-003), anotado con el modelo.
- **Rationale**: SC-001 exige comprobar la física real, que solo existe en el navegador.

## 7. Tamaño de la descarga inicial (SC-005)

- **Decision**: medir con Playwright los bytes descargados al abrir `/habilidades` antes y después
  de F003; el 3D no debe aparecer porque solo se importa al activarlo.
- **Medido el 2026-11-18 (T017)**:
  - Publicando `main` y la rama en Release, la descarga comprimida (Brotli) sin `lib/` ni
    `dice3d.js` pasa de 2827 KB a 2837 KB (+0,35 %): `HackMaster.Domain` 31 → 35 KB y
    `HackMaster.Web` 98 → 103 KB.
  - `lib/` comprimida ocupa 368 KB y solo se descarga al activar «Dados 3D».
  - La prueba `Without_3D_Nothing_Of_The_3D_Part_Is_Downloaded__FR016` comprueba que, con el 3D
    desactivado, ni `/habilidades` ni `/dados` piden nada de `lib/` ni `dice3d.js`.
  - Una tirada de 7 dados en pantalla termina en menos de 4 s (`Dice3DTests`, SC-004).
