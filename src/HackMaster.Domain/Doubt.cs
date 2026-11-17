namespace HackMaster.Domain;

/// <summary>De dónde sale una duda.</summary>
public enum DoubtOrigin
{
    /// <summary>Escrita por el mantenedor en la pestaña "Dudas" de la hoja de cálculo.</summary>
    Sheet,

    /// <summary>Detectada automáticamente al importar los datos.</summary>
    Import,
}

/// <summary>Elemento de juego al que afecta una duda.</summary>
public sealed record DoubtTarget(ElementKind Kind, string Slug);

/// <summary>Discrepancia o laguna en las fuentes sobre un elemento concreto.</summary>
public sealed record Doubt
{
    /// <summary>Identificador estable.</summary>
    public required string Id { get; init; }

    /// <summary>Origen de la duda.</summary>
    public required DoubtOrigin Origin { get; init; }

    /// <summary>Nombre del elemento afectado, tal como está escrito.</summary>
    public required string Subject { get; init; }

    /// <summary>Elemento afectado; nulo en una duda general, que no afecta a un elemento concreto.</summary>
    public DoubtTarget? Target { get; init; }

    /// <summary>Fuente (libro y página) de la duda.</summary>
    public string? Source { get; init; }

    /// <summary>Qué se duda.</summary>
    public required string Question { get; init; }

    /// <summary>Qué se ha hecho mientras no se resuelve.</summary>
    public string? Provisional { get; init; }
}
