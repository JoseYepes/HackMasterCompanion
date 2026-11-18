using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Aspecto de los dados elegido por el jugador (F008: FR-001 a FR-004; data-model).</summary>
public class DiceLookTests
{
    [Fact]
    public void The_Default_Look_Is_The_Ivory_Of_F003_With_Explosion_Sound_And_High_Quality__FR001_FR009()
    {
        var look = DiceLook.Default;

        Assert.Equal(DiceMaterial.Ivory, look.Material);
        Assert.Equal("#f4efe6", look.Color);
        Assert.Equal("#1f2328", look.Ink);
        Assert.Equal(PenetrationEffect.Explosion, look.Effect);
        Assert.True(look.Sound);
        Assert.Equal(DiceQuality.High, look.Quality);
    }

    [Fact]
    public void There_Are_The_11_Materials_Chosen_By_The_User__FR001()
    {
        Assert.Equal(
            [
                DiceMaterial.Ivory, DiceMaterial.Marble, DiceMaterial.OldGold, DiceMaterial.BlackIron, DiceMaterial.Ruby,
                DiceMaterial.Obsidian, DiceMaterial.Bone, DiceMaterial.Amethyst, DiceMaterial.Wood, DiceMaterial.Stone, DiceMaterial.Crystal,
            ],
            Enum.GetValues<DiceMaterial>());
    }

    [Theory]
    [InlineData(DiceMaterial.Marble, "#2f5a46", "#f1e5c4")]
    [InlineData(DiceMaterial.OldGold, "#d4a842", "#2a1608")]
    [InlineData(DiceMaterial.Wood, "#8a5a32", "#f4e3c3")]
    public void Choosing_A_Material_Takes_Its_Colors_And_Keeps_Effect_Sound_And_Quality__FR001(DiceMaterial material, string color, string ink)
    {
        var mine = DiceLook.Default with { Effect = PenetrationEffect.Blood, Sound = false, Quality = DiceQuality.Low };

        var look = mine.ForMaterial(material);

        Assert.Equal((material, color, ink), (look.Material, look.Color, look.Ink));
        Assert.Equal((PenetrationEffect.Blood, false, DiceQuality.Low), (look.Effect, look.Sound, look.Quality));
    }

    [Fact]
    public void Every_Default_Material_Has_Readable_Numbers__FR003()
    {
        foreach (var material in Enum.GetValues<DiceMaterial>())
        {
            var look = DiceLook.Default.ForMaterial(material);
            Assert.True(look.InkContrast >= 3, $"{material}: {look.Ink} sobre {look.Color} tiene {look.InkContrast:0.00}:1.");
        }
    }

    [Theory]
    [InlineData("#ffffff", "#000000", 21)]
    [InlineData("#f4efe6", "#f4efe6", 1)]
    public void Contrast_Uses_The_WCAG_Formula__FR003(string color, string ink, double expected)
    {
        Assert.Equal(expected, (DiceLook.Default with { Color = color, Ink = ink }).InkContrast, 2);
    }

    [Theory]
    [InlineData("#f4efe6", "#1f2328")]
    [InlineData("#141018", "#f8f1e0")]
    [InlineData("#c0182a", "#f8f1e0")]
    public void Readable_Ink_Is_The_Light_Or_Dark_One_That_Contrasts_More__FR003(string color, string expected)
    {
        Assert.Equal(expected, (DiceLook.Default with { Color = color, Ink = color }).ReadableInk());
    }

    [Theory]
    [InlineData("#abc")]
    [InlineData("rojo")]
    [InlineData("")]
    public void A_Broken_Color_Is_Not_Valid__FR012(string color)
    {
        Assert.False(DiceLook.IsColor(color));
        Assert.True(DiceLook.IsColor("#A1b2C3"));
    }
}
