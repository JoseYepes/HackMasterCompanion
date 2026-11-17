using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>Direcciones de las fichas, relativas a la base (<c>contracts/rutas.md</c>).</summary>
public static class Routes
{
    /// <summary>Ficha de un elemento de juego.</summary>
    public static string Element(ElementKind kind, string slug) => kind switch
    {
        ElementKind.Skill => $"habilidades/{slug}",
        ElementKind.Suite => $"conjuntos/{slug}",
        ElementKind.OfficialSuite => $"conjuntos/oficiales/{slug}",
        ElementKind.Style => $"estilos/{slug}",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>Ficha de un conjunto según su tipo.</summary>
    public static string Suite(SkillSuite suite) =>
        Element(suite.Kind == SuiteKind.Official ? ElementKind.OfficialSuite : ElementKind.Suite, suite.Slug);
}
