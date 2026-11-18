using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Formato de lo que se guarda en el dispositivo y sus versiones (FR-018, principio V).</summary>
public class StoredDataTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 10, 0, 0, TimeSpan.FromHours(1));

    private static PlayerSuitesFile SampleSuites() => new(StoredData.SuitesVersion,
    [
        new PlayerSuite
        {
            Slug = "mi-explorador",
            Name = "Mi explorador",
            Components = [new SuiteComponentRef(ElementKind.Skill, "rastrear"), new SuiteComponentRef(ElementKind.Suite, "herbalista")],
            Abilities = [Ability.SAB],
            CreatedAt = At,
            UpdatedAt = At,
        },
    ]);

    private static RollHistoryFile SampleRolls() => new(StoredData.RollsVersion, Penetrating: true,
    [
        new StoredRoll("2d6+3", Penetrating: false, Total: 10, RolledAt: At, Constant: 3,
            Groups: [new StoredGroup(Count: 2, Sides: 6, Sign: 1, Subtotal: 7, Capped: false, Dice: [new StoredDie(3, false), new StoredDie(4, false)])]),
    ]);

    [Fact]
    public void Suites_Survive_A_Write_And_Read_Round_Trip__FR018()
    {
        var original = SampleSuites();

        var read = StoredData.ReadSuites(StoredData.Write(original));

        Assert.Null(read.Error);
        var suite = Assert.Single(read.Value!.Suites);
        Assert.Equal("Mi explorador", suite.Name);
        Assert.Equal(original.Suites[0].Components, suite.Components);
        Assert.Equal([Ability.SAB], suite.Abilities);
        Assert.Equal(At, suite.UpdatedAt);
    }

    [Fact]
    public void Rolls_Survive_A_Write_And_Read_Round_Trip__FR018()
    {
        var read = StoredData.ReadRolls(StoredData.Write(SampleRolls()));

        Assert.Null(read.Error);
        Assert.True(read.Value!.Penetrating);
        var roll = Assert.Single(read.Value.Rolls);
        Assert.Equal("2d6+3", roll.Formula);
        Assert.Equal(10, roll.Total);
        Assert.Equal([3, 4], roll.Groups[0].Dice.Select(die => die.Value));
    }

    [Fact]
    public void Written_Data_Carries_The_Current_Schema_Version_Of_Each_Format__FR018()
    {
        // Cada formato tiene su versión: el historial cambió en F003 (dice3d) y en F008 (look) y los conjuntos no, así
        // que una versión anterior de la aplicación abierta en otra pestaña sigue leyendo los conjuntos.
        Assert.Equal(1, StoredData.SuitesVersion);
        Assert.Equal(3, StoredData.RollsVersion);
        Assert.Contains("\"schemaVersion\": 1", StoredData.Write(SampleSuites()), StringComparison.Ordinal);
        Assert.Contains("\"schemaVersion\": 3", StoredData.Write(SampleRolls()), StringComparison.Ordinal);
    }

    [Fact]
    public void Suites_Of_A_Newer_Version_Are_Rejected_With_A_Reason__FR018()
    {
        var read = StoredData.ReadSuites("{ \"schemaVersion\": 2, \"suites\": [] }");

        Assert.Null(read.Value);
        Assert.Contains("más reciente", read.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void Dice3D_Option_Is_Written_And_Read_Back__FR009()
    {
        var read = StoredData.ReadRolls(StoredData.Write(SampleRolls() with { Dice3D = true }));

        Assert.True(read.Value!.Dice3D);
        Assert.Contains("\"dice3d\": true", StoredData.Write(SampleRolls() with { Dice3D = true }), StringComparison.Ordinal);
    }

    [Fact]
    public void Version_1_History_Is_Migrated_Keeping_Every_Roll_And_Dice3D_Off__F003()
    {
        const string Version1 = """
            { "schemaVersion": 1, "penetrating": true, "rolls": [
              { "formula": "1d20+2", "penetrating": false, "total": 17, "rolledAt": "2026-11-18T10:05:00+01:00", "constant": 2,
                "groups": [ { "count": 1, "sides": 20, "sign": 1, "subtotal": 15, "capped": false, "dice": [ { "value": 15, "penetration": false } ] } ] } ] }
            """;

        var read = StoredData.ReadRolls(Version1);

        Assert.Null(read.Error);
        Assert.True(read.Value!.Penetrating);
        Assert.False(read.Value.Dice3D);
        Assert.Equal(17, Assert.Single(read.Value.Rolls).Total);
        Assert.Equal(StoredData.RollsVersion, read.Value.SchemaVersion);
    }

    private const string Version2 = """
        { "schemaVersion": 2, "penetrating": true, "dice3d": true, "rolls": [
          { "formula": "1d20+2", "penetrating": false, "total": 17, "rolledAt": "2026-11-18T10:05:00+01:00", "constant": 2,
            "groups": [ { "count": 1, "sides": 20, "sign": 1, "subtotal": 15, "capped": false, "dice": [ { "value": 15, "penetration": false } ] } ] } ] }
        """;

    [Fact]
    public void Version_2_History_Is_Migrated_Keeping_Every_Roll_With_The_Default_Look__F008_FR012()
    {
        var read = StoredData.ReadRolls(Version2);

        Assert.Null(read.Error);
        Assert.True(read.Value!.Dice3D);
        Assert.Equal(17, Assert.Single(read.Value.Rolls).Total);
        Assert.Equal(DiceLook.Default, read.Value.Look);
        Assert.Equal(StoredData.RollsVersion, read.Value.SchemaVersion);
    }

    [Fact]
    public void Version_1_History_Is_Migrated_Through_Version_2_To_The_Default_Look__F008_FR012()
    {
        var read = StoredData.ReadRolls("""{ "schemaVersion": 1, "penetrating": false, "rolls": [] }""");

        Assert.Null(read.Error);
        Assert.Equal(DiceLook.Default, read.Value!.Look);
    }

    [Fact]
    public void The_Look_Is_Written_And_Read_Back__F008_FR012()
    {
        var look = DiceLook.Default.ForMaterial(DiceMaterial.Marble) with { Effect = PenetrationEffect.Fire, Sound = false, Quality = DiceQuality.Low };

        var json = StoredData.Write(SampleRolls() with { Look = look });

        Assert.Contains("\"look\":", json, StringComparison.Ordinal);
        Assert.Contains("\"material\": \"Marble\"", json, StringComparison.Ordinal);
        Assert.Equal(look, StoredData.ReadRolls(json).Value!.Look);
    }

    [Theory]
    [InlineData("""{ "material": "Plastico", "color": "#2f5a46", "ink": "#f1e5c4", "effect": "Fire", "sound": false, "quality": "Low" }""", "material")]
    [InlineData("""{ "material": "Marble", "color": "verde", "ink": "#f1e5c4", "effect": "Fire", "sound": false, "quality": "Low" }""", "color")]
    [InlineData("""{ "material": "Marble", "color": "#2f5a46", "ink": "#f1e5c4", "effect": "Lightning", "sound": false, "quality": "Low" }""", "effect")]
    [InlineData("""{ "material": "Marble", "color": "#2f5a46", "ink": "#f1e5c4", "effect": "Fire", "sound": "sí", "quality": "Ultra" }""", "sound+quality")]
    [InlineData("""null""", "all")]
    [InlineData("""[1, 2]""", "all")]
    public void A_Broken_Look_Is_Completed_With_Defaults_Without_Losing_Rolls__F008_FR012(string look, string broken)
    {
        var json = Version2.Replace("\"schemaVersion\": 2,", "\"schemaVersion\": 3, \"look\": " + look + ",", StringComparison.Ordinal);

        var read = StoredData.ReadRolls(json);

        Assert.Null(read.Error);
        Assert.Single(read.Value!.Rolls);
        var value = read.Value.Look!;
        switch (broken)
        {
            case "material":
                Assert.Equal(DiceMaterial.Ivory, value.Material);
                Assert.Equal(PenetrationEffect.Fire, value.Effect);
                break;
            case "color":
                Assert.Equal((DiceMaterial.Marble, "#2f5a46"), (value.Material, value.Color));
                break;
            case "effect":
                Assert.Equal((DiceMaterial.Marble, PenetrationEffect.Explosion), (value.Material, value.Effect));
                break;
            case "sound+quality":
                Assert.Equal((true, DiceQuality.High, PenetrationEffect.Fire), (value.Sound, value.Quality, value.Effect));
                break;
            default:
                Assert.Equal(DiceLook.Default, value);
                break;
        }
    }

    [Fact]
    public void Version_1_Suites_Are_Read_Unchanged__F003()
    {
        const string Version1 = """
            { "schemaVersion": 1, "suites": [ { "slug": "mio", "name": "Mío",
              "components": [ { "kind": "Skill", "slug": "botanica" }, { "kind": "Skill", "slug": "lucha" } ],
              "abilities": ["INT"], "createdAt": "2026-11-18T10:00:00+01:00", "updatedAt": "2026-11-18T10:00:00+01:00" } ] }
            """;

        var read = StoredData.ReadSuites(Version1);

        Assert.Null(read.Error);
        Assert.Equal("Mío", Assert.Single(read.Value!.Suites).Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Nothing_Stored_Yet_Gives_Empty_Data_Without_Error__FR017(string? json)
    {
        var suites = StoredData.ReadSuites(json);
        var rolls = StoredData.ReadRolls(json);

        Assert.Null(suites.Error);
        Assert.Empty(suites.Value!.Suites);
        Assert.Null(rolls.Error);
        Assert.Empty(rolls.Value!.Rolls);
        Assert.False(rolls.Value.Penetrating);
    }

    [Theory]
    [InlineData("{ \"suites\": [] }")]
    [InlineData("{ \"schemaVersion\": \"uno\", \"suites\": [] }")]
    public void Data_Without_A_Valid_Schema_Version_Is_Rejected__FR018(string json)
    {
        var read = StoredData.ReadSuites(json);

        Assert.Null(read.Value);
        Assert.Contains("versión", read.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ \"schemaVersion\": 1, \"penetrating\": false, \"rolls\": [null] }")]
    [InlineData("{ \"schemaVersion\": 1, \"penetrating\": false, \"rolls\": [ { \"formula\": \"1d6\", \"penetrating\": false, \"total\": 3, \"rolledAt\": \"2026-11-18T10:00:00+01:00\", \"constant\": 0, \"groups\": null } ] }")]
    public void Rolls_With_Missing_Parts_Are_Rejected_Without_Throwing__FR019(string json)
    {
        var read = StoredData.ReadRolls(json);

        Assert.Null(read.Value);
        Assert.False(string.IsNullOrEmpty(read.Error));
    }

    [Fact]
    public void Data_From_A_Newer_Version_Is_Rejected_With_A_Reason__FR018()
    {
        var read = StoredData.ReadRolls("{ \"schemaVersion\": 4, \"penetrating\": false, \"rolls\": [] }");

        Assert.Null(read.Value);
        Assert.Contains("más reciente", read.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ esto no es json")]
    [InlineData("[1, 2, 3]")]
    [InlineData("{ \"schemaVersion\": 1, \"suites\": 5 }")]
    [InlineData("{ \"schemaVersion\": 1, \"suites\": [null] }")]
    [InlineData("{ \"schemaVersion\": 1, \"suites\": [ { \"slug\": \"a\", \"name\": \"A\", \"components\": null, \"abilities\": [], \"createdAt\": \"2026-11-18T10:00:00+01:00\", \"updatedAt\": \"2026-11-18T10:00:00+01:00\" } ] }")]
    public void Damaged_Data_Is_Rejected_Without_Throwing__FR019(string json)
    {
        var read = StoredData.ReadSuites(json);

        Assert.Null(read.Value);
        Assert.False(string.IsNullOrEmpty(read.Error));
    }
}
