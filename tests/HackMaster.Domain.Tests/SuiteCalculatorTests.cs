using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Reglas de cálculo de un conjunto de habilidades (HJ 4.05 según la hoja; FR-008 a FR-013).</summary>
public class SuiteCalculatorTests
{
    private static SuiteComponentValues Component(
        string name,
        int? cost = 1,
        int? price = 100,
        int? difficulty = 0,
        string? die = "d6",
        string[]? classes = null,
        Ability[]? abilities = null) =>
        new(name, cost, price, difficulty, die, classes ?? ["Todas"], abilities ?? [Ability.INT]);

    private static SuiteCalculation Calculate(params SuiteComponentValues[] components) => SuiteCalculator.Calculate(components);

    [Fact]
    public void Cost_Is_The_Sum_Minus_One__HJ405()
    {
        var result = Calculate(Component("A", cost: 3), Component("B", cost: 2), Component("C", cost: 4));

        Assert.Equal(8, result.Cost);
        Assert.Null(result.CostPending);
    }

    [Theory]
    [InlineData(new[] { 100, 175, 300, 450, 250 }, 1403)] // 1275 × 1,1 = 1402,5 → 1403
    [InlineData(new[] { 100, 200 }, 330)]
    [InlineData(new[] { 5, 0 }, 6)] // 5,5 → 6: los medios se alejan de cero, como ROUND de la hoja
    public void Price_Is_The_Sum_Times_1_1_Rounded_Half_Away_From_Zero__HJ405(int[] prices, int expected)
    {
        var result = Calculate([.. prices.Select((price, i) => Component($"C{i}", price: price))]);

        Assert.Equal(expected, result.Price);
    }

    [Fact]
    public void Difficulty_Is_The_Highest_Plus_5__HJ405()
    {
        var result = Calculate(Component("A", difficulty: 5), Component("B", difficulty: 15), Component("C", difficulty: -5));

        Assert.Equal(20, result.Difficulty);
    }

    [Theory]
    [InlineData(new[] { "d6", "d8", "d10" }, "d8")] // media 8
    [InlineData(new[] { "d6", "d8" }, "d6")] // media 7: a la baja
    [InlineData(new[] { "d10", "d12", "d20" }, "d12")] // media 14
    [InlineData(new[] { "d4", "d4" }, "d4")]
    [InlineData(new[] { "d20", "d20" }, "d20")]
    [InlineData(new[] { "d4", "d6" }, "d4")] // media 5
    public void Mastery_Die_Is_The_Average_Rounded_Down_To_A_Real_Die__HJ405(string[] dice, string expected)
    {
        var result = Calculate([.. dice.Select((die, i) => Component($"C{i}", die: die))]);

        Assert.Equal(expected, result.MasteryDie);
        Assert.Null(result.MasteryDiePending);
    }

    [Fact]
    public void Mastery_Die_Ignores_Components_Without_A_Numeric_Die__HJ405()
    {
        var result = Calculate(Component("A", die: "d10"), Component("B", die: "NA"), Component("C", die: "*"), Component("D", die: "d10"));

        Assert.Equal("d10", result.MasteryDie);
    }

    [Fact]
    public void Mastery_Die_Is_Pending_When_No_Component_Has_A_Numeric_Die__FR013()
    {
        var result = Calculate(Component("A", die: "NA"), Component("B", die: "*"));

        Assert.Null(result.MasteryDie);
        Assert.NotNull(result.MasteryDiePending);
    }

    [Fact]
    public void Value_Depending_On_A_Pending_Datum_Is_Pending_And_Names_The_Component__FR013()
    {
        var result = Calculate(
            Component("Liderazgo básico", die: null),
            Component("Gran maestría", cost: null),
            Component("Rara", price: null, difficulty: null));

        Assert.Null(result.MasteryDie);
        Assert.Contains("Liderazgo básico", result.MasteryDiePending, StringComparison.Ordinal);
        Assert.Null(result.Cost);
        Assert.Contains("Gran maestría", result.CostPending, StringComparison.Ordinal);
        Assert.Null(result.Price);
        Assert.Contains("Rara", result.PricePending, StringComparison.Ordinal);
        Assert.Null(result.Difficulty);
        Assert.Contains("Rara", result.DifficultyPending, StringComparison.Ordinal);
    }

    [Fact]
    public void Classes_Are_All_When_No_Component_Is_Restricted__FR012b()
    {
        var result = Calculate(Component("A"), Component("B"));

        Assert.Equal(["Todas"], result.Classes);
        Assert.Null(result.ClassesPending);
    }

    [Fact]
    public void A_Restriction_Is_Kept_When_The_Others_Allow_All__FR012b()
    {
        var result = Calculate(Component("A", classes: ["Grupo de ladrones"]), Component("B"), Component("C", classes: ["Grupo de ladrones"]));

        Assert.Equal(["Grupo de ladrones"], result.Classes);
    }

    [Fact]
    public void Shared_Classes_Of_Restricted_Components_Are_Kept__FR012b()
    {
        var result = Calculate(Component("A", classes: ["Grupo de ladrones", "Grupo de magos"]), Component("B", classes: ["Grupo de ladrones"]));

        Assert.Equal(["Grupo de ladrones"], result.Classes);
    }

    [Fact]
    public void Restrictions_That_Cannot_Be_Combined_Are_Pending_And_Listed__FR012b()
    {
        var result = Calculate(Component("A", classes: ["Grupo de ladrones"]), Component("B", classes: ["Ladrón (clase)"]));

        Assert.Empty(result.Classes);
        Assert.Contains("Grupo de ladrones", result.ClassesPending, StringComparison.Ordinal);
        Assert.Contains("Ladrón (clase)", result.ClassesPending, StringComparison.Ordinal);
    }

    [Fact]
    public void Available_Abilities_Are_Those_Of_The_Components_Without_Repeats__FR012()
    {
        var result = Calculate(Component("A", abilities: [Ability.INT, Ability.SAB]), Component("B", abilities: [Ability.SAB, Ability.DES]));

        Assert.Equal([Ability.DES, Ability.INT, Ability.SAB], result.AvailableAbilities.Order());
    }

    [Fact]
    public void A_Suite_Used_As_Component_Contributes_Its_Own_Values__FR011()
    {
        var herbalista = Component("Herbalista", cost: 5, price: 800, difficulty: 10, die: "d8");

        var result = Calculate(herbalista, Component("Rastrear", cost: 2, price: 200, difficulty: 5, die: "d8"));

        Assert.Equal(6, result.Cost);
        Assert.Equal(1100, result.Price);
        Assert.Equal(15, result.Difficulty);
        Assert.Equal("d8", result.MasteryDie);
    }

    [Fact]
    public void Values_Can_Be_Taken_From_Game_Data__FR011()
    {
        var skill = new Skill
        {
            Slug = "s",
            Name = "Sin dado",
            SearchName = "sin dado",
            Type = "Prueba",
            Cost = 2,
            Price = 100,
            Difficulty = 5,
            MasteryDie = null,
            Classes = ["Todas"],
            Abilities = [Ability.FUE],
            Pending = ["masteryDie"],
        };

        var values = SuiteComponentValues.From(skill);

        Assert.Equal(("Sin dado", 2, 100, 5, (string?)null), (values.Name, values.Cost, values.Price, values.Difficulty, values.MasteryDie));
        Assert.Equal([Ability.FUE], values.Abilities);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void A_Suite_Can_Be_Saved_With_2_To_5_Components__FR010(int count, bool canSave)
    {
        var components = Enumerable.Range(1, count).Select(i => new SuiteComponentRef(ElementKind.Skill, $"h{i}")).ToList();

        var problem = SuiteCalculator.CheckComposition(components, self: null, _ => []);

        Assert.Equal(canSave, problem is null);
        if (!canSave)
        {
            Assert.Contains("entre 2 y 5", problem, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void A_Component_Cannot_Be_Repeated__FR011()
    {
        var botanica = new SuiteComponentRef(ElementKind.Skill, "botanica");

        var problem = SuiteCalculator.CheckComposition([botanica, botanica, new SuiteComponentRef(ElementKind.Skill, "rastrear")], null, _ => []);

        Assert.Contains("repetido", problem, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Suite_Cannot_Contain_Itself_Even_Indirectly__FR011()
    {
        var mine = new SuiteComponentRef(ElementKind.Suite, "mi-conjunto");
        var other = new SuiteComponentRef(ElementKind.Suite, "otro");
        IReadOnlyList<SuiteComponentRef> Children(SuiteComponentRef parent) => parent == other ? [mine] : [];

        var problem = SuiteCalculator.CheckComposition([other, new SuiteComponentRef(ElementKind.Skill, "rastrear")], mine, Children);

        Assert.Contains("a sí mismo", problem, StringComparison.Ordinal);
    }
}
