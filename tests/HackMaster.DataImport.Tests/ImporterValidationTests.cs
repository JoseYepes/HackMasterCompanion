namespace HackMaster.DataImport.Tests;

public class ImporterValidationTests
{
    [Fact]
    public void Duplicate_Skill_Name_Ignoring_Accents_And_Case_Is_An_Error_With_Both_Rows__FR006()
    {
        var result = Importer.Import(Sheet.Workbook([Sheet.Skill(40, "Leer/Escribir"), Sheet.Skill(212, "leer/escribir")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Skills, error.Sheet);
        Assert.Equal([40, 212], error.Rows);
        Assert.Contains("Leer/Escribir", error.Text);
        Assert.Contains("leer/escribir", error.Text);
        Assert.Null(result.Model);
    }

    [Fact]
    public void Two_Different_Names_With_The_Same_Slug_Are_An_Error__FR006()
    {
        var result = Importer.Import(Sheet.Workbook([Sheet.Skill(2, "Leer/Escribir"), Sheet.Skill(3, "Leer Escribir")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal([2, 3], error.Rows);
        Assert.Contains("leer-escribir", error.Text);
    }

    [Fact]
    public void Requirement_That_Names_No_Skill_Is_An_Error_With_Sheet_Row_And_Name__FR005()
    {
        var result = Importer.Import(Sheet.Workbook([Sheet.Skill(152, "Sigilo urbano", requirement: "Disfraz")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Skills, error.Sheet);
        Assert.Equal([152], error.Rows);
        Assert.Equal("Sigilo urbano", error.Element);
        Assert.Contains("«Disfraz»", error.Text);
        Assert.Equal("[Habilidades, fila 152] «Sigilo urbano»: el requisito «Disfraz» no coincide con ninguna habilidad ni conjunto.", error.ToString());
    }

    [Fact]
    public void Suite_Component_That_Does_Not_Exist_Is_An_Error__FR005()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            suites: [Sheet.CustomTitle(2, "Herbalista"), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica"), Sheet.Component(5, "saber animalx")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Suites, error.Sheet);
        Assert.Equal([5], error.Rows);
        Assert.Equal("Herbalista", error.Element);
        Assert.Contains("«saber animalx»", error.Text);
    }

    [Fact]
    public void Description_For_A_Skill_That_Does_Not_Exist_Is_An_Error__FR006()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            descriptions: [Sheet.Description(2, "Botánica"), Sheet.Description(3, "Zoología")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Descriptions, error.Sheet);
        Assert.Equal([3], error.Rows);
        Assert.Equal("Zoología", error.Element);
    }

    [Fact]
    public void Doubt_That_Names_No_Element_Is_An_Error__FR005()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Cerrajería")],
            doubts: [Sheet.Doubt(3, "Conjunto de habilidades de granjero / mercader / noble")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Doubts, error.Sheet);
        Assert.Equal([3], error.Rows);
    }

    [Fact]
    public void Same_Name_In_A_Custom_Suite_And_An_Official_Suite_Is_Not_An_Error__FR006()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Leer/Escribir")],
            suites:
            [
                Sheet.CustomTitle(217, "Cartógrafo"), Sheet.CustomHeader(218), Sheet.Component(219, "Leer/escribir"),
                Sheet.SectionTitle(225, "Conjuntos de habilidades oficiales"), Sheet.SectionHeader(226), Sheet.Official(227, "Cartógrafo"),
            ]));

        Assert.Empty(result.Errors);
        Assert.Equal(2, result.Model!.Suites.Count(suite => suite.Slug == "cartografo"));
    }

    [Fact]
    public void Duplicate_Suite_Name_Within_The_Same_Kind_Is_An_Error__FR006()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            suites:
            [
                Sheet.CustomTitle(2, "Herbalista"), Sheet.CustomHeader(3), Sheet.Component(4, "Botánica"),
                Sheet.CustomTitle(6, "herbalista"), Sheet.CustomHeader(7), Sheet.Component(8, "Botánica"),
            ]));

        var error = Assert.Single(result.Errors);
        Assert.Equal([2, 6], error.Rows);
    }

    [Fact]
    public void All_Errors_Are_Reported_At_Once_And_There_Is_No_Model__FR005()
    {
        var result = Importer.Import(Sheet.Workbook(
            [
                Sheet.Skill(2, "Botánica", requirement: "Religión"),
                Sheet.Skill(3, "Herbalismo", requirement: "Disfraz o Mapa mental"),
                Sheet.Skill(4, "botanica"),
            ],
            suites: [Sheet.CustomTitle(2, "Herbalista"), Sheet.CustomHeader(3), Sheet.Component(4, "No existe")]));

        Assert.Equal(5, result.Errors.Count);
        Assert.Null(result.Model);
    }

    [Fact]
    public void Import_Without_Errors_Returns_A_Model__FR010()
    {
        var result = Importer.Import(Sheet.Workbook([Sheet.Skill(2, "Botánica")]));

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Model);
    }
}
