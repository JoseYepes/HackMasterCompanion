using System.Text.Json;
using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>La opción «Dados 3D» del tirador, con el módulo JavaScript simulado (US1: FR-001 a FR-005, FR-009, FR-010).</summary>
public class Dice3DPageTests : BunitContext
{
    private const string ModulePath = "./js/dice3d.js";

    /// <summary>Historial guardado con los dados 3D activados y sin tiradas.</summary>
    private const string Dice3DOn = """{ "schemaVersion": 2, "penetrating": false, "dice3d": true, "rolls": [] }""";

    private readonly BunitJSModuleInterop _module;
    private readonly JSRuntimeInvocationHandler _roll;

    public Dice3DPageTests()
    {
        Services.AddSingleton<IRandom>(new LoopRandom(3, 4));
        Services.AddSingleton<BrowserStore>();
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(ModulePath);
        _module.Setup<bool>("isSupported").SetResult(true);
        _module.SetupVoid("create", _ => true).SetVoidResult();
        _module.SetupVoid("finish", _ => true).SetVoidResult();
        _module.SetupVoid("dispose", _ => true).SetVoidResult();
        _roll = _module.SetupVoid("roll", _ => true);
    }

    private IRenderedComponent<Dice> RenderWith3DOn()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls").SetResult(Dice3DOn);
        var cut = Render<Dice>();
        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));
        return cut;
    }

    private static void Submit(IRenderedComponent<Dice> cut, string formula)
    {
        cut.Find("[data-testid=dice-formula]").Input(formula);
        cut.Find("form").Submit();
    }

    private string LastSaved() => (string)JSInterop.Invocations["localStorage.setItem"].Last().Arguments[1]!;

    [Fact]
    public void Dice3D_Starts_Off_And_Nothing_3D_Is_Downloaded__FR010()
    {
        var cut = Render<Dice>();

        Assert.False(cut.WaitForElement("[data-testid=dice3d-toggle]").HasAttribute("checked"));
        Assert.Empty(JSInterop.Invocations["import"]);
    }

    [Fact]
    public void Turning_Dice3D_On_Loads_The_Module_And_Remembers_The_Choice__FR009()
    {
        var cut = Render<Dice>();

        cut.WaitForElement("[data-testid=dice3d-toggle]").Change(true);

        cut.WaitForAssertion(() => _module.VerifyInvoke("create"));
        Assert.Contains("\"dice3d\": true", LastSaved(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_Roll_Sends_The_Dice_Of_The_Domain_Plan_And_Shows_The_Total_When_They_Stop__FR005()
    {
        var cut = RenderWith3DOn();

        Submit(cut, "2d6+3");

        cut.WaitForAssertion(() => _module.VerifyInvoke("roll"));
        var plan = JsonSerializer.Serialize(_module.Invocations["roll"].Single().Arguments[1]);
        Assert.Contains("\"shape\":\"D6\",\"face\":\"3\"", plan, StringComparison.Ordinal);
        Assert.Contains("\"shape\":\"D6\",\"face\":\"4\"", plan, StringComparison.Ordinal);
        Assert.Empty(cut.FindAll("[data-testid=dice-total]"));

        _roll.SetVoidResult();

        cut.WaitForAssertion(() => Assert.Equal("10", cut.Find("[data-testid=dice-total]").TextContent.Trim()));
    }

    [Fact]
    public void Touching_The_Dice_Finishes_The_Roll__FR004()
    {
        var cut = RenderWith3DOn();
        Submit(cut, "1d20");
        cut.WaitForAssertion(() => _module.VerifyInvoke("roll"));

        cut.Find("[data-testid=dice-board]").Click();

        _module.VerifyInvoke("finish");
    }

    [Fact]
    public void Rolling_Again_While_The_Dice_Roll_Finishes_The_Previous_Roll_First__F003()
    {
        var cut = RenderWith3DOn();
        Submit(cut, "1d20");
        cut.WaitForAssertion(() => _module.VerifyInvoke("roll"));

        Submit(cut, "1d6");

        cut.WaitForAssertion(() => _module.VerifyInvoke("finish"));
    }

    [Fact]
    public void The_History_Is_Saved_The_Same_With_3D__FR002()
    {
        var cut = RenderWith3DOn();

        Submit(cut, "2d6+3");

        cut.WaitForAssertion(() => Assert.Contains("\"formula\": \"2d6+3\"", LastSaved(), StringComparison.Ordinal));
        Assert.Contains("\"total\": 10", LastSaved(), StringComparison.Ordinal);
    }
}
