using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Búsqueda y filtros del Grimorio (F009). Los conjuros son de prueba, no del manual.</summary>
public class SpellQueryTests
{
    private static Spell Spell(string name, SpellClass spellClass, int level, params string[] schools) => new()
    {
        Slug = TextNormalizer.ToSlug(name),
        Name = name,
        SearchName = TextNormalizer.Normalize(name),
        Class = spellClass,
        Level = level,
        Schools = schools,
        Books = [new BookRef("MJ", "175")],
    };

    private static readonly IReadOnlyList<Spell> All =
    [
        Spell("Proyectil mágico", SpellClass.Mage, 1, "Evocación"),
        Spell("Agrandar", SpellClass.Mage, 1, "Alteración"),
        Spell("Alarma", SpellClass.Mage, 1, "Abjuración", "Evocación"),
        Spell("Bola de fuego", SpellClass.Mage, 3, "Evocación"),
        Spell("Bendecir", SpellClass.Cleric, 1, "Todas"),
        Spell("Agrandar", SpellClass.Cleric, 2, "Animal"),
    ];

    private static IEnumerable<string> Names(SpellCriteria criteria) =>
        SpellQuery.Filter(All, criteria).Select(spell => $"{spell.Name} ({spell.Class})");

    [Fact]
    public void Without_Criteria_All_Spells_Are_Listed_By_Name_Then_Class__FR001()
    {
        Assert.Equal(
            ["Agrandar (Mage)", "Agrandar (Cleric)", "Alarma (Mage)", "Bendecir (Cleric)", "Bola de fuego (Mage)", "Proyectil mágico (Mage)"],
            Names(new SpellCriteria()));
    }

    [Theory]
    [InlineData("PROYECTIL MAGICO")]
    [InlineData("magico")]
    [InlineData("  proyectil  ")]
    public void Search_Ignores_Accents_And_Capitals__FR002(string text)
    {
        Assert.Equal(["Proyectil mágico (Mage)"], Names(new SpellCriteria(Text: text)));
    }

    [Fact]
    public void Filters_Combine_Class_Level_And_School__FR003()
    {
        Assert.Equal(["Alarma (Mage)", "Proyectil mágico (Mage)"], Names(new SpellCriteria(Class: SpellClass.Mage, Level: 1, School: "evocacion")));
        Assert.Equal(["Agrandar (Cleric)"], Names(new SpellCriteria(Text: "agrandar", Class: SpellClass.Cleric)));
        Assert.Equal(["Bola de fuego (Mage)"], Names(new SpellCriteria(Level: 3)));
    }

    [Fact]
    public void Each_Class_Has_Its_Name_And_Address__FR001()
    {
        Assert.Equal(("Mago", "mago"), (SpellClasses.Name(SpellClass.Mage), SpellClasses.Slug(SpellClass.Mage)));
        Assert.Equal(("Clérigo", "clerigo"), (SpellClasses.Name(SpellClass.Cleric), SpellClasses.Slug(SpellClass.Cleric)));
        Assert.Equal(SpellClass.Cleric, SpellClasses.FromSlug("clerigo"));
        Assert.Null(SpellClasses.FromSlug("druida"));
    }

    [Fact]
    public void Facets_List_The_Levels_And_Schools_Present_In_The_Data__FR003()
    {
        var facets = SpellFacets.From(All);

        Assert.Equal([1, 2, 3], facets.Levels);
        Assert.Equal(["Abjuración", "Alteración", "Animal", "Evocación", "Todas"], facets.Schools);
    }
}
