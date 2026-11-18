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

    /// <summary>
    /// Espera a que la página guarde el historial con ese valor de 3D y hace que localStorage devuelva
    /// eso desde entonces, como el navegador. Sin esperar, en un equipo lento se tomaba el guardado
    /// anterior y la página volvía a leer el valor viejo.
    /// </summary>
    private void StoreSaved(IRenderedComponent<Dice> cut, bool dice3d)
    {
        string? saved = null;
        cut.WaitForAssertion(() =>
        {
            saved = (string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!;
            Assert.Equal(dice3d, System.Text.Json.JsonDocument.Parse(saved).RootElement.GetProperty("dice3d").GetBoolean());
        });
        _stored.SetResult(saved);
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
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));

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
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));

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
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));

        Submit(cut, "1d3+1d6");

        cut.WaitForAssertion(() => Assert.Contains("1d3", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal));
        Assert.Contains("no tiene forma 3D", cut.Find("[data-testid=dice3d-warning]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Turning_3D_Off_While_It_Loads_And_On_Again_Leaves_It_Working__FR011()
    {
        var supported = _module.Setup<bool>("isSupported");
        _module.SetupVoid("roll", _ => true).SetVoidResult();
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => Assert.Single(_module.Invocations["isSupported"]));

        // Se apaga antes de que el módulo responda; después responde.
        cut.Find("[data-testid=dice3d-toggle]").Change(false);
        StoreSaved(cut, dice3d: false);
        supported.SetResult(true);
        Assert.Empty(_module.Invocations["create"]);

        cut.Find("[data-testid=dice3d-toggle]").Change(true);
        StoreSaved(cut, dice3d: true);
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));
        Assert.Single(_module.Invocations["create"]);
        Submit(cut, "1d6");

        cut.WaitForAssertion(() => _module.VerifyInvoke("roll"));
        Assert.Empty(cut.FindAll("[data-testid=dice3d-warning]"));
    }

    [Fact]
    public void Turning_3D_Off_And_On_Before_The_First_Load_Ends_Still_Starts_The_New_Board__FR011()
    {
        var supported = _module.Setup<bool>("isSupported");
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => Assert.Single(_module.Invocations["isSupported"]));

        cut.Find("[data-testid=dice3d-toggle]").Change(false);
        StoreSaved(cut, dice3d: false);
        cut.Find("[data-testid=dice3d-toggle]").Change(true);
        StoreSaved(cut, dice3d: true);
        supported.SetResult(true);

        cut.WaitForAssertion(() => Assert.Single(_module.Invocations["create"]));
    }

    [Fact]
    public void Turning_3D_Off_Rolls_Exactly_As_Before__FR012()
    {
        _module.Setup<bool>("isSupported").SetResult(true);
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => Assert.Equal("true", cut.Find("[data-testid=dice3d-toggle]").GetAttribute("data-ready")));

        cut.Find("[data-testid=dice3d-toggle]").Change(false);
        StoreSaved(cut, dice3d: false);
        Submit(cut, "2d6+3");

        Assert.Empty(_module.Invocations["roll"]);
        Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim());
        Assert.Empty(cut.FindAll("[data-testid=dice-board]"));
    }
}
