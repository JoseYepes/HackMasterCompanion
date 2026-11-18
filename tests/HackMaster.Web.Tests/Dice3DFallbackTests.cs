using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests;

/// <summary>El tirador funciona siempre: sin WebGL, si el 3D falla o con demasiados dados (US2: FR-007, FR-008, FR-011, FR-012).</summary>
public class Dice3DFallbackTests : BunitContext
{
    private const string Dice3DOn = """{ "schemaVersion": 2, "penetrating": false, "dice3d": true, "rolls": [] }""";

    private readonly BunitJSModuleInterop _module;
    private readonly JSRuntimeInvocationHandler<string?> _stored;

    public Dice3DFallbackTests()
    {
        Services.AddSingleton<IRandom>(new LoopRandom(3, 4));
        Services.AddSingleton<BrowserStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _stored = JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls");
        _stored.SetResult(Dice3DOn);
        _module = JSInterop.SetupModule("./js/dice3d.js");
        _module.SetupVoid("create", _ => true).SetVoidResult();
        _module.SetupVoid("finish", _ => true).SetVoidResult();
        _module.SetupVoid("dispose", _ => true).SetVoidResult();
    }

    private static void Submit(IRenderedComponent<Dice> cut, string formula)
    {
        cut.Find("[data-testid=dice-formula]").Input(formula);
        cut.Find("form").Submit();
    }

    [Fact]
    public void Without_WebGL_The_Option_Is_Disabled_With_A_Warning_And_Rolls_Work__FR011()
    {
        _module.Setup<bool>("isSupported").SetResult(false);

        var cut = Render<Dice>();

        cut.WaitForAssertion(() => Assert.Contains("no puede mostrar dados 3D", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal));
        Assert.True(cut.Find("[data-testid=dice3d-toggle]").HasAttribute("disabled"));
        Assert.Empty(cut.FindAll("[data-testid=dice-board]"));
        Submit(cut, "2d6+3");
        Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim());
    }

    [Fact]
    public void A_Failure_While_Rolling_Still_Shows_The_Result_And_Switches_To_No_3D__FR011()
    {
        _module.Setup<bool>("isSupported").SetResult(true);
        _module.SetupVoid("roll", _ => true).SetException(new JSException("WebGL: CONTEXT_LOST"));
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));

        Submit(cut, "2d6+3");

        cut.WaitForAssertion(() => Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim()));
        Assert.Contains("han fallado", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=dice-board]"));
    }

    [Fact]
    public void A_Failure_Loading_The_Module_Switches_To_No_3D__FR011()
    {
        _module.Setup<bool>("isSupported").SetException(new JSException("Failed to fetch dynamically imported module"));

        var cut = Render<Dice>();

        cut.WaitForAssertion(() => Assert.Contains("No se pudieron cargar", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal));
        Submit(cut, "1d6");
        Assert.Equal("3", cut.Find("[data-testid=dice-total]").TextContent.Trim());
    }

    [Fact]
    public void More_Than_20_Dice_Are_Not_Animated_And_The_Result_Is_Shown_At_Once__FR007()
    {
        _module.Setup<bool>("isSupported").SetResult(true);
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));

        Submit(cut, "21d6");

        Assert.Empty(_module.Invocations["roll"]);
        Assert.Contains("Más de 20 dados", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(cut.FindAll("[data-testid=dice-total]"));
    }

    [Fact]
    public void Dice_Without_A_Physical_Shape_Get_A_Note__FR008()
    {
        _module.Setup<bool>("isSupported").SetResult(true);
        _module.SetupVoid("roll", _ => true).SetVoidResult();
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));

        Submit(cut, "1d3+1d6");

        cut.WaitForAssertion(() => Assert.Contains("1d3", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal));
        Assert.Contains("no tiene forma 3D", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Turning_3D_Off_Rolls_Exactly_As_Before__FR012()
    {
        _module.Setup<bool>("isSupported").SetResult(true);
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));

        cut.Find("[data-testid=dice3d-toggle]").Change(false);
        // Como el navegador: la siguiente lectura devuelve lo que se acaba de guardar.
        _stored.SetResult((string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!);
        Submit(cut, "2d6+3");

        Assert.Empty(_module.Invocations["roll"]);
        Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid=dice-board]"));
    }
}
