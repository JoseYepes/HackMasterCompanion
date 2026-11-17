namespace HackMaster.Domain;

/// <summary>Clase de fragmento dentro de un requisito.</summary>
public enum PrerequisiteSegmentKind
{
    /// <summary>Texto sin significado propio: separadores como "," u " o ".</summary>
    Text,

    /// <summary>Nombre de una habilidad existente.</summary>
    Skill,

    /// <summary>Nombre de un conjunto de habilidades existente.</summary>
    Suite,

    /// <summary>Nombre de un conjunto oficial existente.</summary>
    OfficialSuite,

    /// <summary>Condición que no es una habilidad: característica mínima, competencia, nivel de maestría.</summary>
    Condition,

    /// <summary>Requisito que la fuente deja sin verificar.</summary>
    Unverified,
}

/// <summary>Fragmento de un requisito tal como está escrito en la fuente.</summary>
public sealed record PrerequisiteSegment(string Text, PrerequisiteSegmentKind Kind, string? Target = null);

/// <summary>
/// Requisito para aprender una habilidad. Los fragmentos, concatenados, reproducen el texto de la
/// fuente; no se interpreta la lógica "y/o".
/// </summary>
public sealed record Prerequisite
{
    /// <summary>Requisito vacío: no hace falta nada.</summary>
    public static Prerequisite None { get; } = new();

    /// <summary>Fragmentos en el orden de la fuente.</summary>
    public IReadOnlyList<PrerequisiteSegment> Segments { get; init; } = [];
}
