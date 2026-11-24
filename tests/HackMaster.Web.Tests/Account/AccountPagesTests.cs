using Bunit;
using HackMaster.Web.Account;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests.Account;

/// <summary>Pantallas de la cuenta (F006; contracts/ui.md) con un servicio de cuentas falso.</summary>
public class AccountPagesTests : BunitContext
{
    private readonly FakeAccountSync _sync = new();

    public AccountPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IAccountSync>(_sync);
    }

    [Fact]
    public void Without_Configuration_Nothing_Is_Shown__F006_FR012()
    {
        _sync.State = AccountState.NotConfigured;

        var cut = Render<LoginDisplay>();

        Assert.Empty(cut.FindAll("[data-testid=account-menu]"));
    }

    [Fact]
    public void Signed_Out_Shows_Sign_In__F006_FR001()
    {
        _sync.State = AccountState.SignedOut;
        var cut = Render<LoginDisplay>();

        cut.Find("[data-testid=sign-in]").Click();

        Assert.Equal(1, _sync.SignIns);
    }

    [Fact]
    public void Signed_In_Shows_The_Name_And_Sign_Out__F006_FR001()
    {
        _sync.SignIn("Ana");
        var cut = Render<LoginDisplay>();

        Assert.Contains("Ana", cut.Find("[data-testid=account-name]").TextContent);
        Assert.Equal("Sincronizado", cut.Find("[data-testid=account-name]").GetAttribute("title"));
        cut.Find("[data-testid=sign-out]").Click();
        Assert.Equal(1, _sync.SignOuts);
    }

    [Fact]
    public void Header_Follows_State_Changes__F006_FR001()
    {
        _sync.State = AccountState.SignedOut;
        var cut = Render<LoginDisplay>();

        _sync.SignIn("Ana");
        _sync.RaiseChanged();

        cut.WaitForAssertion(() => Assert.Contains("Ana", cut.Find("[data-testid=account-name]").TextContent));
    }

    [Fact]
    public void Not_Invited_Shows_The_Notice__F006_FR003()
    {
        _sync.State = AccountState.NotInvited;
        _sync.AccountId = "oid-de-prueba";

        var cut = Render<AccountNotices>();

        Assert.Contains("no está invitada", cut.Find("[data-testid=not-invited]").TextContent);
        Assert.Contains("oid-de-prueba", cut.Find("[data-testid=not-invited]").TextContent);
        Assert.Equal(1, _sync.Initializations);
    }

    [Theory]
    [InlineData(SyncStatus.Pending, "sync-pending")]
    [InlineData(SyncStatus.Failed, "sync-failed")]
    public void Sync_Problems_Are_Shown__F006_FR005(SyncStatus status, string testId)
    {
        _sync.SignIn("Ana");
        _sync.Status = status;

        var cut = Render<AccountNotices>();

        Assert.Single(cut.FindAll($"[data-testid={testId}]"));
    }

    [Fact]
    public void Conflict_Warnings_Are_Shown_And_Can_Be_Closed__F006_FR007()
    {
        _sync.SignIn("Ana");
        var cut = Render<AccountNotices>();

        _sync.RaiseWarning("Se ha quedado la versión más reciente de «Brunilda».");
        cut.WaitForAssertion(() => Assert.Contains("Brunilda", cut.Find("[data-testid=sync-warning]").TextContent));
        cut.Find("[data-testid=sync-warning] button").Click();

        Assert.Empty(cut.FindAll("[data-testid=sync-warning]"));
    }

    [Theory]
    [InlineData(false, "subes a tu cuenta")]
    [InlineData(true, "ya tiene datos")]
    public void First_Session_Asks_What_To_Do_With_Device_Data__F006_US4(bool accountHasData, string text)
    {
        _sync.SignIn("Ana");
        _sync.NeedsDeviceChoice = true;
        _sync.AccountHasData = accountHasData;

        var cut = Render<AccountNotices>();

        Assert.Contains(text, cut.Find("[data-testid=device-choice]").TextContent);
        cut.Find("[data-testid=device-upload]").Click();
        Assert.Equal(1, _sync.Uploads);
    }

    [Fact]
    public void Keeping_Device_Data_Apart_Is_An_Option__F006_FR010()
    {
        _sync.SignIn("Ana");
        _sync.NeedsDeviceChoice = true;
        var cut = Render<AccountNotices>();

        cut.Find("[data-testid=device-keep]").Click();

        Assert.Equal(1, _sync.Keeps);
        Assert.Equal(0, _sync.Uploads);
    }

    [Fact]
    public void Account_Page_Without_Session_Invites_To_Sign_In__F006_US5()
    {
        _sync.State = AccountState.SignedOut;

        var cut = Render<AccountPage>();

        Assert.Single(cut.FindAll("[data-testid=account-signed-out]"));
    }

    [Fact]
    public void Account_Page_Downloads_The_Data__F006_FR011()
    {
        _sync.SignIn("Ana");
        var cut = Render<AccountPage>();

        cut.Find("[data-testid=account-download]").Click();

        Assert.Equal(1, _sync.Downloads);
        Assert.Equal("Descargado.", cut.Find("[data-testid=account-message]").TextContent);
    }

    [Fact]
    public void Account_Is_Deleted_Only_After_Typing_BORRAR__F006_FR011()
    {
        _sync.SignIn("Ana");
        var cut = Render<AccountPage>();
        Assert.True(cut.Find("[data-testid=account-delete]").HasAttribute("disabled"));

        cut.Find("[data-testid=account-delete-confirm]").Input("borrar");
        Assert.True(cut.Find("[data-testid=account-delete]").HasAttribute("disabled"));

        cut.Find("[data-testid=account-delete-confirm]").Input("BORRAR");
        cut.Find("[data-testid=account-delete]").Click();

        Assert.Equal(1, _sync.Deletions);
        Assert.Contains("se quedan", cut.Find("[data-testid=account-message]").TextContent);
    }

    [Fact]
    public void Invitations_Link_Only_For_Admins__F006_FR003()
    {
        _sync.SignIn("Ana");
        Assert.Empty(Render<AccountPage>().FindAll("[data-testid=account-invitations]"));

        _sync.IsAdmin = true;
        Assert.Single(Render<AccountPage>().FindAll("[data-testid=account-invitations]"));
    }

    [Fact]
    public void Invitations_Page_Is_Only_For_Admins__F006_FR003()
    {
        _sync.SignIn("Ana");

        var cut = Render<Invitations>();

        Assert.Single(cut.FindAll("[data-testid=invitations-denied]"));
        Assert.Equal(0, _sync.InvitationLists);
    }

    [Fact]
    public void Admin_Invites_And_Removes__F006_FR003()
    {
        _sync.SignIn("Admin");
        _sync.IsAdmin = true;
        _sync.InvitationList.Add(new InvitationInfo("bo@pruebas.example", DateTimeOffset.UnixEpoch));
        var cut = Render<Invitations>();
        Assert.Contains("bo@pruebas.example", cut.Find("[data-testid=invitation-list]").TextContent);

        cut.Find("[data-testid=invite-email]").Input("cid@pruebas.example");
        cut.Find("form").Submit();
        cut.WaitForAssertion(() => Assert.Contains("cid@pruebas.example", cut.Find("[data-testid=invitation-list]").TextContent));

        cut.FindAll("[data-testid=uninvite]")[0].Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("bo@pruebas.example", cut.Find("[data-testid=invitation-list]").TextContent));
    }

    [Fact]
    public void Invalid_Invitation_Shows_Why__F006_FR003()
    {
        _sync.SignIn("Admin");
        _sync.IsAdmin = true;
        var cut = Render<Invitations>();

        cut.Find("[data-testid=invite-email]").Input("no-es-un-correo");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => Assert.Equal("Ese correo no es válido.", cut.Find("[data-testid=invitations-message]").TextContent));
    }
}

/// <summary>Servicio de cuentas falso para probar las pantallas.</summary>
public sealed class FakeAccountSync : IAccountSync
{
    public AccountState State { get; set; } = AccountState.SignedOut;

    public SyncStatus Status { get; set; } = SyncStatus.Synced;

    public string? Name { get; set; }

    public string? AccountId { get; set; }

    public bool IsAdmin { get; set; }

    public bool NeedsDeviceChoice { get; set; }

    public bool AccountHasData { get; set; }

    public List<InvitationInfo> InvitationList { get; } = [];

    public int SignIns { get; private set; }

    public int SignOuts { get; private set; }

    public int Initializations { get; private set; }

    public int Uploads { get; private set; }

    public int Keeps { get; private set; }

    public int Downloads { get; private set; }

    public int Deletions { get; private set; }

    public int InvitationLists { get; private set; }

    public event Action? Changed;

    public event Action<string>? Warning;

    public void SignIn(string name)
    {
        State = AccountState.SignedIn;
        Name = name;
    }

    public void RaiseChanged() => Changed?.Invoke();

    public void RaiseWarning(string text) => Warning?.Invoke(text);

    public Task InitializeAsync()
    {
        Initializations++;
        return Task.CompletedTask;
    }

    public void StartRetries()
    {
    }

    public Task SignInAsync()
    {
        SignIns++;
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        SignOuts++;
        State = AccountState.SignedOut;
        return Task.CompletedTask;
    }

    public Task SyncNowAsync() => Task.CompletedTask;

    public Task ThemeChangedAsync(string mode) => Task.CompletedTask;

    public Task UploadDeviceDataAsync()
    {
        Uploads++;
        NeedsDeviceChoice = false;
        return Task.CompletedTask;
    }

    public Task KeepDeviceDataAsync()
    {
        Keeps++;
        NeedsDeviceChoice = false;
        return Task.CompletedTask;
    }

    public Task<string?> DownloadAccountDataAsync()
    {
        Downloads++;
        return Task.FromResult<string?>(null);
    }

    public Task<string?> DeleteAccountAsync()
    {
        Deletions++;
        return Task.FromResult<string?>(null);
    }

    public Task<IReadOnlyList<InvitationInfo>?> ListInvitationsAsync()
    {
        InvitationLists++;
        return Task.FromResult<IReadOnlyList<InvitationInfo>?>([.. InvitationList]);
    }

    public Task<string?> InviteAsync(string email)
    {
        if (!email.Contains('@', StringComparison.Ordinal))
        {
            return Task.FromResult<string?>("Ese correo no es válido.");
        }

        InvitationList.Add(new InvitationInfo(email, DateTimeOffset.UnixEpoch));
        return Task.FromResult<string?>(null);
    }

    public Task<string?> UninviteAsync(string email)
    {
        InvitationList.RemoveAll(i => i.Email == email);
        return Task.FromResult<string?>(null);
    }
}
