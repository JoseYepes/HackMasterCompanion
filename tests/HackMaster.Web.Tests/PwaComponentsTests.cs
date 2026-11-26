using Bunit;
using HackMaster.Web.Account;
using HackMaster.Web.Components;
using HackMaster.Web.Pages;
using HackMaster.Web.Tests.Account;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>
/// Avisos de la aplicación instalable (F013, specs/011-instalable-sin-conexion/contracts/pwa.md):
/// botón «Instalar» y nota de iPhone, aviso de versión nueva e inicio de sesión sin conexión. El
/// script <c>js/pwa.js</c> se simula con la interoperabilidad de bUnit.
/// </summary>
public class PwaComponentsTests : BunitContext
{
    public PwaComponentsTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private static PwaState State(bool updateReady = false, bool canInstall = false, bool isIos = false, bool isStandalone = false) =>
        new(updateReady, canInstall, isIos, isStandalone);

    [Fact]
    public void Install_Prompt_Shows_Nothing_Until_The_Script_Says_So__FR010()
    {
        var cut = Render<InstallPrompt>();

        Assert.Empty(cut.FindAll("[data-testid=install]"));
        Assert.Empty(cut.FindAll("[data-testid=install-ios]"));
        Assert.Single(JSInterop.Invocations["hackmasterPwa.subscribe"]);
    }

    [Fact]
    public async Task Install_Button_Opens_The_Browser_Prompt__FR010()
    {
        var cut = Render<InstallPrompt>();

        await cut.InvokeAsync(() => cut.Instance.OnPwaState(State(canInstall: true)));
        cut.Find("[data-testid=install]").Click();

        Assert.Single(JSInterop.Invocations["hackmasterPwa.install"]);
    }

    [Fact]
    public async Task Iphone_Shows_How_To_Add_To_The_Home_Screen__FR010()
    {
        var cut = Render<InstallPrompt>();

        await cut.InvokeAsync(() => cut.Instance.OnPwaState(State(isIos: true)));

        var note = cut.Find("[data-testid=install-ios]").TextContent;
        Assert.Contains("Compartir", note, StringComparison.Ordinal);
        Assert.Contains("Añadir a pantalla de inicio", note, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=install]"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task Installed_App_Shows_Neither_Button_Nor_Note__FR010(bool canInstall, bool isIos)
    {
        var cut = Render<InstallPrompt>();

        await cut.InvokeAsync(() => cut.Instance.OnPwaState(State(canInstall: canInstall, isIos: isIos, isStandalone: true)));

        Assert.Empty(cut.FindAll("[data-testid=install]"));
        Assert.Empty(cut.FindAll("[data-testid=install-ios]"));
    }

    [Fact]
    public void Home_Page_Has_The_Install_Prompt__FR010()
    {
        var cut = Render<Home>();

        Assert.Single(cut.FindComponents<InstallPrompt>());
    }

    [Fact]
    public async Task Install_Prompt_Stops_Listening_When_Removed__ContractsPwa()
    {
        JSInterop.Setup<int>("hackmasterPwa.subscribe", _ => true).SetResult(7);
        Render<InstallPrompt>();

        await DisposeComponentsAsync();

        Assert.Equal(7, JSInterop.Invocations["hackmasterPwa.unsubscribe"].Single().Arguments[0]);
    }

    [Fact]
    public void Update_Notice_Is_Hidden_Without_A_New_Version__FR008()
    {
        var cut = Render<UpdateNotice>();

        Assert.Empty(cut.FindAll("[data-testid=update-ready]"));
    }

    [Fact]
    public async Task Update_Notice_Offers_To_Update__FR008()
    {
        var cut = Render<UpdateNotice>();

        await cut.InvokeAsync(() => cut.Instance.OnPwaState(State(updateReady: true)));

        Assert.Contains("Hay una versión nueva", cut.Find("[data-testid=update-ready]").TextContent, StringComparison.Ordinal);
        cut.Find("[data-testid=update-apply]").Click();
        Assert.Single(JSInterop.Invocations["hackmasterPwa.applyUpdate"]);
    }

    [Fact]
    public void Layout_Has_The_Update_Notice__FR008()
    {
        Services.AddSingleton(TestData.Create());
        Services.AddSingleton<AccountSession>();
        Services.AddSingleton<BrowserStore>();

        var cut = Render<HackMaster.Web.Layout.MainLayout>();

        Assert.Single(cut.FindComponents<UpdateNotice>());
    }

    [Fact]
    public void Sign_In_Without_Connection_Explains_It_Needs_One__FR006()
    {
        var sync = new FakeAccountSync { State = AccountState.SignedOut };
        Services.AddSingleton<IAccountSync>(sync);
        JSInterop.Setup<bool>("hackmasterPwa.isOffline").SetResult(true);
        var cut = Render<LoginDisplay>();

        cut.Find("[data-testid=sign-in]").Click();

        Assert.Equal(0, sync.SignIns);
        Assert.Contains("hace falta conexión", cut.Find("[data-testid=sign-in-offline]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Sign_In_With_Connection_Goes_On_As_Before__FR006_F006_FR001()
    {
        var sync = new FakeAccountSync { State = AccountState.SignedOut };
        Services.AddSingleton<IAccountSync>(sync);
        JSInterop.Setup<bool>("hackmasterPwa.isOffline").SetResult(false);
        var cut = Render<LoginDisplay>();

        cut.Find("[data-testid=sign-in]").Click();

        Assert.Equal(1, sync.SignIns);
        Assert.Empty(cut.FindAll("[data-testid=sign-in-offline]"));
    }
}
