using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Formato de lo que se guarda en el dispositivo y sus versiones (FR-018, principio V).</summary>
public class StoredDataTests
{
    private static readonly DateTimeOffset At = new(2026, 11, 18, 10, 0, 0, TimeSpan.FromHours(1));

    private static PlayerSuitesFile SampleSuites() => new(StoredData.CurrentVersion,
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

    private static RollHistoryFile SampleRolls() => new(StoredData.CurrentVersion, Penetrating: true,
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
    public void Written_Data_Carries_The_Schema_Version__FR018()
    {
        Assert.Contains("\"schemaVersion\": 1", StoredData.Write(SampleSuites()), StringComparison.Ordinal);
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
        var read = StoredData.ReadRolls("{ \"schemaVersion\": 2, \"penetrating\": false, \"rolls\": [] }");

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
