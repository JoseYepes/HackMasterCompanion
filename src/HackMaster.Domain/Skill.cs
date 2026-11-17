namespace HackMaster.Domain;

/// <summary>
/// Habilidad que un personaje puede aprender. Un valor nulo significa que la fuente no lo da o que
/// no aplica; si además su campo figura en <see cref="Pending"/>, está pendiente de verificar.
/// </summary>
public sealed record Skill
{
    /// <summary>Identificador para direcciones, único entre las habilidades.</summary>
    public required string Slug { get; init; }

    /// <summary>Nombre tal como figura en la fuente.</summary>
    public required string Name { get; init; }

    /// <summary>Nombre normalizado para buscar y ordenar.</summary>
    public required string SearchName { get; init; }

    /// <summary>Tipo de habilidad (Académicas, Artes marciales…).</summary>
    public required string Type { get; init; }

    /// <summary>Coste en puntos de construcción (PC).</summary>
    public int? Cost { get; init; }

    /// <summary>Características relevantes.</summary>
    public IReadOnlyList<Ability> Abilities { get; init; } = [];

    /// <summary>Dado de maestría: d4, d6, d8, d10, d12, d20, NA o *.</summary>
    public string? MasteryDie { get; init; }

    /// <summary>Requisito para aprenderla.</summary>
    public Prerequisite Prerequisite { get; init; } = Prerequisite.None;

    /// <summary>Precio en piezas de oro.</summary>
    public int? Price { get; init; }

    /// <summary>Dificultad en tanto por ciento; puede ser negativa.</summary>
    public int? Difficulty { get; init; }

    /// <summary>Libros donde aparece.</summary>
    public IReadOnlyList<BookRef> Books { get; init; } = [];

    /// <summary>Clases o grupos que pueden aprenderla; "Todas" si no hay restricción.</summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    /// <summary>Resumen propio de la habilidad.</summary>
    public string? Summary { get; init; }

    /// <summary>Resumen normalizado para buscar.</summary>
    public string? SearchSummary { get; init; }

    /// <summary>Nombres de los campos pendientes de verificar.</summary>
    public IReadOnlyList<string> Pending { get; init; } = [];

    /// <summary>Texto original de la fuente para los campos que no se pudieron interpretar.</summary>
    public IReadOnlyDictionary<string, string> Raw { get; init; } = new SortedDictionary<string, string>();
}
