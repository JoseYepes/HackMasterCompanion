namespace HackMaster.Domain;

/// <summary>Características de un personaje, con las siglas que usan los manuales en castellano.</summary>
public enum Ability
{
    /// <summary>Fuerza.</summary>
    FUE,

    /// <summary>Destreza.</summary>
    DES,

    /// <summary>Constitución.</summary>
    CON,

    /// <summary>Inteligencia.</summary>
    INT,

    /// <summary>Sabiduría.</summary>
    SAB,

    /// <summary>Carisma.</summary>
    CAR,

    /// <summary>Aspecto.</summary>
    ASP,
}

/// <summary>Tipos de elemento de juego a los que puede apuntar una referencia.</summary>
public enum ElementKind
{
    /// <summary>Habilidad.</summary>
    Skill,

    /// <summary>Conjunto de habilidades definido con las reglas de HJ 4.05.</summary>
    Suite,

    /// <summary>Conjunto de habilidades oficial, publicado en los manuales.</summary>
    OfficialSuite,

    /// <summary>Estilo de combate.</summary>
    Style,
}
