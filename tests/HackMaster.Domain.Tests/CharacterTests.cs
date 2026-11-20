using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>La ficha de personaje: qué impide guardarla y cómo se copia (F004).</summary>
public class CharacterTests
{
    private static readonly DateTimeOffset Created = new(2026, 11, 19, 10, 0, 0, TimeSpan.FromHours(1));
    private static readonly DateTimeOffset Later = Created.AddHours(2);

    /// <summary>Ficha con todos los campos rellenos. Los valores son de prueba, no de un manual.</summary>
    public static Character Full() => new()
    {
        Id = "ficha-1",
        Name = "Grimbold",
        Race = "Enano",
        Class = "Guerrero",
        Level = 3,
        Alignment = "Legal neutral",
        Abilities = CharacterAbilities.Empty
            .With(Ability.FUE, new AbilityScore(15, 47))
            .With(Ability.DES, new AbilityScore(11))
            .With(Ability.ASP, new AbilityScore(9, 5)),
        HitPoints = 31,
        Honor = -2,
        Skills = [new CharacterSkill("actuar", "Actuar", 25, 2), new CharacterSkill("rastrear", "Rastrear", null, null)],
        Equipment = "Hacha de batalla\nCota de mallas",
        Notes = "Odia a los orcos.",
        CreatedAt = Created,
        UpdatedAt = Later,
    };

    [Fact]
    public void A_Complete_Character_Has_No_Problems__FR002()
    {
        Assert.Empty(Full().Problems());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_Character_Needs_A_Name__FR002(string name)
    {
        var problems = (Full() with { Name = name }).Problems();

        Assert.Contains("Escribe un nombre para la ficha.", problems);
    }

    [Fact]
    public void Level_Abilities_Mastery_And_Points_Cannot_Be_Negative__FR002()
    {
        var character = Full() with
        {
            Level = -1,
            Abilities = CharacterAbilities.Empty.With(Ability.CON, new AbilityScore(-3)),
            Skills = [new CharacterSkill("actuar", "Actuar", -5, -1)],
        };

        var problems = character.Problems();

        Assert.Contains("El nivel no puede ser negativo.", problems);
        Assert.Contains("CON no puede ser negativa.", problems);
        Assert.Contains("La maestría de Actuar no puede ser negativa.", problems);
        Assert.Contains("Los puntos de Actuar no pueden ser negativos.", problems);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100)]
    public void The_Fraction_Of_An_Ability_Goes_From_0_To_99__FR002(int fraction)
    {
        var character = Full() with { Abilities = CharacterAbilities.Empty.With(Ability.SAB, new AbilityScore(12, fraction)) };

        Assert.Contains("La fracción de SAB va de 0 a 99.", character.Problems());
    }

    [Fact]
    public void Hit_Points_And_Honor_Can_Be_Negative__FR002()
    {
        Assert.Empty((Full() with { HitPoints = -4, Honor = -10 }).Problems());
    }

    [Theory]
    [InlineData(15, 47, "15/47")]
    [InlineData(12, 5, "12/05")]
    [InlineData(11, null, "11")]
    [InlineData(null, null, "")]
    public void An_Ability_Is_Shown_With_Its_Fraction_In_Two_Digits__FR002(int? score, int? fraction, string shown)
    {
        Assert.Equal(shown, new AbilityScore(score, fraction).ToString());
    }

    [Fact]
    public void Every_Ability_Can_Be_Read_And_Changed_By_Its_Abbreviation__FR002()
    {
        var abilities = CharacterAbilities.Empty;
        foreach (var ability in Enum.GetValues<Ability>())
        {
            abilities = abilities.With(ability, new AbilityScore((int)ability + 1));
        }

        Assert.Equal(Enum.GetValues<Ability>().Select(ability => (int?)((int)ability + 1)), Enum.GetValues<Ability>().Select(ability => abilities[ability].Score));
    }

    [Fact]
    public void A_Copy_Has_Another_Id_The_Same_Data_And_Copia_In_Its_Name__FR005()
    {
        var original = Full();

        var copy = original.Duplicate("ficha-2", Later.AddDays(1));

        Assert.Equal("ficha-2", copy.Id);
        Assert.Equal("Grimbold (copia)", copy.Name);
        Assert.Equal(Later.AddDays(1), copy.CreatedAt);
        Assert.Equal(Later.AddDays(1), copy.UpdatedAt);
        Assert.Equal(original.Abilities, copy.Abilities);
        Assert.Equal(original.Skills, copy.Skills);
        Assert.NotSame(original.Skills, copy.Skills);
        Assert.Equal(original with { Id = "ficha-2", Name = "Grimbold (copia)", CreatedAt = copy.CreatedAt, UpdatedAt = copy.UpdatedAt, Skills = copy.Skills }, copy);
    }

    [Fact]
    public void Two_Characters_With_The_Same_Data_Are_Equal_Even_With_Different_Skill_Lists__SC001()
    {
        var one = Full();
        var other = Full() with { Skills = [.. Full().Skills] };

        Assert.True(one.HasSameData(other));
        Assert.False(one.HasSameData(other with { Skills = [one.Skills[0]] }));
        Assert.False(one.HasSameData(other with { Notes = "Otra cosa" }));
    }

    [Fact]
    public void Improvements_Cannot_Be_Negative__F005()
    {
        var character = Full() with { Skills = [new CharacterSkill("actuar", "Actuar") { Improvements = [new SkillImprovement(-1, 3)] }] };

        Assert.Contains("Las mejoras de Actuar no pueden ser negativas.", character.Problems());
    }
}
