namespace HackMaster.Domain;

/// <summary>Estilo de combate. La fuente no le asigna coste en PC.</summary>
public sealed record CombatStyle
{
    /// <summary>Identificador para direcciones, único entre los estilos.</summary>
    public required string Slug { get; init; }

    /// <summary>Nombre tal como figura en la fuente.</summary>
    public required string Name { get; init; }

    /// <summary>Nombre normalizado para buscar y ordenar.</summary>
    public required string SearchName { get; init; }

    /// <summary>Características relevantes.</summary>
    public IReadOnlyList<Ability> Abilities { get; init; } = [];

    /// <summary>Dado de maestría.</summary>
    public string? MasteryDie { get; init; }

    /// <summary>Requisito para aprenderlo.</summary>
    public Prerequisite Prerequisite { get; init; } = Prerequisite.None;

    /// <summary>Precio en piezas de oro.</summary>
    public int? Price { get; init; }

    /// <summary>Dificultad en tanto por ciento.</summary>
    public int? Difficulty { get; init; }

    /// <summary>Libros donde aparece.</summary>
    public IReadOnlyList<BookRef> Books { get; init; } = [];

    /// <summary>Nombres de los campos pendientes de verificar.</summary>
    public IReadOnlyList<string> Pending { get; init; } = [];

    /// <summary>Texto original de la fuente para los campos que no se pudieron interpretar.</summary>
    public IReadOnlyDictionary<string, string> Raw { get; init; } = new SortedDictionary<string, string>();
}
