namespace HackMaster.Domain;

/// <summary>Origen de un conjunto de habilidades.</summary>
public enum SuiteKind
{
    /// <summary>Definido con las reglas de HJ 4.05.</summary>
    Custom,

    /// <summary>Oficial, publicado en los manuales.</summary>
    Official,
}

/// <summary>Componente de un conjunto: una habilidad u otro conjunto.</summary>
public sealed record SuiteComponent(string Name, ElementKind Kind, string Target);

/// <summary>
/// Conjunto de habilidades. Los valores son los de la fuente; no se recalculan. El nombre es único
/// dentro de cada <see cref="SuiteKind"/>, no entre los dos.
/// </summary>
public sealed record SkillSuite
{
    /// <summary>Identificador para direcciones, único dentro de su <see cref="Kind"/>.</summary>
    public required string Slug { get; init; }

    /// <summary>Nombre tal como figura en la fuente.</summary>
    public required string Name { get; init; }

    /// <summary>Nombre normalizado para buscar y ordenar.</summary>
    public required string SearchName { get; init; }

    /// <summary>Origen del conjunto.</summary>
    public required SuiteKind Kind { get; init; }

    /// <summary>Coste en puntos de construcción (PC).</summary>
    public int? Cost { get; init; }

    /// <summary>Características relevantes.</summary>
    public IReadOnlyList<Ability> Abilities { get; init; } = [];

    /// <summary>Dado de maestría.</summary>
    public string? MasteryDie { get; init; }

    /// <summary>Precio en piezas de oro.</summary>
    public int? Price { get; init; }

    /// <summary>Dificultad en tanto por ciento.</summary>
    public int? Difficulty { get; init; }

    /// <summary>Libros donde aparece.</summary>
    public IReadOnlyList<BookRef> Books { get; init; } = [];

    /// <summary>Clases o grupos que pueden aprenderlo.</summary>
    public IReadOnlyList<string> Classes { get; init; } = [];

    /// <summary>Habilidades y conjuntos que lo componen; vacío si la fuente no los detalla.</summary>
    public IReadOnlyList<SuiteComponent> Components { get; init; } = [];

    /// <summary>Requisito del conjunto.</summary>
    public Prerequisite Prerequisite { get; init; } = Prerequisite.None;

    /// <summary>Nombres de los campos pendientes de verificar.</summary>
    public IReadOnlyList<string> Pending { get; init; } = [];

    /// <summary>Texto original de la fuente para los campos que no se pudieron interpretar.</summary>
    public IReadOnlyDictionary<string, string> Raw { get; init; } = new SortedDictionary<string, string>();
}
