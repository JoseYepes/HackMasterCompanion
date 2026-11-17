namespace HackMaster.Domain.Tests;

public class SkillQueryTests
{
    private static Skill Make(
        string name,
        string type = "Académicas",
        int? cost = null,
        int? price = null,
        int? difficulty = null,
        string[]? books = null,
        string[]? classes = null,
        string? summary = null) => new()
        {
            Slug = TextNormalizer.ToSlug(name),
            Name = name,
            SearchName = TextNormalizer.Normalize(name),
            Type = type,
            Cost = cost,
            Price = price,
            Difficulty = difficulty,
            Books = (books ?? ["MJ 100"]).Select(book => new BookRef(book.Split(' ')[0], book.Contains(' ') ? book.Split(' ')[1] : null)).ToList(),
            Classes = classes ?? ["Todas"],
            Summary = summary,
            SearchSummary = summary is null ? null : TextNormalizer.Normalize(summary),
        };

    private static readonly Skill Botanica = Make("Botánica", "Académicas", cost: 3, price: 250, difficulty: 10, books: ["MJ 120"], summary: "Conocimiento de las plantas.");
    private static readonly Skill Herbalismo = Make("Herbalismo", "Técnicas", cost: 5, price: 1403, difficulty: -5, books: ["CG 40"], classes: ["Grupo de clérigos"], summary: "Preparar remedios con conocimientos de botánica y hierbas.");
    private static readonly Skill Cerrajeria = Make("Cerrajería", "Técnicas", cost: 4, price: null, difficulty: 25, books: ["MJ 130", "GMG 71"], classes: ["Grupo de ladrones"]);
    private static readonly Skill Karate = Make("Kárate", "Artes marciales", cost: null, price: 500, difficulty: null, books: ["CG 50"], classes: ["Monje"]);
    private static readonly Skill Zoologia = Make("Zoología", "Académicas", cost: 2, price: 100, difficulty: 0, books: ["MJ 140"]);

    private static readonly Skill[] All = [Zoologia, Karate, Herbalismo, Cerrajeria, Botanica];

    private static IReadOnlyList<string> Names(IEnumerable<SkillMatch> matches) => matches.Select(match => match.Skill.Name).ToList();

    [Fact]
    public void Empty_Criteria_Returns_Every_Skill_Sorted_By_Name__FR012()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty);

        Assert.Equal(["Botánica", "Cerrajería", "Herbalismo", "Kárate", "Zoología"], Names(result));
    }

    [Theory]
    [InlineData("botanica")]
    [InlineData("BOTÁNICA")]
    [InlineData("botán")]
    [InlineData("  Botán ")]
    public void Text_Matches_Name_Ignoring_Accents_And_Case__FR013(string text)
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Text = text });

        Assert.Equal("Botánica", result[0].Skill.Name);
        Assert.Equal(SkillMatchKind.Name, result[0].MatchedBy);
        Assert.Null(result[0].Snippet);
    }

    [Fact]
    public void Name_Matches_Come_Before_Summary_Matches__FR013()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Text = "botanica" });

        Assert.Equal(["Botánica", "Herbalismo"], Names(result));
        Assert.Equal(SkillMatchKind.Summary, result[1].MatchedBy);
    }

    [Fact]
    public void Summary_Match_Carries_A_Snippet_With_The_Original_Text__FR013()
    {
        var match = Assert.Single(SkillQuery.Apply(All, SkillCriteria.Empty with { Text = "hierbas" }));

        Assert.Equal("Herbalismo", match.Skill.Name);
        Assert.NotNull(match.Snippet);
        Assert.Contains("hierbas", match.Snippet);
        Assert.Contains("botánica", match.Snippet);
    }

    [Fact]
    public void Snippet_Of_A_Long_Summary_Is_Shortened_Around_The_Match__FR013()
    {
        var summary = string.Join(' ', Enumerable.Repeat("relleno", 40)) + " palabra clave " + string.Join(' ', Enumerable.Repeat("relleno", 40));
        var skill = Make("Larga", summary: summary);

        var match = Assert.Single(SkillQuery.Apply([skill], SkillCriteria.Empty with { Text = "clave" }));

        Assert.Contains("palabra clave", match.Snippet);
        Assert.True(match.Snippet!.Length < summary.Length);
        Assert.StartsWith("…", match.Snippet, StringComparison.Ordinal);
        Assert.EndsWith("…", match.Snippet, StringComparison.Ordinal);
    }

    [Fact]
    public void Text_Without_Matches_Returns_Nothing__FR013()
    {
        Assert.Empty(SkillQuery.Apply(All, SkillCriteria.Empty with { Text = "dragón" }));
    }

    [Fact]
    public void Type_Filter_Keeps_Only_That_Type__FR014()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Types = Set("Artes marciales") });

        Assert.Equal(["Kárate"], Names(result));
    }

    [Fact]
    public void Several_Values_In_One_Filter_Need_Only_One__FR014()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Books = Set("MJ", "CG") });

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void Different_Filters_Must_All_Be_Met__FR014()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Books = Set("MJ", "CG"), Types = Set("Técnicas"), PriceMax = 2000 });

        Assert.Equal(["Herbalismo"], Names(result));
    }

    [Fact]
    public void Book_Filter_Compares_The_Acronym_Of_Any_Book__FR014()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Books = Set("GMG") });

        Assert.Equal(["Cerrajería"], Names(result));
    }

    [Fact]
    public void Class_Filter_Includes_Skills_For_All_Classes__FR015()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Classes = Set("Grupo de ladrones") });

        Assert.Equal(["Botánica", "Cerrajería", "Zoología"], Names(result));
    }

    [Fact]
    public void Price_Range_Excludes_Skills_Without_Price__FR016()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { PriceMin = 100, PriceMax = 500 });

        Assert.Equal(["Botánica", "Kárate", "Zoología"], Names(result));
    }

    [Fact]
    public void Difficulty_Range_Admits_Negative_Values_And_Excludes_Missing_Ones__FR016()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { DifficultyMin = -10, DifficultyMax = 10 });

        Assert.Equal(["Botánica", "Herbalismo", "Zoología"], Names(result));
    }

    [Fact]
    public void Sort_By_Cost_Puts_Missing_Values_Last__FR018()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Sort = new SkillSort(SkillSortField.Cost) });

        Assert.Equal(["Zoología", "Botánica", "Cerrajería", "Herbalismo", "Kárate"], Names(result));
    }

    [Fact]
    public void Descending_Sort_By_Price_Still_Puts_Missing_Values_Last__FR018()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Sort = new SkillSort(SkillSortField.Price, Descending: true) });

        Assert.Equal(["Herbalismo", "Kárate", "Botánica", "Zoología", "Cerrajería"], Names(result));
    }

    [Fact]
    public void Sort_By_Difficulty_Orders_Negative_First__FR018()
    {
        var result = SkillQuery.Apply(All, SkillCriteria.Empty with { Sort = new SkillSort(SkillSortField.Difficulty) });

        Assert.Equal(["Herbalismo", "Zoología", "Botánica", "Cerrajería", "Kárate"], Names(result));
    }

    [Fact]
    public void Sort_Applies_Inside_Name_And_Summary_Groups__FR013()
    {
        var first = Make("Plantas útiles", cost: 9);
        var second = Make("Plantas raras", cost: 1);
        var bySummary = Make("Alquimia", cost: 0, summary: "Usa plantas.");

        var result = SkillQuery.Apply([bySummary, first, second], SkillCriteria.Empty with { Text = "plantas", Sort = new SkillSort(SkillSortField.Cost) });

        Assert.Equal(["Plantas raras", "Plantas útiles", "Alquimia"], Names(result));
    }

    [Fact]
    public void Empty_Criteria_Has_No_Active_Filters_And_Any_Value_Activates_Them__FR014()
    {
        Assert.False(SkillCriteria.Empty.IsActive);
        Assert.False((SkillCriteria.Empty with { Text = "  " }).IsActive);
        Assert.False((SkillCriteria.Empty with { Sort = new SkillSort(SkillSortField.Price) }).IsActive);
        Assert.True((SkillCriteria.Empty with { Text = "a" }).IsActive);
        Assert.True((SkillCriteria.Empty with { Types = Set("Técnicas") }).IsActive);
        Assert.True((SkillCriteria.Empty with { DifficultyMax = 0 }).IsActive);
    }

    private static IReadOnlySet<string> Set(params string[] values) => new HashSet<string>(values, StringComparer.Ordinal);
}
