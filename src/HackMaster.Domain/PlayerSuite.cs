namespace HackMaster.Domain;

/// <summary>Referencia a un componente de un conjunto del jugador: una habilidad u otro conjunto.</summary>
public sealed record SuiteComponentRef(ElementKind Kind, string Slug);

/// <summary>
/// Conjunto de habilidades creado por el jugador en la aplicación. Solo guarda su composición: los
/// valores se calculan al mostrarlo con los datos vigentes.
/// </summary>
public sealed record PlayerSuite
{
    public required string Slug { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<SuiteComponentRef> Components { get; init; } = [];

    /// <summary>Características relevantes, elegidas entre las de los componentes.</summary>
    public IReadOnlyList<Ability> Abilities { get; init; } = [];

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }
}
