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
        Reversible = name is "Agrandar" or "Bendecir",
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
        Assert.Contains("Clérigo, nivel 1", bendecir.TextContent, StringComparison.Ordinal);
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

        string Field(string name) => cut.Find($"[data-testid=spell-field-{name}]").TextContent.Trim();
        Assert.Equal("Agrandar", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Mago, nivel 1", cut.Find("[data-testid=spell-class-level]").TextContent.Trim());
        Assert.Equal("Alteración", Field("schools"));
        Assert.Equal("Sí (Reducir)", Field("reverse"));
        Assert.Equal("5 yardas por nivel", Field("range"));
        Assert.Equal("V, S, M", Field("components"));
        Assert.Equal("5 asaltos por nivel", Field("duration"));
        Assert.Equal("1 segmento", Field("castingTime"));
        Assert.Equal("Una criatura u objeto", Field("areaOfEffect"));
        Assert.Equal("Niega", Field("savingThrow"));
        Assert.Equal("MJ 175", Field("books"));
        Assert.Contains("Resumen de prueba de Agrandar.", cut.Find("[data-testid=spell-summary]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Reversible_Spell_Without_A_Name_For_Its_Reverse_Says_Just_Yes__Review()
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "clerigo").Add(page => page.Slug, "bendecir"));

        Assert.Equal("Sí", cut.Find("[data-testid=spell-field-reverse]").TextContent.Trim());
    }

    [Fact]
    public void A_Spell_That_Is_Not_Reversible_Has_No_Reversible_Row__Review()
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "mago").Add(page => page.Slug, "bola-de-fuego"));

        Assert.Empty(cut.FindAll("[data-testid=spell-field-reverse]"));
    }

    [Fact]
    public void A_Pending_Reverse_And_A_Missing_Summary_Are_Marked__Review()
    {
        Services.AddSingleton(new GrimoireData([Spell("Dudoso", SpellClass.Mage, 1, "Alteración", "reverse") with { Summary = null }], []));

        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "mago").Add(page => page.Slug, "dudoso"));

        Assert.NotEmpty(cut.FindAll("[data-testid=spell-field-reverse] .pending-mark"));
        Assert.NotEmpty(cut.FindAll("[data-testid=spell-summary] .pending-mark"));
    }

    [Fact]
    public void The_Class_In_The_Address_Ignores_Capitals__Review()
    {
        var cut = Render<SpellDetail>(parameters => parameters.Add(page => page.Class, "Mago").Add(page => page.Slug, "agrandar"));

        Assert.Equal("Agrandar", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Spell_Data_Of_Another_Version_Or_With_Repeated_Spells_Is_Reported__Review()
    {
        var newer = GrimoireData.FromJson("""{ "schemaVersion": 99, "spells": [], "rules": [] }""");
        var repeated = GrimoireData.FromJson("""
            { "schemaVersion": 1, "rules": [], "spells": [
              { "slug": "luz", "name": "Luz", "searchName": "luz", "class": "Mage", "level": 1, "books": [] },
              { "slug": "luz", "name": "Luz", "searchName": "luz", "class": "Mage", "level": 1, "books": [] } ] }
            """);

        Assert.Contains("versión", newer.LoadError, StringComparison.Ordinal);
        Assert.Contains("repetido", repeated.LoadError, StringComparison.Ordinal);
        Assert.Empty(repeated.Spells);
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
        Assert.True(data.Spells.Count >= 87, $"Solo hay {data.Spells.Count} conjuros incrustados.");
        Assert.True(data.Rules.Count >= 7, $"Solo hay {data.Rules.Count} reglas incrustadas.");
    }
}
