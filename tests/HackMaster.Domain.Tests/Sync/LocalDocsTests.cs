using System.Text.Json.Nodes;
using HackMaster.Domain.Sync;

namespace HackMaster.Domain.Tests.Sync;

public class LocalDocsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = T0.AddHours(1);

    private static Character Pj(string id, string name, DateTimeOffset? updated = null) =>
        new() { Id = id, Name = name, CreatedAt = T0, UpdatedAt = updated ?? T0 };

    private static PlayerSuite Suite(string slug, string name) =>
        new() { Slug = slug, Name = name, CreatedAt = T0, UpdatedAt = T0.AddMinutes(3) };

    private static string Characters(params Character[] characters) =>
        StoredData.Write(new CharactersFile(StoredData.CharactersVersion, characters));

    private static string Suites(params PlayerSuite[] suites) =>
        StoredData.Write(new PlayerSuitesFile(StoredData.SuitesVersion, suites));

    [Fact]
    public void Characters_Split_One_Doc_Each_With_Their_Date__F006_ResearchR4()
    {
        var docs = LocalDocs.Split(LocalDocs.CharactersKey, Characters(Pj("c1", "Ana"), Pj("c2", "Bo", T0.AddDays(1))), Now)!;

        Assert.Equal(["character:c1", "character:c2"], docs.Select(d => d.Id));
        Assert.All(docs, d => Assert.Equal(StoredData.CharactersVersion, d.SchemaVersion));
        Assert.Equal(T0.AddDays(1), docs[1].UpdatedAt);
        Assert.Equal("Bo", docs[1].Label);
    }

    [Fact]
    public void Suites_Split_By_Slug__F006_ResearchR4()
    {
        var doc = Assert.Single(LocalDocs.Split(LocalDocs.SuitesKey, Suites(Suite("mi-conjunto", "Mi conjunto")), Now)!);

        Assert.Equal("suite:mi-conjunto", doc.Id);
        Assert.Equal(T0.AddMinutes(3), doc.UpdatedAt);
        Assert.Equal("Mi conjunto", doc.Label);
    }

    [Fact]
    public void Rolls_And_Theme_Are_One_Doc_Each__F006_FR004()
    {
        var rolls = StoredData.Write(RollHistoryFile.Empty);
        var theme = """{"schemaVersion":1,"mode":"dark"}""";

        var rollDoc = Assert.Single(LocalDocs.Split(LocalDocs.RollsKey, rolls, Now)!);
        var themeDoc = Assert.Single(LocalDocs.Split(LocalDocs.ThemeKey, theme, Now)!);

        Assert.Equal("rolls:main", rollDoc.Id);
        Assert.Equal(Now, rollDoc.UpdatedAt);
        Assert.Equal("theme:main", themeDoc.Id);
        Assert.Equal(theme, themeDoc.Payload);
    }

    [Fact]
    public void Nothing_Saved_Splits_To_No_Docs_And_Unreadable_To_Null__F006_Edge_Unreadable()
    {
        Assert.Empty(LocalDocs.Split(LocalDocs.CharactersKey, null, Now)!);
        Assert.Null(LocalDocs.Split(LocalDocs.CharactersKey, "basura", Now));
        Assert.Null(LocalDocs.Split(LocalDocs.ThemeKey, """{"schemaVersion":1,"mode":"rosa"}""", Now));
        Assert.Null(LocalDocs.Split("hackmaster.otra", "{}", Now));
    }

    [Fact]
    public void Changes_Give_New_Edited_And_Deleted_Items__F006_FR005()
    {
        var before = Characters(Pj("c1", "Ana"), Pj("c2", "Bo"), Pj("c3", "Cid"));
        var after = Characters(Pj("c1", "Ana"), Pj("c2", "Bo el Bravo"), Pj("c4", "Dax"));

        var changes = LocalDocs.Changes(LocalDocs.CharactersKey, before, after, Now);

        Assert.Equal(["character:c2", "character:c4", "character:c3"], changes.Select(d => d.Id));
        Assert.All(changes, d => Assert.Equal(Now, d.UpdatedAt));
        Assert.True(changes[2].Deleted);
        Assert.Null(changes[2].Payload);
    }

    [Fact]
    public void Unreadable_Files_Give_No_Changes__F006_Edge_Unreadable()
    {
        Assert.Empty(LocalDocs.Changes(LocalDocs.CharactersKey, Characters(Pj("c1", "Ana")), "basura", Now));
    }

    [Fact]
    public void Apply_Adds_Replaces_And_Removes_Characters__F006_FR006()
    {
        var local = Characters(Pj("c1", "Ana"), Pj("c2", "Bo"));
        var incoming = LocalDocs.Split(LocalDocs.CharactersKey, Characters(Pj("c2", "Bo el Bravo"), Pj("c3", "Cid")), Now)!
            .Append(new SyncDoc("character:c1", SyncKinds.Character, StoredData.CharactersVersion, Now, true, null));

        var result = StoredData.ReadCharacters(LocalDocs.Apply(LocalDocs.CharactersKey, local, incoming));

        Assert.Equal(["Bo el Bravo", "Cid"], result.Value!.Characters.Select(c => c.Name));
    }

    [Fact]
    public void Apply_Migrates_Older_Character_Payloads__F006_Edge_SchemaVersion()
    {
        var v1 = JsonNode.Parse(StoredData.Write(new CharactersFile(1, [Pj("c1", "Antigua")])))!["characters"]![0]!.ToJsonString();
        var doc = new SyncDoc("character:c1", SyncKinds.Character, 1, Now, false, v1);

        var result = StoredData.ReadCharacters(LocalDocs.Apply(LocalDocs.CharactersKey, null, [doc]));

        Assert.Null(result.Error);
        Assert.Equal("Antigua", Assert.Single(result.Value!.Characters).Name);
    }

    [Fact]
    public void Apply_Skips_Unreadable_Payloads_Without_Losing_The_Rest__F006_Edge_Unreadable()
    {
        var local = Suites(Suite("a", "A"));
        var bad = new SyncDoc("suite:b", SyncKinds.Suite, 1, Now, false, "basura");

        var result = StoredData.ReadSuites(LocalDocs.Apply(LocalDocs.SuitesKey, local, [bad]));

        Assert.Equal(["A"], result.Value!.Suites.Select(s => s.Name));
    }

    [Fact]
    public void Apply_Replaces_Whole_Rolls_And_Theme__F006_FR006()
    {
        var rolls = StoredData.Write(RollHistoryFile.Empty with { Penetrating = true });
        var rollDoc = Assert.Single(LocalDocs.Split(LocalDocs.RollsKey, rolls, Now)!);
        var themeDoc = new SyncDoc("theme:main", SyncKinds.Theme, 1, Now, false, """{"schemaVersion":1,"mode":"dark"}""");

        Assert.True(StoredData.ReadRolls(LocalDocs.Apply(LocalDocs.RollsKey, null, [rollDoc])).Value!.Penetrating);
        Assert.Equal(themeDoc.Payload, LocalDocs.Apply(LocalDocs.ThemeKey, null, [themeDoc]));
    }

    [Fact]
    public void Apply_Ignores_Docs_Of_Other_Kinds__F006_DataModel()
    {
        var local = Characters(Pj("c1", "Ana"));
        var theme = new SyncDoc("theme:main", SyncKinds.Theme, 1, Now, false, "{}");

        Assert.Equal(local, LocalDocs.Apply(LocalDocs.CharactersKey, local, [theme]));
    }

    [Fact]
    public void Kind_Maps_To_Its_Key__F006_DataModel()
    {
        Assert.Equal(LocalDocs.CharactersKey, LocalDocs.KeyForKind(SyncKinds.Character));
        Assert.Equal(LocalDocs.ThemeKey, LocalDocs.KeyForKind(SyncKinds.Theme));
        Assert.Null(LocalDocs.KeyForKind("otro"));
    }
}
