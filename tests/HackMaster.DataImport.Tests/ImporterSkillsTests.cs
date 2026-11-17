using HackMaster.Domain;

namespace HackMaster.DataImport.Tests;

public class ImporterSkillsTests
{
    private static ImportResult Import(params RawRow[] skills) => Importer.Import(Sheet.Workbook(skills));

    private static Skill ImportOne(RawRow skill)
    {
        var result = Import(skill);
        Assert.Empty(result.Errors);
        return Assert.Single(result.Model!.Skills);
    }

    [Fact]
    public void Skill_Keeps_Name_And_Gets_Slug_And_Search_Key__FR003()
    {
        var skill = ImportOne(Sheet.Skill(2, "Botánica", type: "Académicas"));

        Assert.Equal("Botánica", skill.Name);
        Assert.Equal("botanica", skill.Slug);
        Assert.Equal("botanica", skill.SearchName);
        Assert.Equal("Académicas", skill.Type);
        Assert.Empty(skill.Pending);
    }

    [Theory]
    [InlineData("8", "d8")]
    [InlineData("d8", "d8")]
    [InlineData("D20", "d20")]
    [InlineData("4", "d4")]
    [InlineData("NA", "NA")]
    [InlineData("na", "NA")]
    [InlineData("*", "*")]
    public void Mastery_Die_Is_Normalized__FR008(string cell, string expected)
    {
        var skill = ImportOne(Sheet.Skill(2, "Botánica", die: cell));

        Assert.Equal(expected, skill.MasteryDie);
        Assert.DoesNotContain("masteryDie", skill.Pending);
    }

    [Theory]
    [InlineData("5")]
    [InlineData("3")]
    [InlineData("2")]
    public void Mastery_Die_That_Does_Not_Exist_Is_Pending_Not_Corrected__FR009(string cell)
    {
        var result = Import(Sheet.Skill(7, "Liderazgo básico", die: cell));
        var skill = Assert.Single(result.Model!.Skills);

        Assert.Null(skill.MasteryDie);
        Assert.Contains("masteryDie", skill.Pending);
        Assert.Equal(cell, skill.Raw["masteryDie"]);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal(SheetNames.Skills, warning.Sheet);
        Assert.Equal([7], warning.Rows);
        Assert.Equal("Liderazgo básico", warning.Element);
    }

    [Theory]
    [InlineData("250", 250)]
    [InlineData("250 po", 250)]
    [InlineData("1403", 1403)]
    public void Price_Is_Read_Without_Unit__FR008(string cell, int expected)
    {
        Assert.Equal(expected, ImportOne(Sheet.Skill(2, "Botánica", price: cell)).Price);
    }

    [Theory]
    [InlineData("10", 10)]
    [InlineData("0", 0)]
    [InlineData("-5", -5)]
    [InlineData("0.1", 10)]
    [InlineData("-0.05", -5)]
    [InlineData("-0.1", -10)]
    [InlineData("15%", 15)]
    public void Difficulty_Is_A_Percentage_Even_When_Written_As_A_Fraction__FR008(string cell, int expected)
    {
        Assert.Equal(expected, ImportOne(Sheet.Skill(2, "Botánica", difficulty: cell)).Difficulty);
    }

    [Fact]
    public void Not_Applicable_Values_Are_Null_Without_Being_Pending__FR009()
    {
        var skill = ImportOne(Sheet.Skill(2, "Botánica", cost: "NA", price: "NA", difficulty: "NA"));

        Assert.Null(skill.Cost);
        Assert.Null(skill.Price);
        Assert.Null(skill.Difficulty);
        Assert.Empty(skill.Pending);
    }

    [Fact]
    public void Cost_With_Annotation_Is_Pending_And_Keeps_The_Original_Text__FR009()
    {
        var result = Import(Sheet.Skill(2, "Barrer en busca de trampas", cost: "1 R"));
        var skill = Assert.Single(result.Model!.Skills);

        Assert.Null(skill.Cost);
        Assert.Contains("cost", skill.Pending);
        Assert.Equal("1 R", skill.Raw["cost"]);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Missing_Value_Is_Pending__FR009()
    {
        var skill = Assert.Single(Import(Sheet.Skill(2, "Botánica", price: "")).Model!.Skills);

        Assert.Null(skill.Price);
        Assert.Contains("price", skill.Pending);
    }

    [Theory]
    [InlineData("INT, SAB")]
    [InlineData("INT/SAB")]
    [InlineData("int, sAB")]
    public void Abilities_Accept_Comma_Or_Slash_And_Any_Case__FR008(string cell)
    {
        Assert.Equal([Ability.INT, Ability.SAB], ImportOne(Sheet.Skill(2, "Botánica", abilities: cell)).Abilities);
    }

    [Fact]
    public void Unknown_Ability_Is_Pending_And_Known_Ones_Are_Kept__FR009()
    {
        var skill = Assert.Single(Import(Sheet.Skill(2, "Arte de la seducción, el", abilities: "CAR, ASP")).Model!.Skills);

        Assert.Equal([Ability.CAR], skill.Abilities);
        Assert.Contains("abilities", skill.Pending);
        Assert.Equal("CAR, ASP", skill.Raw["abilities"]);
    }

    [Fact]
    public void Book_Cell_With_Two_References_Gives_Two_Books__FR003()
    {
        var skill = ImportOne(Sheet.Skill(2, "Botánica", book: "MJ 329 / GMG 71"));

        Assert.Equal([new BookRef("MJ", "329"), new BookRef("GMG", "71")], skill.Books);
    }

    [Fact]
    public void Book_Without_Page_Has_Only_Acronym__FR003()
    {
        Assert.Equal([new BookRef("GrifeMaster", null)], ImportOne(Sheet.Skill(2, "Botánica", book: "GrifeMaster")).Books);
    }

    [Fact]
    public void Classes_Are_Split_By_Comma__FR003()
    {
        var skill = ImportOne(Sheet.Skill(2, "Botánica", classes: "Grupo de guerreros, Grupo de clérigos"));

        Assert.Equal(["Grupo de guerreros", "Grupo de clérigos"], skill.Classes);
    }

    [Fact]
    public void Requirement_That_Names_A_Skill_Is_Linked__FR020()
    {
        var result = Import(Sheet.Skill(2, "Botánica"), Sheet.Skill(3, "Herbalismo", requirement: "botanica"));

        Assert.Empty(result.Errors);
        var herbalism = result.Model!.Skills.Single(skill => skill.Slug == "herbalismo");
        Assert.Equal(new PrerequisiteSegment("botanica", PrerequisiteSegmentKind.Skill, "botanica"), Assert.Single(herbalism.Prerequisite.Segments));
    }

    [Fact]
    public void Unverified_Requirement_Is_Pending_And_Does_Not_Stop_The_Import__FR009()
    {
        var result = Import(Sheet.Skill(2, "Justa", requirement: "Montar criatura terrestre (sin verificar)"));

        Assert.Empty(result.Errors);
        var skill = Assert.Single(result.Model!.Skills);
        Assert.Contains("prerequisite", skill.Pending);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Summary_Comes_From_The_Descriptions_Sheet__FR003()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            descriptions: [Sheet.Description(2, "botanica", "Conocer las plantas y sus usos.")]));

        var skill = Assert.Single(result.Model!.Skills);
        Assert.Equal("Conocer las plantas y sus usos.", skill.Summary);
        Assert.Equal("conocer las plantas y sus usos.", skill.SearchSummary);
    }

    [Fact]
    public void Missing_Summary_Is_Pending_Not_Invented__FR009()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            descriptions: [Sheet.Description(2, "Botánica", summary: "")]));

        var skill = Assert.Single(result.Model!.Skills);
        Assert.Null(skill.Summary);
        Assert.Contains("summary", skill.Pending);
    }

    [Fact]
    public void Skills_Are_Sorted_By_Search_Name__FR010()
    {
        var result = Import(Sheet.Skill(2, "Zoología"), Sheet.Skill(3, "Ácidos"), Sheet.Skill(4, "botánica"));

        Assert.Equal(["acidos", "botanica", "zoologia"], result.Model!.Skills.Select(skill => skill.SearchName));
    }

    [Fact]
    public void Each_Pending_Field_Becomes_A_Doubt_Detected_By_The_Import__FR023()
    {
        var result = Import(Sheet.Skill(7, "Liderazgo básico", die: "5"));

        var doubt = Assert.Single(result.Model!.Doubts);
        Assert.Equal(DoubtOrigin.Import, doubt.Origin);
        Assert.Equal("Liderazgo básico", doubt.Subject);
        Assert.Equal(new DoubtTarget(ElementKind.Skill, "liderazgo-basico"), doubt.Target);
        Assert.Contains("5", doubt.Question);
    }

    [Fact]
    public void Rows_Without_A_Name_Are_Ignored__FR003()
    {
        var result = Import(Sheet.Skill(2, "Botánica"), Sheet.Row(SheetNames.Skills, 3, ("B", "Académicas")));

        Assert.Empty(result.Errors);
        Assert.Single(result.Model!.Skills);
    }
}
