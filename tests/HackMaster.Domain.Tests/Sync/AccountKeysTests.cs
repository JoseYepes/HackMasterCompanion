using HackMaster.Domain.Sync;

namespace HackMaster.Domain.Tests.Sync;

public class AccountKeysTests
{
    [Fact]
    public void Without_Session_Keys_Stay_As_Today__F006_FR012()
    {
        Assert.Equal("hackmaster.characters", AccountKeys.For("hackmaster.characters", null));
    }

    [Fact]
    public void With_Session_Keys_Carry_The_Account__F006_FR009()
    {
        Assert.Equal("hackmaster.u.abc-123.characters", AccountKeys.For("hackmaster.characters", "abc-123"));
        Assert.Equal("hackmaster.u.abc-123.theme", AccountKeys.For("hackmaster.theme", "abc-123"));
    }

    [Fact]
    public void Each_Account_Has_Its_Own_Sync_State_Key__F006_FR009()
    {
        Assert.Equal("hackmaster.u.abc-123.sync", AccountKeys.SyncState("abc-123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("a.b")]
    [InlineData("a/b")]
    public void Invalid_Account_Ids_Are_Rejected__F006_FR009(string id)
    {
        Assert.False(AccountKeys.IsValidAccountId(id));
        Assert.Throws<ArgumentException>(() => AccountKeys.For("hackmaster.characters", id));
    }

    [Fact]
    public void Only_Application_Keys_Are_Accepted__F006_FR009()
    {
        Assert.Throws<ArgumentException>(() => AccountKeys.For("otra.cosa", "abc"));
    }
}
