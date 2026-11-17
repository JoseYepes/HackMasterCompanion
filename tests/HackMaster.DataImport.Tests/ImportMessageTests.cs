namespace HackMaster.DataImport.Tests;

public class ImportMessageTests
{
    [Fact]
    public void Message_With_One_Row_And_Element_Follows_The_Contract_Format__FR005()
    {
        var message = new ImportMessage("Habilidades", 152, "Sigilo urbano", "el requisito «Disfraz» no coincide con ninguna habilidad ni conjunto.");

        Assert.Equal("[Habilidades, fila 152] «Sigilo urbano»: el requisito «Disfraz» no coincide con ninguna habilidad ni conjunto.", message.ToString());
    }

    [Fact]
    public void Message_With_Two_Rows_And_No_Element_Lists_Both_Rows__FR006()
    {
        var message = new ImportMessage("Habilidades", [40, 212], null, "nombre duplicado: «Leer/Escribir» y «Leer/escribir».");

        Assert.Equal("[Habilidades, filas 40 y 212] nombre duplicado: «Leer/Escribir» y «Leer/escribir».", message.ToString());
    }

    [Fact]
    public void Message_Without_Rows_Names_Only_The_Sheet__FR006()
    {
        var message = new ImportMessage("Dudas", [], null, "falta la pestaña en la hoja de cálculo.");

        Assert.Equal("[Dudas] falta la pestaña en la hoja de cálculo.", message.ToString());
    }

    [Fact]
    public void Description_Row_Repeated_For_The_Same_Skill_Is_An_Error__FR006()
    {
        var result = Importer.Import(Sheet.Workbook(
            [Sheet.Skill(2, "Botánica")],
            descriptions: [Sheet.Description(2, "Botánica"), Sheet.Description(9, "botanica")]));

        var error = Assert.Single(result.Errors);
        Assert.Equal(SheetNames.Descriptions, error.Sheet);
        Assert.Equal([9], error.Rows);
    }
}
