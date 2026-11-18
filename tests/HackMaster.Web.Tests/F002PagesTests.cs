using Bunit;
using HackMaster.Web.Layout;
using HackMaster.Web.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Direcciones nuevas de F002 y enlace en la navegación (FR-021).</summary>
public class F002PagesTests : BunitContext
{
    public F002PagesTests()
    {
        Services.AddSingleton(TestData.Create());
        Services.AddSingleton<BrowserStore>();
    }

    [Fact]
    public void Dice_Page_Has_Its_Title__FR021()
    {
        var cut = Render<Dice>();

        Assert.Equal("Dados", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Suite_Editor_Page_Has_Its_Title__FR021()
    {
        var cut = Render<SuiteEditor>();

        Assert.Equal("Crear conjunto", cut.Find("h1").TextContent.Trim());
    }

    [Fact]
    public void Main_Navigation_Links_To_The_Dice_Page__FR021()
    {
        var cut = Render<MainLayout>();

        Assert.Contains(cut.FindAll(".site-nav a"), link => link.GetAttribute("href") == "dados" && link.TextContent.Trim() == "Dados");
    }
}
