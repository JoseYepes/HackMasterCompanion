namespace HackMaster.Domain.Tests;

public class TextNormalizerTests
{
    [Theory]
    [InlineData("BOTÁNICA")]
    [InlineData("botanica")]
    [InlineData(" Botánica ")]
    [InlineData("Botánica")]
    public void Normalize_Ignores_Accents_Case_And_Outer_Spaces__FR007(string text)
    {
        Assert.Equal("botanica", TextNormalizer.Normalize(text));
    }

    [Fact]
    public void Normalize_Removes_Diaeresis__FR007()
    {
        Assert.Equal("linguistica", TextNormalizer.Normalize("Lingüística"));
    }

    [Fact]
    public void Normalize_Treats_Enye_As_N__FR007()
    {
        Assert.Equal("montanero", TextNormalizer.Normalize("Montañero"));
    }

    [Fact]
    public void Normalize_Collapses_Inner_Whitespace__FR007()
    {
        Assert.Equal("saber animal", TextNormalizer.Normalize("Saber   animal"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_Returns_Empty_For_Missing_Text__FR007(string? text)
    {
        Assert.Equal(string.Empty, TextNormalizer.Normalize(text));
    }

    [Theory]
    [InlineData("Leer/Escribir", "leer-escribir")]
    [InlineData("Botánica", "botanica")]
    [InlineData("Arte de la seducción, el", "arte-de-la-seduccion-el")]
    [InlineData("Forrajear (por clima)", "forrajear-por-clima")]
    [InlineData("  Saber   animal  ", "saber-animal")]
    [InlineData("Tasación: gemas", "tasacion-gemas")]
    public void ToSlug_Uses_Hyphens_Without_Repeats_Or_Edges__FR019(string name, string expected)
    {
        Assert.Equal(expected, TextNormalizer.ToSlug(name));
    }

    [Fact]
    public void ToSlug_Is_Equal_For_Names_That_Differ_Only_In_Case_Or_Accents__FR019()
    {
        Assert.Equal(TextNormalizer.ToSlug("Leer/Escribir"), TextNormalizer.ToSlug("leer/escribir"));
    }
}
