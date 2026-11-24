using HackMaster.Domain.Sync;

namespace HackMaster.Domain.Tests.Sync;

public class SyncMergeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private static SyncDoc Character(string id, string name, DateTimeOffset at, string? etag = null, bool deleted = false) =>
        new(SyncDoc.MakeId(SyncKinds.Character, id), SyncKinds.Character, StoredData.CharactersVersion, at, deleted,
            deleted ? null : $$"""{"id":"{{id}}","name":"{{name}}"}""", etag);

    [Fact]
    public void Newest_Change_Wins_A_Conflict__F006_FR007()
    {
        var local = Character("c1", "Local", T0.AddMinutes(5));
        var remote = Character("c1", "Nube", T0, etag: "e2");

        var result = SyncMerge.Resolve(local, remote);

        Assert.True(result.LocalWins);
        Assert.Equal("e2", result.Winner.ETag);
        Assert.Contains("Local", result.Warning);
    }

    [Fact]
    public void Remote_Wins_When_Newer_Or_Tied__F006_FR007()
    {
        var remote = Character("c1", "Nube", T0, etag: "e2");

        Assert.False(SyncMerge.Resolve(Character("c1", "Local", T0.AddMinutes(-1)), remote).LocalWins);
        var tie = SyncMerge.Resolve(Character("c1", "Local", T0), remote);
        Assert.False(tie.LocalWins);
        Assert.Same(remote, tie.Winner);
        Assert.Contains("Nube", tie.Warning);
    }

    [Fact]
    public void Newer_Deletion_Removes_The_Item__F006_FR007()
    {
        var local = Character("c1", "Local", T0.AddMinutes(5), deleted: true);
        var remote = Character("c1", "Nube", T0, etag: "e2");

        var result = SyncMerge.Resolve(local, remote);

        Assert.True(result.LocalWins);
        Assert.True(result.Winner.Deleted);
        Assert.Contains("Nube", result.Warning);
    }

    [Fact]
    public void Newer_Edit_Brings_A_Deleted_Item_Back__F006_FR007()
    {
        var local = Character("c1", "Local", T0.AddMinutes(5));
        var remote = Character("c1", "", T0, etag: "e2", deleted: true);

        var result = SyncMerge.Resolve(local, remote);

        Assert.True(result.LocalWins);
        Assert.False(result.Winner.Deleted);
    }

    [Fact]
    public void Incoming_Changes_Not_Pending_Replace_The_Local_Copy__F006_FR006()
    {
        var remote = Character("c1", "Nube", T0, etag: "e1");

        var result = SyncMerge.ApplyIncoming([remote], SyncState.Empty);

        Assert.Equal([remote], result.Apply);
        Assert.Equal("e1", result.State.Known[remote.Id]);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Pending_Change_Based_On_Known_Version_Is_Kept__F006_FR005()
    {
        var pending = Character("c1", "Local", T0.AddMinutes(5));
        var state = SyncState.Empty.WithKnown(pending.Id, "e1").WithPending(pending);
        var remote = Character("c1", "Nube", T0, etag: "e1");

        var result = SyncMerge.ApplyIncoming([remote], state);

        Assert.Empty(result.Apply);
        Assert.Single(result.State.Pending);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Pending_Change_That_Lost_Is_Replaced_And_Warned__F006_FR007()
    {
        var pending = Character("c1", "Local", T0);
        var state = SyncState.Empty.WithKnown(pending.Id, "e1").WithPending(pending);
        var remote = Character("c1", "Nube", T0.AddMinutes(5), etag: "e2");

        var result = SyncMerge.ApplyIncoming([remote], state);

        Assert.Equal([remote], result.Apply);
        Assert.Empty(result.State.Pending);
        Assert.Equal("e2", result.State.Known[remote.Id]);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Pending_Change_That_Won_Is_Resent_With_The_New_Version__F006_FR007()
    {
        var pending = Character("c1", "Local", T0.AddMinutes(9));
        var state = SyncState.Empty.WithKnown(pending.Id, "e1").WithPending(pending);
        var remote = Character("c1", "Nube", T0.AddMinutes(5), etag: "e2");

        var result = SyncMerge.ApplyIncoming([remote], state);

        Assert.Empty(result.Apply);
        Assert.Equal("e2", Assert.Single(result.State.Pending).ETag);
        Assert.Equal("e2", result.State.Known[pending.Id]);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Newer_Schema_Is_Ignored_With_A_Warning__F006_Edge_SchemaVersion()
    {
        var remote = Character("c1", "Futura", T0, etag: "e1") with { SchemaVersion = StoredData.CharactersVersion + 1 };

        var result = SyncMerge.ApplyIncoming([remote], SyncState.Empty);

        Assert.Empty(result.Apply);
        Assert.False(result.State.Known.ContainsKey(remote.Id));
        Assert.Contains("Futura", Assert.Single(result.Warnings));
    }

    [Fact]
    public void Unknown_Kinds_Are_Ignored_With_A_Warning__F006_Edge_Unreadable()
    {
        var remote = new SyncDoc("otro:x", "otro", 1, T0, false, "{}", "e1");

        var result = SyncMerge.ApplyIncoming([remote], SyncState.Empty);

        Assert.Empty(result.Apply);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public void Labels_Name_The_Item__F006_FR007()
    {
        Assert.Equal("Brunilda", Character("c1", "Brunilda", T0).Label);
        Assert.Equal("c1", Character("c1", "", T0, deleted: true).Label);
        Assert.Equal("el historial de tiradas", new SyncDoc(SyncDoc.MakeId(SyncKinds.Rolls, "main"), SyncKinds.Rolls, 3, T0, false, "{}").Label);
        Assert.Equal("el modo de pantalla", new SyncDoc(SyncDoc.MakeId(SyncKinds.Theme, "main"), SyncKinds.Theme, 1, T0, false, "{}").Label);
    }

    [Fact]
    public void Ids_Combine_Kind_And_Item__F006_DataModel()
    {
        Assert.Equal("character:c1", SyncDoc.MakeId(SyncKinds.Character, "c1"));
        Assert.Equal("c1", Character("c1", "x", T0).ItemId);
    }
}
