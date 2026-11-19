# Contrato: archivo de ficha (exportar e importar)

Nombre al exportar: el nombre de la ficha pasado a slug + `.hackmaster.json` (por ejemplo,
`grimbold.hackmaster.json`). Codificación UTF-8, JSON con sangría de 2 espacios.

```json
{
  "schemaVersion": 2,
  "kind": "hackmaster.character",
  "character": {
    "id": "8b0c…",
    "name": "Grimbold",
    "race": "Enano",
    "class": "Guerrero",
    "level": 3,
    "alignment": "Legal neutral",
    "abilities": {
      "FUE": { "score": 15, "fraction": 47 },
      "DES": { "score": 11 },
      "CON": {}, "INT": {}, "SAB": {}, "CAR": {}, "ASP": {}
    },
    "hitPoints": 31,
    "honor": 12,
    "skills": [ { "slug": "actuar", "name": "Actuar", "mastery": 25, "points": 2 } ],
    "equipment": "Hacha de batalla",
    "notes": "…",
    "createdAt": "2026-11-19T10:00:00+00:00",
    "updatedAt": "2026-11-19T10:05:00+00:00"
  }
}
```

Los valores de ejemplo son solo ilustrativos. Los campos vacíos se omiten.

## Reglas al importar (`StoredData.ReadCharacterFile`)

| Entrada | Resultado |
|---|---|
| `kind` distinto de `hackmaster.character` o falta | error «El archivo no es una ficha de HackMaster Companion.» |
| sin `schemaVersion` | error de versión |
| `schemaVersion` > 2 | error «versión más reciente» |
| `schemaVersion` 1 | se migra a 2 (ASP vacío) |
| sin `id` o `name` | error «incompleto» |
| JSON no válido | error «dañado» |

Exportar una ficha e importarla da una ficha igual en todos los campos (SC-001).

## Módulo `wwwroot/js/characters.js`

| Función | Qué hace |
|---|---|
| `get(key)` | valor de texto guardado en IndexedDB, o `null` |
| `set(key, value)` | guarda el texto; si falla, lanza error (C# lo convierte en aviso) |
| `download(fileName, text)` | descarga el texto como archivo `application/json` |
