using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Páginas del Grimorio (F009). Los conjuros son de prueba, no del manual.</summary>
public class GrimoirePagesTests : BunitContext
{
    private static Spell Spell(string name, SpellClass spellClass, int level, string school, params string[] pending) => new()
    {
        Slug = TextNormalizer.ToSlug(name),
        Name = name,
        SearchName = TextNormalizer.Normalize(name),
        Class = spellClass,
        Level = level,
        Schools = [school],
        Reverse = name == "Agrandar" ? "Reducir" : null,
        Range = pending.Contains("range") ? null : "5 yardas por nivel",
        Components = "V, S, M",
        Duration = "5 asaltos por nivel",
        CastingTime = "1 segmento",
        AreaOfEffect = "Una criatura u objeto",
        SavingThrow = "Niega",
        Books = [new BookRef("MJ", "175")],
        Summary = $"Resumen de prueba de {name}.",
        Pending = pending,
    };

    private static GrimoireData Data() => new(
        [
            Spell("Agrandar", SpellClass.Mage, 1, "Alteración"),
            Spell("Proyectil mágico", SpellClass.Mage, 1, "Evocación", "range"),
            Spell("Bola de fuego", SpellClass.Mage, 3, "Evocación"),
            Spell("Bendecir", SpellClass.Cleric, 1, "Todas"),
        ],
        [new GrimoireRule("Libros de conjuros", "Resumen de prueba de la regla.", [new BookRef("MJ", "123")])]);

    public GrimoirePagesTests() => Services.AddSingleton(Data());

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Grimoire> Open(string uri)
    {
        Navigation.NavigateTo(uri);
        return Render<Grimoire>();
    }

    private static IEnumerable<string> Names(IRenderedComponent<Grimoire> cut) =>
        cut.FindAll("[data-testid=spell-row] .spell-name").Select(name => name.TextContent.Trim());

    [Fact]
    public void The_Grimoire_Lists_Every_Spell_With_Class_Level_And_School__FR001()
    {
        var cut = Open("grimorio");

        Assert.Equal(["Agrandar", "Bendecir", "Bola de fuego", "Proyectil mágico"], Names(cut));
        Assert.Contains("4 conjuros", cut.Find("[data-testid=spell-count]").TextContent, StringComparison.Ordinal);
        var bendecir = cut.FindAll("[data-testid=spell-row]")[1];
        Assert.Contains("Clérigo 1", bendecir.TextContent, StringComparison.Ordinal);
        Assert.Contains("Todas", bendecir.TextContent, StringComparison.Ordinal);
        Assert.Equal("grimorio/clerigo/bendecir", bendecir.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void Searching_Ignores_Accents_And_Capitals__FR002()
    {
        var cut = Open("grimorio");

        cut.Find("[data-testid=spell-search]").Input("MAGICO");

        cut.WaitForAssertion(() => Assert.Equal(["Proyectil mágico"], Names(cut)));
    }

    [Fact]
    public void Filters_Are_Read_From_The_Address__FR003()
    {
        var cut = Open("grimorio?clase=mago&nivel=1&escuela=Evocación");

        Assert.Equal(["Proyectil mágico"], Names(cut));
        Assert.Equal("mago", cut.Find("[data-testid=spell-class]").GetAttribute("value"));
    }

    [Fact]
    public void Choosing_A_Filter_Puts_It_In_The_Address__FR003()
    {
        var cut = Open("grimorio");

        cut.Find("[data-testid=spell-class]").Change("clerigo");

        Assert.Equal(["Bendecir"], Names(cut));
        Assert.EndsWith("grimorio?clase=clerigo", Navigation.Uri, StringComparison.Ordinal);
        cut.Find("[data-testid=spell-level]").Change("1");
        Assert.EndsWith("grimorio?clase=clerigo&nivel=1", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_Matches_It_Says_So__FR001()
    {
        var cut = Open("grimorio?nivel=9");

        Assert.Contains("Ningún conjuro", cut.Find("[data-testid=spell-none]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_Spells_Loaded_It_Says_They_Arrive_In_Batches__Spec()
    {
        Services.AddSingleton(new GrimoireData([], []));
        Navigation.NavigateTo("grimorio");

        var cut = Render<Grimoire>();

        Assert.Contains("aún no", cut.Find("[data-testid=spell-none]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Spell_Shows_All_Its_Data_Summary_And_Book__FR004()
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "mago").Add(page => page.Slug, "agrandar"));

        var text = cut.Markup;
        Assert.Equal("Agrandar", cut.Find("h1").TextContent.Trim());
        foreach (var expected in new[] { "Mago", "nivel 1", "Alteración", "Reducir", "5 yardas por nivel", "V, S, M", "5 asaltos por nivel", "1 segmento", "Una criatura u objeto", "Niega", "MJ 175", "Resumen de prueba de Agrandar." })
        {
            Assert.Contains(expected, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_Pending_Field_Is_Marked__FR004_Constitucion_I()
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "mago").Add(page => page.Slug, "proyectil-magico"));

        Assert.NotEmpty(cut.FindAll("[data-testid=spell-field-range] .pending-mark"));
    }

    [Theory]
    [InlineData("mago", "no-existe")]
    [InlineData("druida", "agrandar")]
    [InlineData("clerigo", "agrandar")]
    public void An_Unknown_Spell_Shows_Not_Found__FR004(string spellClass, string slug)
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, spellClass).Add(page => page.Slug, slug));

        Assert.Contains("Página no encontrada", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Rules_Page_Lists_The_Grimoire_Rules_With_Their_Source__FR005()
    {
        var cut = Render<GrimoireRules>();

        var rule = cut.Find("[data-testid=grimoire-rule]");
        Assert.Contains("Libros de conjuros", rule.TextContent, StringComparison.Ordinal);
        Assert.Contains("Resumen de prueba de la regla.", rule.TextContent, StringComparison.Ordinal);
        Assert.Contains("MJ 123", rule.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Embedded_Spells_Load_Without_Errors__FR006()
    {
        var data = GrimoireData.LoadEmbedded();

        Assert.Null(data.LoadError);
    }
}
