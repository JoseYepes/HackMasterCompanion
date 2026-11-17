using HackMaster.Domain;

namespace HackMaster.DataImport.Tests;

public class ImporterSuitesTests
{
    private static readonly RawRow[] Skills =
    [
        Sheet.Skill(2, "Botánica"),
        Sheet.Skill(3, "Herbalismo", requirement: "Botánica"),
        Sheet.Skill(4, "Sanación"),
        Sheet.Skill(5, "Puñetazo con escudo"),
        Sheet.Skill(6, "Cerrajería"),
    ];

    private static ImportResult Import(RawRow[] suites, RawRow[]? doubts = null) =>
        Importer.Import(Sheet.Workbook(Skills, suites: suites, doubts: doubts));

    [Fact]
    public void Custom_Block_Becomes_A_Suite_With_Linked_Components__FR021()
    {
        var result = Import(
        [
            Sheet.CustomTitle(2, "Herbalista", cost: "9", abilities: "INT, sAB", price: "1150", difficulty: "15", classes: "Todas"),
            Sheet.CustomHeader(3),
            Sheet.Component(4, "Botánica"),
            Sheet.Component(5, "herbalismo"),
        ]);

        Assert.Empty(result.Errors);
        var suite = Assert.Single(result.Model!.Suites);
        Assert.Equal("Herbalista", suite.Name);
        Assert.Equal("herbalista", suite.Slug);
        Assert.Equal(SuiteKind.Custom, suite.Kind);
        Assert.Equal(9, suite.Cost);
        Assert.Equal([Ability.INT, Ability.SAB], suite.Abilities);
        Assert.Equal(1150, suite.Price);
        Assert.Equal(15, suite.Difficulty);
        Assert.Equal([new BookRef("HJ", "4.05")], suite.Books);
        Assert.Equal(["Todas"], suite.Classes);
        Assert.Equal(
            [new SuiteComponent("Botánica", ElementKind.Skill, "botanica"), new SuiteComponent("herbalismo", ElementKind.Skill, "herbalismo")],
            suite.Components);
        Assert.Empty(suite.Pending);
    }

    [Fact]
    public void Custom_Suite_Without_Mastery_Die_Is_Not_Pending__F002()
    {
        var suite = Assert.Single(Import([Sheet.CustomTitle(2, "Herbalista", die: ""), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica")]).Model!.Suites);

        Assert.Null(suite.MasteryDie);
        Assert.DoesNotContain("masteryDie", suite.Pending);
    }

    [Fact]
    public void Two_Consecutive_Custom_Blocks_Are_Two_Suites__FR021()
    {
        var result = Import(
        [
            Sheet.CustomTitle(2, "Herbalista"), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica"),
            Sheet.CustomTitle(5, "Curandero"), Sheet.CustomHeader(6), Sheet.Component(7, "Sanación"),
        ]);

        Assert.Empty(result.Errors);
        Assert.Equal(["curandero", "herbalista"], result.Model!.Suites.Select(suite => suite.Slug));
        Assert.All(result.Model.Suites, suite => Assert.Single(suite.Components));
    }

    [Fact]
    public void Component_That_Is_An_Official_Suite_Is_Linked_As_Official_Suite__FR021()
    {
        var result = Import(
        [
            Sheet.CustomTitle(2, "Curandero"), Sheet.CustomHeader(3),
            Sheet.Component(4, "Conjunto de Habilidades de primeros auxilios"), Sheet.Component(5, "Sanación"),
            Sheet.SectionTitle(10, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(11),
            Sheet.Official(12, "Conjunto de habilidades de primeros auxilios"),
        ]);

        Assert.Empty(result.Errors);
        var custom = result.Model!.Suites.Single(suite => suite.Kind == SuiteKind.Custom);
        Assert.Equal(ElementKind.OfficialSuite, custom.Components[0].Kind);
        Assert.Equal("conjunto-de-habilidades-de-primeros-auxilios", custom.Components[0].Target);
    }

    [Fact]
    public void Component_Named_Like_Both_Kinds_Of_Suite_Links_To_The_Custom_One__FR021()
    {
        var result = Import(
        [
            Sheet.CustomTitle(2, "Cartógrafo"), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica"),
            Sheet.CustomTitle(6, "Explorador"), Sheet.CustomHeader(7), Sheet.Component(8, "Cartógrafo"),
            Sheet.SectionTitle(10, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(11), Sheet.Official(12, "Cartógrafo"),
        ]);

        Assert.Empty(result.Errors);
        var explorer = result.Model!.Suites.Single(suite => suite.Slug == "explorador");
        Assert.Equal(ElementKind.Suite, Assert.Single(explorer.Components).Kind);
    }

    [Fact]
    public void Official_Section_Gives_Official_Suites_With_Pending_Components__FR021()
    {
        var result = Import(
        [
            Sheet.SectionTitle(225, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(226),
            Sheet.Official(227, "Historiador", cost: "4", abilities: "INT, SAB", die: "8", price: "1375", difficulty: "15", book: "MJ 314 / GMG 71"),
        ]);

        Assert.Empty(result.Errors);
        var suite = Assert.Single(result.Model!.Suites);
        Assert.Equal(SuiteKind.Official, suite.Kind);
        Assert.Equal(4, suite.Cost);
        Assert.Equal("d8", suite.MasteryDie);
        Assert.Equal(1375, suite.Price);
        Assert.Equal([new BookRef("MJ", "314"), new BookRef("GMG", "71")], suite.Books);
        Assert.Empty(suite.Components);
        Assert.Equal(["components"], suite.Pending);
        Assert.Equal(Sheet.SuiteRequirementText, Assert.Single(suite.Prerequisite.Segments).Text);
        Assert.Equal(PrerequisiteSegmentKind.Text, suite.Prerequisite.Segments[0].Kind);
    }

    [Fact]
    public void Official_Suite_With_Question_Marks_Has_Pending_Values__FR009()
    {
        var result = Import(
        [
            Sheet.SectionTitle(225, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(226),
            Sheet.Official(236, "Conjunto de habilidades de granjero", requirement: "?", price: "", difficulty: ""),
        ]);

        Assert.Empty(result.Errors);
        var suite = Assert.Single(result.Model!.Suites);
        Assert.Null(suite.Price);
        Assert.Null(suite.Difficulty);
        Assert.Contains("price", suite.Pending);
        Assert.Contains("difficulty", suite.Pending);
        Assert.Contains("prerequisite", suite.Pending);
    }

    [Fact]
    public void Custom_Suite_Without_Values_Is_Loaded_With_Pending_Fields__FR009()
    {
        var result = Import(
        [
            Sheet.Row(SheetNames.Suites, 261, ("B", "Jinete"), ("D", "SAB"), ("F", Sheet.SuiteRequirementText), ("I", "HJ 4.05"), ("J", "Todas")),
            Sheet.CustomHeader(262),
            Sheet.Row(SheetNames.Suites, 263, ("B", "Botánica")),
        ]);

        Assert.Empty(result.Errors);
        var suite = Assert.Single(result.Model!.Suites);
        Assert.Null(suite.Cost);
        Assert.Contains("cost", suite.Pending);
        Assert.Contains("price", suite.Pending);
        Assert.Contains("difficulty", suite.Pending);
        Assert.Single(suite.Components);
    }

    [Fact]
    public void Styles_Section_Gives_Combat_Styles_With_Parsed_Requirements__FR024()
    {
        var result = Import(
        [
            Sheet.SectionTitle(252, "Estilos de combate"), Sheet.SectionHeader(253),
            Sheet.Style(256, "Estilo Escudo de la Muerte", abilities: "FUE, INT", die: "4", requirement: "Puñetazo con escudo, Fuerza 17+", price: "200", difficulty: "5", book: "CG 55"),
        ]);

        Assert.Empty(result.Errors);
        var style = Assert.Single(result.Model!.Styles);
        Assert.Equal("estilo-escudo-de-la-muerte", style.Slug);
        Assert.Equal([Ability.FUE, Ability.INT], style.Abilities);
        Assert.Equal("d4", style.MasteryDie);
        Assert.Equal(200, style.Price);
        Assert.Equal([new BookRef("CG", "55")], style.Books);
        Assert.Equal(
            [PrerequisiteSegmentKind.Skill, PrerequisiteSegmentKind.Text, PrerequisiteSegmentKind.Condition],
            style.Prerequisite.Segments.Select(segment => segment.Kind));
        Assert.Empty(result.Model.Suites);
    }

    [Fact]
    public void Custom_Block_After_The_Styles_Section_Is_Still_A_Custom_Suite__FR021()
    {
        var result = Import(
        [
            Sheet.SectionTitle(252, "Estilos de combate"), Sheet.SectionHeader(253), Sheet.Style(254, "Estilo del Yunque"),
            Sheet.CustomTitle(261, "Jinete"), Sheet.CustomHeader(262), Sheet.Component(263, "Botánica"),
        ]);

        Assert.Empty(result.Errors);
        Assert.Single(result.Model!.Styles);
        Assert.Equal(SuiteKind.Custom, Assert.Single(result.Model.Suites).Kind);
    }

    [Fact]
    public void Each_Row_Of_The_Doubts_Sheet_Is_One_Doubt_With_One_Target__FR023()
    {
        var result = Import(
            [
                Sheet.SectionTitle(225, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(226), Sheet.Official(236, "Conjunto de habilidades de granjero"),
                Sheet.SectionTitle(252, "Estilos de combate"), Sheet.SectionHeader(253), Sheet.Style(254, "Estilo del Yunque"),
            ],
            doubts:
            [
                Sheet.Doubt(2, "cerrajeria", source: "GrG 65 (tabla) / GrG 69 (texto)", question: "Discrepan.", provisional: "Tabla."),
                Sheet.Doubt(3, "Conjunto de habilidades de granjero"),
                Sheet.Doubt(4, "Estilo del Yunque"),
            ]);

        Assert.Empty(result.Errors);
        var sheetDoubts = result.Model!.Doubts.Where(doubt => doubt.Origin == DoubtOrigin.Sheet).ToList();
        Assert.Equal(3, sheetDoubts.Count);
        var locksmith = sheetDoubts.Single(doubt => doubt.Target.Kind == ElementKind.Skill);
        Assert.Equal(new DoubtTarget(ElementKind.Skill, "cerrajeria"), locksmith.Target);
        Assert.Equal("cerrajeria", locksmith.Subject);
        Assert.Equal("GrG 65 (tabla) / GrG 69 (texto)", locksmith.Source);
        Assert.Equal("Discrepan.", locksmith.Question);
        Assert.Equal("Tabla.", locksmith.Provisional);
        Assert.Contains(sheetDoubts, doubt => doubt.Target == new DoubtTarget(ElementKind.OfficialSuite, "conjunto-de-habilidades-de-granjero"));
        Assert.Contains(sheetDoubts, doubt => doubt.Target == new DoubtTarget(ElementKind.Style, "estilo-del-yunque"));
    }

    [Fact]
    public void Doubt_Ids_Are_Unique_And_Stable__FR010()
    {
        RawRow[] doubts = [Sheet.Doubt(2, "Cerrajería"), Sheet.Doubt(3, "Cerrajería", question: "Otra duda.")];

        var first = Import([], doubts).Model!.Doubts.Select(doubt => doubt.Id).ToList();
        var second = Import([], doubts).Model!.Doubts.Select(doubt => doubt.Id).ToList();

        Assert.Equal(2, first.Distinct().Count());
        Assert.Equal(first, second);
    }

    [Fact]
    public void Pending_Fields_Of_Suites_Become_Import_Doubts_After_The_Sheet_Ones__FR023()
    {
        var result = Import(
            [Sheet.SectionTitle(225, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(226), Sheet.Official(227, "Historiador")],
            doubts: [Sheet.Doubt(2, "Cerrajería")]);

        Assert.Equal([DoubtOrigin.Sheet, DoubtOrigin.Import], result.Model!.Doubts.Select(doubt => doubt.Origin));
        Assert.Equal(new DoubtTarget(ElementKind.OfficialSuite, "historiador"), result.Model.Doubts[1].Target);
    }
}
