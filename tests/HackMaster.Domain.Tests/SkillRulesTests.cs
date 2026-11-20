using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Maestría y prueba de habilidad del Manual del Jugador (F005).</summary>
public class SkillRulesTests
{
    private static CharacterAbilities Abilities(params (Ability Ability, int Score)[] scores) =>
        scores.Aggregate(CharacterAbilities.Empty, (all, score) => all.With(score.Ability, new AbilityScore(score.Score)));

    [Fact]
    public void Mastery_Is_The_Ability_Plus_Mastery_And_Honor_Rolls_Corte_A_La_Yugular_Gives_29__MJ100()
    {
        // Puño Cerrado: Corte a la yugular (DES, d4), Destreza 18, saca 3 en el d4 y 8 en su Dado de Honor d12.
        var mastery = SkillRules.Mastery([Ability.DES], Abilities((Ability.DES, 18)), [new SkillImprovement(3, 8)]);

        Assert.Equal(29, mastery);
    }

    [Fact]
    public void With_Several_Abilities_The_Base_Is_Their_Average_Rounded_Down__MJ307()
    {
        // «Si pone CON, SAB deben sumarse […] y dividir el total entre dos […] redondearse a la baja.»
        Assert.Equal(13, SkillRules.BaseMastery([Ability.CON, Ability.SAB], Abilities((Ability.CON, 15), (Ability.SAB, 12))));
        Assert.Equal(11, SkillRules.BaseMastery([Ability.FUE, Ability.DES, Ability.INT], Abilities((Ability.FUE, 10), (Ability.DES, 11), (Ability.INT, 14))));
    }

    [Fact]
    public void The_Fraction_Of_An_Ability_Does_Not_Count_In_The_Base__MJ307()
    {
        var abilities = CharacterAbilities.Empty.With(Ability.DES, new AbilityScore(18, 90));

        Assert.Equal(18, SkillRules.BaseMastery([Ability.DES], abilities));
    }

    [Fact]
    public void The_Ability_Counts_Only_Once_However_Many_Improvements__MJ101()
    {
        var mastery = SkillRules.Mastery([Ability.DES], Abilities((Ability.DES, 18)), [new SkillImprovement(3, 8), new SkillImprovement(2, 5), new SkillImprovement(4, 1)]);

        Assert.Equal(18 + 11 + 7 + 5, mastery);
    }

    [Fact]
    public void Mastery_Cannot_Exceed_125__MJ101()
    {
        var improvements = Enumerable.Repeat(new SkillImprovement(20, 20), 4).ToList();

        Assert.Equal(125, SkillRules.Mastery([Ability.INT], Abilities((Ability.INT, 17)), improvements));
    }

    [Fact]
    public void Without_A_Relevant_Ability_Score_There_Is_No_Mastery__MJ307()
    {
        Assert.Null(SkillRules.Mastery([Ability.DES], CharacterAbilities.Empty, [new SkillImprovement(3, 8)]));
        Assert.Null(SkillRules.Mastery([Ability.CON, Ability.SAB], Abilities((Ability.CON, 15)), []));
        Assert.Null(SkillRules.Mastery([], Abilities((Ability.CON, 15)), []));
    }

    [Theory]
    [InlineData(0, MasteryLevel.NoIdea)]
    [InlineData(1, MasteryLevel.Novice)]
    [InlineData(25, MasteryLevel.Novice)]
    [InlineData(26, MasteryLevel.Apprentice)]
    [InlineData(50, MasteryLevel.Apprentice)]
    [InlineData(51, MasteryLevel.Advanced)]
    [InlineData(75, MasteryLevel.Advanced)]
    [InlineData(76, MasteryLevel.Expert)]
    [InlineData(100, MasteryLevel.Expert)]
    [InlineData(101, MasteryLevel.Master)]
    [InlineData(125, MasteryLevel.Master)]
    public void The_Mastery_Level_Follows_The_Table__MJ100(int mastery, MasteryLevel level)
    {
        Assert.Equal(level, SkillRules.Level(mastery));
    }

    [Theory]
    [InlineData(MasteryLevel.NoIdea, "ni idea")]
    [InlineData(MasteryLevel.Novice, "principiante")]
    [InlineData(MasteryLevel.Apprentice, "aprendiz")]
    [InlineData(MasteryLevel.Advanced, "avanzado")]
    [InlineData(MasteryLevel.Expert, "experto")]
    [InlineData(MasteryLevel.Master, "maestro")]
    public void Each_Level_Has_The_Name_Of_The_Manual__MJ100(MasteryLevel level, string name)
    {
        Assert.Equal(name, SkillRules.LevelName(level));
    }

    [Fact]
    public void An_Improvement_Rolls_The_Mastery_Die_And_The_Honor_Die__MJ101()
    {
        var random = new SequenceRandom(3, 7);

        var improvement = SkillRules.RollImprovement(4, new HonorDie(12, 1), random);

        Assert.Equal(new SkillImprovement(3, 8), improvement);
        Assert.Equal([(1, 4), (1, 12)], random.Requests);
    }

    [Theory]
    [InlineData(35, 0, 35, SkillCheckOutcome.Success)]
    [InlineData(36, 0, 35, SkillCheckOutcome.Failure)]
    [InlineData(30, 10, 35, SkillCheckOutcome.Failure)]
    [InlineData(40, -10, 35, SkillCheckOutcome.Success)]
    public void A_Check_Succeeds_If_The_Roll_Plus_Modifiers_Is_At_Most_The_Mastery__MJ356(int roll, int modifier, int mastery, SkillCheckOutcome outcome)
    {
        var check = SkillRules.Check(roll, modifier, mastery);

        Assert.Equal(outcome, check.Outcome);
        Assert.Equal(roll + modifier, check.Total);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void A_Roll_Of_01_To_05_Always_Succeeds__MJ356(int roll)
    {
        Assert.Equal(SkillCheckOutcome.AutomaticSuccess, SkillRules.Check(roll, 50, 0).Outcome);
    }

    [Theory]
    [InlineData(98)]
    [InlineData(100)]
    public void A_Roll_Of_98_To_00_Always_Fails__MJ356(int roll)
    {
        Assert.Equal(SkillCheckOutcome.AutomaticFailure, SkillRules.Check(roll, -50, 125).Outcome);
    }

    [Fact]
    public void Only_01_To_05_And_98_To_00_Are_Automatic__Review_MJ356()
    {
        Assert.Equal(SkillCheckOutcome.Failure, SkillRules.Check(6, 0, 0).Outcome);
        Assert.Equal(SkillCheckOutcome.Success, SkillRules.Check(97, -50, 125).Outcome);
    }

    [Theory]
    [InlineData(-1001)]
    [InlineData(1001)]
    public void A_Check_Modifier_Beyond_1000_Is_Not_Accepted__Review(int modifier)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.RollCheck(35, modifier, new SequenceRandom(1), DateTimeOffset.Now));
        Assert.False(SkillRules.IsValidModifier(modifier));
        Assert.True(SkillRules.IsValidModifier(modifier > 0 ? 1000 : -1000));
    }

    [Fact]
    public void A_Check_Rolls_A_d100_With_The_Modifier_In_The_Formula__MJ100()
    {
        var at = new DateTimeOffset(2026, 11, 20, 10, 0, 0, TimeSpan.FromHours(1));

        var (roll, check) = SkillRules.RollCheck(35, -10, new SequenceRandom(42), at);

        Assert.Equal("1d100-10", roll.Formula.Text);
        Assert.Equal(32, roll.Total);
        Assert.Equal((42, -10, 32, 35, SkillCheckOutcome.Success), (check.Roll, check.Modifier, check.Total, check.Mastery, check.Outcome));
        Assert.Equal("1d100", SkillRules.RollCheck(35, 0, new SequenceRandom(1), at).Roll.Formula.Text);
        Assert.Equal("1d100+5", SkillRules.RollCheck(35, 5, new SequenceRandom(1), at).Roll.Formula.Text);
    }
}
