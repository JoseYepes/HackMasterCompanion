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

### F004 — Fichas de PJ
Como jugador, quiero crear, editar, duplicar, borrar, exportar e importar fichas.
**Aceptación:** exportar e importar deja la ficha idéntica; migración de esquema v1→v2 con test; funciona sin conexión.

### F005 — Fichas con reglas
Como jugador, quiero costes, PC disponibles y tiradas de habilidad desde la ficha.
**Aceptación:** un test por regla, con casos de los manuales referenciados por libro y página en el nombre del test.

### F006 — Backend y cuentas en Azure
Como jugador, quiero ver mis fichas en cualquier dispositivo tras iniciar sesión.
**Aceptación:** la ficha creada en un dispositivo aparece en otro; tests de integración; despliegue repetible por CI; solo servicios gratuitos, con alerta de presupuesto.

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
