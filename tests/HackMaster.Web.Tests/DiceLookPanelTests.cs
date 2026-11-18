using System.Text.Json;
using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Components;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Panel «Aspecto de los dados» (F008 US1: FR-001 a FR-005, FR-009, FR-011, FR-012, FR-017).</summary>
public class DiceLookPanelTests : BunitContext
{
    private const string ModulePath = "./js/dice3d.js";

    private readonly BunitJSModuleInterop _module;
    private DiceLook? _changed;

    public DiceLookPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(ModulePath);
        _module.SetupVoid("preview", _ => true).SetVoidResult();
        _module.SetupVoid("stopPreview", _ => true).SetVoidResult();
    }

    private IRenderedComponent<DiceLookPanel> RenderOpen(DiceLook? look = null)
    {
        var cut = Render<DiceLookPanel>(parameters => parameters
            .Add(panel => panel.Look, look ?? DiceLook.Default)
            .Add(panel => panel.LookChanged, (DiceLook changed) => _changed = changed));
        cut.Find("[data-testid=look-open]").Click();
        return cut;
    }

    /// <summary>El aspecto que recibió la última vista previa, tal como llega a JavaScript.</summary>
    private JsonElement LastPreview()
    {
        var look = _module.Invocations["preview"].Last().Arguments[1];
        return JsonSerializer.SerializeToElement(look);
    }

    [Fact]
    public void The_Panel_Shows_The_11_Materials_In_Spanish_With_The_Current_One_Pressed__FR001()
    {
        var cut = RenderOpen();

        var buttons = cut.FindAll("[data-testid^=look-material-]");
        Assert.Equal(
            ["Marfil", "Mármol", "Oro viejo", "Hierro negro", "Rubí", "Obsidiana", "Hueso", "Amatista", "Madera", "Piedra", "Cristal"],
            buttons.Select(button => button.TextContent.Trim()));
        Assert.Equal("true", cut.Find("[data-testid=look-material-Ivory]").GetAttribute("aria-pressed"));
        Assert.Equal("false", cut.Find("[data-testid=look-material-Marble]").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void Opening_The_Panel_Shows_A_Preview_With_The_Current_Look__FR005()
    {
        RenderOpen(DiceLook.Default.ForMaterial(DiceMaterial.Ruby));

        _module.VerifyInvoke("preview");
        Assert.Equal("Ruby", LastPreview().GetProperty("material").GetString());
        Assert.Equal("#c0182a", LastPreview().GetProperty("color").GetString());
    }

    [Fact]
    public void Choosing_A_Material_Takes_Its_Colors_Updates_The_Preview_And_Is_Saved__FR001_FR005()
    {
        var cut = RenderOpen();

        cut.Find("[data-testid=look-material-Marble]").Click();

        Assert.Equal(DiceLook.Default.ForMaterial(DiceMaterial.Marble), _changed);
        Assert.Equal("Marble", LastPreview().GetProperty("material").GetString());
    }

    [Fact]
    public void Changing_The_Colors_Updates_The_Preview__FR002_FR005()
    {
        var cut = RenderOpen();

        cut.Find("[data-testid=look-color]").Change("#2244aa");

        Assert.Equal("#2244aa", _changed!.Color);
        Assert.Equal("#2244aa", LastPreview().GetProperty("color").GetString());
    }

    [Fact]
    public void Unreadable_Numbers_Show_A_Warning_And_A_Fix__FR003()
    {
        var cut = RenderOpen(DiceLook.Default with { Color = "#202020", Ink = "#303030" });

        Assert.Contains("no se leerán", cut.Find("[data-testid=look-contrast-warning]").TextContent, StringComparison.Ordinal);
        cut.Find("[data-testid=look-fix-ink]").Click();

        Assert.Equal(DiceLook.LightInk, _changed!.Ink);
    }

    [Fact]
    public void Readable_Numbers_Show_No_Warning__FR003()
    {
        var cut = RenderOpen();

        Assert.Empty(cut.FindAll("[data-testid=look-contrast-warning]"));
    }

    [Theory]
    [InlineData("look-effect-Fire", PenetrationEffect.Fire)]
    [InlineData("look-effect-Blood", PenetrationEffect.Blood)]
    [InlineData("look-effect-None", PenetrationEffect.None)]
    public void The_Penetration_Effect_Can_Be_Chosen__FR017(string testId, PenetrationEffect effect)
    {
        var cut = RenderOpen();

        cut.Find($"[data-testid={testId}]").Change(true);

        Assert.Equal(effect, _changed!.Effect);
    }

    [Fact]
    public void Sound_Starts_On_And_Can_Be_Muted__FR009()
    {
        var cut = RenderOpen();
        Assert.True(cut.Find("[data-testid=look-sound]").HasAttribute("checked"));

        cut.Find("[data-testid=look-sound]").Change(false);

        Assert.False(_changed!.Sound);
    }

    [Fact]
    public void Low_Quality_Can_Be_Chosen__FR011()
    {
        var cut = RenderOpen();

        cut.Find("[data-testid=look-quality-Low]").Change(true);

        Assert.Equal(DiceQuality.Low, _changed!.Quality);
    }

    [Fact]
    public void Controls_Have_Accessible_Names__FR016()
    {
        var cut = RenderOpen();

        Assert.Equal("true", cut.Find("[data-testid=look-open]").GetAttribute("aria-expanded"));
        Assert.Equal("Color del dado", cut.Find("label[for=look-color]").TextContent.Trim());
        Assert.Equal("Color de los números", cut.Find("label[for=look-ink]").TextContent.Trim());
    }

    [Fact]
    public void Closing_The_Panel_Stops_The_Preview__FR005()
    {
        var cut = RenderOpen();

        cut.Find("[data-testid=look-open]").Click();

        _module.VerifyInvoke("stopPreview");
        Assert.Empty(cut.FindAll("[data-testid^=look-material-]"));
    }
}

/// <summary>El panel dentro del tirador: solo con dados 3D y guardado en el historial (F008: FR-012).</summary>
public class DiceLookOnDicePageTests : BunitContext
{
    private readonly BunitJSModuleInterop _module;

    public DiceLookOnDicePageTests()
    {
        Services.AddSingleton<IRandom>(new LoopRandom(3, 4));
        Services.AddSingleton<BrowserStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule("./js/dice3d.js");
        _module.Setup<bool>("isSupported").SetResult(true);
        _module.SetupVoid("create", _ => true).SetVoidResult();
        _module.SetupVoid("preview", _ => true).SetVoidResult();
        _module.SetupVoid("stopPreview", _ => true).SetVoidResult();
        _module.SetupVoid("roll", _ => true).SetVoidResult();
        _module.SetupVoid("unlockAudio", _ => true).SetVoidResult();
    }

    private string LastSaved() => (string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!;

    [Fact]
    public void Without_3D_The_Panel_Is_Not_Shown__FR016()
    {
        var cut = Render<Dice>();

        cut.WaitForElement("[data-testid=dice3d-toggle]");
        Assert.Empty(cut.FindAll("[data-testid=look-open]"));
    }

    [Fact]
    public void The_Chosen_Look_Is_Saved_In_The_History_And_Sent_With_Each_Roll__FR012()
    {
        var stored = JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls");
        stored.SetResult("""{ "schemaVersion": 3, "penetrating": false, "dice3d": true, "rolls": [] }""");
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));

        cut.Find("[data-testid=look-open]").Click();
        cut.Find("[data-testid=look-material-Wood]").Click();
        cut.WaitForAssertion(() => Assert.Contains("\"material\": \"Wood\"", LastSaved(), StringComparison.Ordinal));
        // Como el navegador: la siguiente lectura devuelve lo que se acaba de guardar.
        stored.SetResult(LastSaved());

        cut.Find("[data-testid=dice-formula]").Input("1d6");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => _module.VerifyInvoke("roll"));
        var look = JsonSerializer.SerializeToElement(_module.Invocations["roll"].Last().Arguments[2]);
        Assert.Equal("Wood", look.GetProperty("material").GetString());
        _module.VerifyInvoke("unlockAudio");
    }
}
