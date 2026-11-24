using HackMaster.Domain.Sync;

namespace HackMaster.Domain.Tests.Sync;

public class SyncStateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

    private static SyncDoc Suite(string slug, DateTimeOffset at) =>
        new(SyncDoc.MakeId(SyncKinds.Suite, slug), SyncKinds.Suite, StoredData.SuitesVersion, at, false, $$"""{"slug":"{{slug}}","name":"{{slug}}"}""");

    [Fact]
    public void Only_The_Last_Change_Per_Item_Stays_Pending__F006_FR005()
    {
        var state = SyncState.Empty.WithPending(Suite("a", T0)).WithPending(Suite("b", T0)).WithPending(Suite("a", T0.AddMinutes(1)));

        Assert.Equal(2, state.Pending.Count);
        Assert.Equal(T0.AddMinutes(1), state.Pending.Single(d => d.Id == "suite:a").UpdatedAt);
    }

    [Fact]
    public void Pending_Changes_Carry_The_Known_Version__F006_FR005()
    {
        var state = SyncState.Empty.WithKnown("suite:a", "e1").WithPending(Suite("a", T0));

        Assert.Equal("e1", Assert.Single(state.Pending).ETag);
    }

    [Fact]
    public void Sent_Changes_Leave_The_Queue_And_Update_The_Known_Version__F006_FR005()
    {
        var doc = Suite("a", T0);
        var state = SyncState.Empty.WithPending(doc).Sent(doc with { ETag = "e9" });

        Assert.Empty(state.Pending);
        Assert.Equal("e9", state.Known["suite:a"]);
    }

    [Fact]
    public void A_Newer_Pending_Change_Survives_An_Older_Send__F006_FR005()
    {
        var older = Suite("a", T0);
        var newer = Suite("a", T0.AddMinutes(1));
        var state = SyncState.Empty.WithPending(newer).Sent(older with { ETag = "e9" });

        var pending = Assert.Single(state.Pending);
        Assert.Equal(newer.UpdatedAt, pending.UpdatedAt);
        Assert.Equal("e9", pending.ETag);
    }

    [Fact]
    public void State_Round_Trips_With_Schema_Version__F006_PrincipleV()
    {
        var state = SyncState.Empty.WithKnown("suite:a", "e1").WithPending(Suite("b", T0)) with { LastSync = T0 };

        var json = SyncState.Write(state);
        var read = SyncState.Read(json);

        Assert.Contains("\"schemaVersion\":1", json);
        Assert.Null(read.Error);
        Assert.Equal("e1", read.Value!.Known["suite:a"]);
        Assert.Equal("suite:b", Assert.Single(read.Value.Pending).Id);
        Assert.Equal(T0, read.Value.LastSync);
    }

    [Fact]
    public void Missing_State_Reads_As_Empty__F006_PrincipleV()
    {
        var read = SyncState.Read(null);

        Assert.Null(read.Error);
        Assert.Empty(read.Value!.Pending);
    }

    [Theory]
    [InlineData("no es json")]
    [InlineData("""{"schemaVersion":2,"known":{},"pending":[]}""")]
    public void Unreadable_State_Is_Reported_Not_Emptied__F006_Edge_Unreadable(string json)
    {
        var read = SyncState.Read(json);

        Assert.Null(read.Value);
        Assert.NotNull(read.Error);
    }
}
