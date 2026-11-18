# Research: Creador de conjuntos y tirador 2D (F002)

Decisiones técnicas del plan. Formato: decisión, por qué, alternativas descartadas.

## 1. Fuente de las reglas de cálculo de conjuntos

- **Decision**: las reglas salen de las fórmulas de la pestaña "Conjuntos de habilidades" de la
  hoja, que el usuario escribió a partir de HJ 4.05. En las 36 filas de título:
  - PC: `SUM(PC) - 1`
  - Precio: `ROUND(SUM(Precio) * 1.1, 0)`
  - Dificultad: `MAX(Dificultad) + 5`
  - Dado: `LOOKUP(MAX(4, SUM(dado) / COUNT(dado)), {4, 6, 8, 10, 12, 20})` en 32 filas.

  Las características y el requisito están escritos a mano.
- **Rationale**: las dos copias del HJ 4.05 que tiene el usuario están escaneadas y no se pueden
  leer sin reconocimiento de texto; la spec toma la hoja como referencia (SC-001), y el usuario
  confirmó la regla del dado (media redondeada a la baja a un dado real).
- **Excepciones observadas** (se registran como duda en la hoja; las pruebas las excluyen con
  nombre y motivo):
  - Dado con media + 1: Zoólogo, Escribano y Hechicero.
  - Dado escrito a mano: Cerrajero.
  - Más de 5 componentes: Forjador de armaduras y Trovador, con 6.
- **Alternatives considered**: reconocer el texto del PDF (no hay herramienta instalada y daría
  un texto poco fiable); preguntar cada regla al usuario (ya validó las que tenían dudas).

## 2. Redondeo del precio

- **Decision**: `decimal`, con `Math.Round(suma * 1.1m, MidpointRounding.AwayFromZero)`.
- **Rationale**: `ROUND` de Excel y LibreOffice redondea los medios alejándose de cero, y `double`
  daría errores de representación con × 1,1; `decimal` es exacto en base 10.
- **Alternatives considered**: `Math.Round` por defecto (redondeo al par: 2,5 → 2, distinto de la
  hoja).

## 3. Dado de maestría

- **Decision**: solo cuentan los componentes con dado numérico (d4 … d20); "NA" y "*" se ignoran,
  como `SUM/COUNT` en la hoja. La media de caras, con un mínimo de 4, se redondea a la baja al dado
  existente más grande que no la supera. Si algún componente tiene el dado pendiente de verificar,
  el dado del conjunto queda pendiente (principio I), aunque la hoja use el número erróneo. Si
  ninguno tiene dado numérico, también queda pendiente. Un conjunto componente aporta su dado.
- **Rationale**: es la regla que confirmó el usuario y coincide con la hoja en los demás casos.
- **Alternatives considered**: usar el número pendiente ("5"), como la hoja: daría un valor sin
  fuente.

## 4. Clases permitidas

- **Decision**: "Todas" es neutro. Si todas las restricciones de los componentes son el mismo
  texto, ese es el resultado. Si hay restricciones distintas (por ejemplo "Grupo de ladrones" y
  "Ladrón (clase)"), el resultado es "pendiente de verificar" y se listan. En los datos hay 9
  valores: "Todas", "Grupo de guerreros", "Grupo de ladrones", "Grupo de clérigos", "Grupo de
  magos", "Grupo de magos (Sindicato de Adivinos)", "Bárbaro (clase)", "Guerrero (clase)" y
  "Ladrón (clase)".
- **Rationale**: los datos no dicen qué clases forman cada grupo; deducirlo sería inventar
  (principio I). Con los datos actuales cubre casi todos los casos reales.
- **Alternatives considered**: una tabla de clases por grupo (no está en ninguna fuente cargada;
  se puede añadir cuando llegue a la hoja).

## 5. Gramática de las fórmulas y límites

- **Decision**: analizador escrito a mano en `Domain`, sin expresiones regulares para el conjunto,
  con esta gramática:

  ```text
  fórmula := término (("+" | "-") término)*
  término := [N] "d" X | entero
  ```

  - Se aceptan mayúsculas y espacios, y un signo inicial.
  - Debe haber al menos un grupo de dados.
  - Límites (FR-004): hasta 100 dados por fórmula, de 2 a 1000 caras, números hasta 10 000 y
    hasta 100 caracteres.
  - Penetración: hasta 100 dados extra por dado. Si se alcanza el límite, se indica.
  - Cada error lleva la posición y el trozo, más un ejemplo válido.
- **Rationale**: los mensajes con posición son más fáciles de dar con un analizador propio; la
  gramática es pequeña. Los límites cubren cualquier tirada de HackMaster (1d1000 aparece en
  tablas; d100 es habitual).
- **Alternatives considered**: expresión regular única (mensajes de error pobres); librería de
  dados de NuGet (dependencia innecesaria para una gramática de dos reglas).

## 6. Azar

- **Decision**: interfaz `IRandom` en `Domain` (`int Next(int minInclusive, int maxInclusive)`),
  exigida por la constitución (principio III). En `Web`, `SystemRandom` usa `Random.Shared`. En
  las pruebas, una secuencia fija.
- **Rationale**: tiradas deterministas en las pruebas; `Random.Shared` es uniforme y suficiente
  para un juego (no hace falta un generador criptográfico).
- **Alternatives considered**: `RandomNumberGenerator` (más lento y sin ventaja para el juego).

## 7. Guardado en el dispositivo

- **Decision**: `localStorage` del navegador mediante `IJSRuntime`, en una clase concreta
  `BrowserStore` de `Web`, sin paquetes nuevos. Hay dos claves:
  - `hackmaster.suites`: conjuntos del jugador.
  - `hackmaster.rolls`: historial y la opción de penetración.

  Cada valor es JSON con `schemaVersion`. Las migraciones son funciones puras en `Domain`
  (`StoredData.Migrate`), con pruebas. Si `localStorage` falla (modo privado, lleno), se avisa y
  la aplicación sigue en memoria.
- **Rationale**: los datos son pequeños (unos KB). IndexedDB queda para F004, con su ADR, como
  dice `PRODUCTO.md`. bUnit simula `IJSRuntime`, así que no hace falta una interfaz de
  almacenamiento (principio III).
- **Alternatives considered**: Blazored.LocalStorage (paquete innecesario para dos claves);
  IndexedDB ya (adelanta una decisión de F004).

## 8. Conjuntos del jugador junto a los de la hoja

- **Decision**: `GameData` sigue cargando los datos incrustados. Un servicio nuevo, `PlayerSuites`
  (de ámbito único), carga los conjuntos guardados y ofrece la lista combinada a las páginas de
  conjuntos y al creador. Un conjunto del jugador guarda solo su nombre, `slug`, componentes
  (tipo y `slug`), características elegidas y fechas; sus valores se calculan al mostrarlo con los
  datos vigentes (`SuiteCalculator`).
- **Direcciones**: mismo espacio de `slug` que los 36 (`/conjuntos/{slug}`), porque no se
  distinguen (aclaración de la spec). Un nombre que da un `slug` ya usado se rechaza.
- **Rationale**: los datos del juego cambian al reimportar; calcular al mostrar evita valores
  viejos (caso límite de la spec).
- **Alternatives considered**: copiar los valores al guardar (quedarían desfasados).

## 9. Pruebas

- **Decision**:
  - **xUnit en `Domain.Tests`**:
    - analizador y tirador, con `IRandom` fijo;
    - calculador de conjuntos, una prueba por regla;
    - `SuiteRulesMatchSheetTests`: recalcula los 36 conjuntos de `data/` y los compara (SC-001);
    - migraciones;
    - uniformidad de 1d6 (SC-003), con `Random` y semilla fija.
  - **bUnit**: tirador, creador, historial y lista de conjuntos, con `IJSRuntime` simulado.
  - **Playwright**: tirada y creación de un conjunto a 390 px y 820 px, sin desbordamiento, y que
    lo guardado sigue tras recargar (SC-005, SC-006).
- **Rationale**: cada criterio de éxito tiene su prueba automática.
