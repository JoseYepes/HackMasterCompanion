namespace HackMaster.Domain.Tests;

public class SkillFacetsTests
{
    private static Skill Make(string name, string type, string[] books, string[] classes) => new()
    {
        Slug = TextNormalizer.ToSlug(name),
        Name = name,
        SearchName = TextNormalizer.Normalize(name),
        Type = type,
        Books = books.Select(book => new BookRef(book, "1")).ToList(),
        Classes = classes,
    };

    private static readonly Skill[] Skills =
    [
        Make("Uno", "Técnicas", ["MJ", "GMG"], ["Grupo de ladrones", "Bardo"]),
        Make("Dos", "Académicas", ["CG"], ["Todas"]),
        Make("Tres", "Técnicas", ["MJ"], ["Grupo de ladrones"]),
        Make("Cuatro", "Artes marciales", ["CG"], ["Élite"]),
    ];

    [Fact]
    public void Options_Come_From_The_Data_Without_Duplicates_And_Sorted__FR017()
    {
        var facets = SkillFacets.From(Skills);

        Assert.Equal(["Académicas", "Artes marciales", "Técnicas"], facets.Types);
        Assert.Equal(["CG", "GMG", "MJ"], facets.Books);
        Assert.Equal(["Bardo", "Élite", "Grupo de ladrones"], facets.Classes);
    }

    [Fact]
    public void All_Classes_Is_Not_A_Class_Option_Because_It_Is_Always_Included__FR015()
    {
        Assert.DoesNotContain("Todas", SkillFacets.From(Skills).Classes);
    }

    [Fact]
    public void No_Skills_Give_No_Options__FR017()
    {
        var facets = SkillFacets.From([]);

        Assert.Empty(facets.Types);
        Assert.Empty(facets.Books);
        Assert.Empty(facets.Classes);
    }
}
