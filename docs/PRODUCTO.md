# PRODUCTO — HackMaster Companion

> Entrada para `/speckit-specify` (parte A: qué y por qué) y `/speckit-plan` (parte B: guía técnica). No sustituye a las specs que genere Spec Kit: son su materia prima.

# Parte A — Qué y por qué

## A1. Contexto
Grupo privado de **HackMaster 4.ª edición** (castellano). Existen una web estática (`HackMasterWeb`) y un Excel (`Hackhabilidades_completo.xlsx`) con 343 habilidades, 36 conjuntos de habilidades, 23 conjuntos oficiales, 5 estilos de combate, una pestaña de descripciones (resúmenes propios) y una de dudas. Se quiere evolucionar a una **aplicación web instalable (PWA)** utilizable en móvil, con fichas de personaje y tirador de dados 3D.

## A2. Usuarios
Jugadores y director de juego, sobre todo en móvil, también tablet y PC, a veces **sin conexión** en la mesa. Castellano. Abrir en menos de 3 s en un móvil medio tras la primera carga.

## A3. Fuera de alcance por ahora
Tiendas de aplicaciones; automatizar todo el sistema de reglas; multijugador en tiempo real.

## A4. Funcionalidades, en orden (cada una = una `/speckit-specify`)

### F000 — Base del proyecto
Como mantenedor, quiero una base que compile, pase tests en CI y se publique, para trabajar con seguridad.
**Aceptación:** compila sin avisos (`dotnet build -warnaserror`); el test de ejemplo pasa en CI; la página vacía se publica en GitHub Pages con la ruta base correcta.

### F001 — Datos y consulta de habilidades
Como jugador, quiero buscar y filtrar habilidades, ver su ficha, descripciones, conjuntos y dudas.
**Aceptación:** 343 habilidades y 36 conjuntos cargados; búsqueda insensible a acentos y mayúsculas; filtros por tipo, libro, clase, precio y dificultad; sin desbordamiento horizontal a 390 px y 820 px; el importador falla si un requisito no resuelve a una habilidad.

### F002 — Creador de conjuntos y tirador 2D
Como jugador, quiero crear conjuntos con las reglas de HJ 4.05 y lanzar fórmulas de dados.
**Aceptación:** el cálculo coincide con los 36 conjuntos del Excel; el parser (`2d6+3`, varios grupos) rechaza entradas inválidas con mensajes claros; historial persistente.

### F003 — Dados 3D
Como jugador, quiero ver los dados rodar con física.
**Aceptación:** existe modo "sin 3D" de respaldo; el resultado mostrado coincide con el dominio en 1 000 tiradas de prueba; ≥ 30 fps en móvil medio (comprobación manual documentada).

### F007 — Estilo de fantasía heroica (se hace antes de F004)
Como jugador, quiero que la aplicación tenga un aspecto de fantasía heroica, con un modo noche para la mesa de juego.
**Aceptación:** estilo «pergamino y tinta» (fondo de pergamino, granate, dorado y letras de inscripción) y modo noche «taberna» (madera oscura y ámbar), elegidos por el usuario el 2026-11-18 sobre tres maquetas; contraste legible en los dos modos; sin desbordamiento horizontal a 390 px y 820 px; funciona sin conexión. Numerada F007 para no renumerar las demás.

### F008 — Dados personalizables y sonido (después de F007, antes de F004)
Como jugador, quiero elegir el color, el material y los bordes de mis dados 3D, y oír cómo ruedan, para que se parezcan a dados de verdad (calidad y ambiente parecidos a los de Baldur's Gate 3, sin copiar sus diseños).
**Aceptación:** color del dado y de los números; bordes redondeados; varios materiales o texturas; sonido de choques que depende de la fuerza del golpe, con botón para silenciarlo; texturas y sonidos de licencia libre e incluidos en la aplicación (descarga con permiso del usuario); opción de calidad más baja para móviles lentos. Pedida por el usuario el 2026-11-18.

### F004 — Fichas de PJ
Como jugador, quiero crear, editar, duplicar, borrar, exportar e importar fichas.
**Aceptación:** exportar e importar deja la ficha idéntica; migración de esquema v1→v2 con test; funciona sin conexión.

### F005 — Fichas con reglas
Como jugador, quiero que la ficha calcule la maestría de mis habilidades y me deje tirarlas.
**Aceptación:** un test por regla, con casos de los manuales referenciados por libro y página en el nombre del test. Cambiada por el usuario el 2026-11-20: los PC (puntos de construcción) solo sirven al crear el personaje y van en la futura creación de PJ (F010), no en la ficha.

### F011 — Conjuntos y muchas habilidades en la ficha (sin orden todavía)
Como jugador, quiero añadir a la ficha conjuntos de habilidades como si fueran una habilidad más (ese es el objetivo de un conjunto) y manejar con comodidad fichas con muchas habilidades. Pedida por el usuario el 2026-11-20; se especificará más adelante.

### F012 — Descargar la ficha en PDF (sin orden todavía)
Como jugador, quiero descargar mi ficha en las hojas de PJ que ya usamos: la hoja básica («HackMaster - Hoja De Personaje») y el «Player Character Record Book 3.0» en castellano, en PDF. Pedida por el usuario el 2026-11-20; necesitará una biblioteca de PDF (dependencia nueva, a consultar).

### F010 — Creación de PJ (sin orden todavía)
Como jugador, quiero una función o pestaña que me guíe al crear un personaje, con sus Puntos de Construcción (raza, clase, defectos y peculiaridades, características sacrificadas, repeticiones de tiradas y lo que sobra en oro o puntos fraccionales; MJ p. 75 y 80) y las compras iniciales de habilidades. Propuesta por el usuario el 2026-11-20; se especificará más adelante.

### F006 — Backend y cuentas en Azure
Como jugador, quiero tener mi propia cuenta e iniciar sesión para ver mis datos en cualquier dispositivo: fichas, historial de tiradas, configuración de los dados (F008), conjuntos propios y modo de pantalla.
**Aceptación:** lo creado o cambiado en un dispositivo aparece en otro con la misma cuenta; dos personas en el mismo dispositivo ven cada una sus datos; sin sesión, la aplicación sigue funcionando con los datos del dispositivo, como hasta ahora; tests de integración; despliegue repetible por CI; solo servicios gratuitos, con alerta de presupuesto. Ampliada por el usuario el 2026-11-18 (antes solo hablaba de fichas); se mantiene el orden F008 → F004 → F005 → F006.

### F009 — Grimorio (después de F005)
Como jugador, quiero buscar y filtrar los conjuros de clérigo y de mago, ver su ficha y consultar las reglas de los grimorios y de la transcripción de conjuros, igual que consulto las habilidades.
**Aceptación:** pestaña «Grimorio» con los conjuros de clérigo y de mago; búsqueda insensible a acentos y mayúsculas y filtros (clase, nivel y los que se decidan al especificarla); ficha de cada conjuro con libro y página; reglas de grimorios y de transcripción resumidas con su fuente; sin desbordamiento a 390 px y 820 px. **Fuentes** (pedidas por el usuario): Manual del Jugador en castellano y en inglés (para cotejar erratas), los manuales de erratas y la guía de magos (*Spellslinger's Guide to Wurld Domination*, SSG); las discrepancias van a `docs/dudas.md` (constitución II). Cómo llegan los datos a la aplicación (pestaña nueva en la hoja u otra vía) se decide al especificarla. Pedida por el usuario el 2026-11-19; numerada F009 para no renumerar las demás.

## A5. Dudas abiertas
Desde F001 la lista viva está en la pestaña "Dudas" de la hoja y se publica en `docs/dudas.md` (generado por `DataImport`); estas son las dudas de partida, ya trasladadas a la hoja.

1. Justa, Maniobra Ka-Bob y Desenfunde veloz (KODT 64, p. 38): sin texto fuente.
2. Requisitos de los conjuntos granjero, mercader y noble (HJ 2): sin fuente ("?").
3. Cerrajería: la tabla de GrG (3 PC; FUE/DES/SAB) y el texto (DES; 1-2 PC) discrepan; se usó la tabla.
4. Pellizcar: texto del MJ pendiente de verificar.
5. Estilos de artes marciales: descripciones genéricas; falta la tabla 5D del CG.

---

# Parte B — Guía técnica (para `/speckit-plan`)

## B1. Stack
.NET LTS, C#, Blazor WebAssembly PWA. Dados 3D con `@3d-dice/dice-box` o three.js + cannon-es vía JS interop tras `IDiceRenderer`. xUnit (dominio), bUnit (componentes), Playwright (interfaz). IndexedDB para fichas (Blazored.LocalStorage no basta). GitHub Pages (ruta base `/<repo>/`).

## B2. Estructura
```
/src/HackMaster.Domain     reglas puras, sin dependencias externas
/src/HackMaster.Web        Blazor WASM PWA, interop JS, almacenamiento local
/tests/HackMaster.Domain.Tests
/tests/HackMaster.Web.Tests
/tools/DataImport          Excel → JSON validado
/data                      JSON generado (versionado)
/docs                      PRODUCTO.md, dudas.md, adr/
/specs                     (Spec Kit) una carpeta por funcionalidad
/.specify                  (Spec Kit) constitución, plantillas, scripts
/.claude/skills            (Spec Kit con Claude Code) comandos /speckit-…
```

## B3. Modelo de datos (borrador; refinar en `data-model.md`)
**Skill:** Name, Type, Cost (PC = puntos de construcción), Abilities (FUE/DES/CON/INT/SAB/CAR), MasteryDie (d4…d20, `Auto`, `NA`), Prerequisites, Price (po, sin separador de miles), Difficulty (%), Book (acrónimo + página; `MJ` = Manual del Jugador en castellano), Classes, Description (resumen propio), ManualText (local, no se publica).
**SkillSuite:** nombre y componentes; valores calculados. Máx. 5 habilidades incluyendo requisitos; PC = suma − 1; precio = `ROUND(suma × 1,1; 0)`; dificultad = máx + 5; característica = la más frecuente; dado = media redondeada a la baja al dado existente (`MAX(4, media)` sobre {4, 6, 8, 10, 12, 20}), `*` si no aplica; clases = intersección.
**Character:** nombre, raza, clase, nivel, alineamiento, 6 características, PG, Honor, `CharacterSkill` (habilidad, nivel de maestría, puntos), equipo, notas, `SchemaVersion`.
**DiceRoll:** fórmula (`NdM±K`; `kh/kl`, explosión y ventaja/desventaja después), resultados, total, marca de tiempo, semilla opcional.

## B4. Itinerario Azure (AI-200 / Azure AI Cloud Developer)
| Tema AI-200 | Aplicación | Funcionalidad |
|---|---|---|
| Hosting y contenedores | API en Container Apps o App Service (nivel gratuito) | F006 |
| APIs sin servidor | Azure Functions para endpoints pequeños | F006 |
| Datos | Azure SQL Database serverless y/o Cosmos DB (niveles gratuitos) | F006 |
| Mensajería y eventos | Service Bus / Event Grid solo con caso real; si no, ejercicio aparte (sandbox de Microsoft Learn) | F006+ |
| IA con datos | PostgreSQL + `pgvector` para búsqueda semántica sobre descripciones propias | opcional |
| Seguridad y observabilidad | Identidad gestionada, Application Insights con muestreo bajo | F006 |

Las condiciones de los niveles gratuitos cambian: verificar en la documentación oficial antes de cada despliegue.
