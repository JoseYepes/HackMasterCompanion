using Bunit;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

public class SkillsPageTests : BunitContext
{
    public SkillsPageTests() => Services.AddSingleton(TestData.Create());

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<Skills> RenderAt(string relativeUri)
    {
        Navigation.NavigateTo(relativeUri);
        return Render<Skills>();
    }

    private static IReadOnlyList<string> RowNames(IRenderedComponent<Skills> cut) =>
        cut.FindAll("[data-testid=skill-row] .skill-name").Select(name => name.TextContent.Trim()).ToList();

    [Fact]
    public void Shows_Every_Skill_And_The_Count__FR012()
    {
        var cut = RenderAt("habilidades");

        Assert.Equal(8, cut.FindAll("[data-testid=skill-row]").Count);
        Assert.Contains("8", cut.Find("[data-testid=result-count]").TextContent);
        Assert.Equal("Botánica", RowNames(cut)[0]);
    }

    [Fact]
    public void Each_Row_Shows_Name_Type_Cost_And_Book__FR012()
    {
        var cut = RenderAt("habilidades");

        var row = cut.FindAll("[data-testid=skill-row]").Single(row => row.TextContent.Contains("Cerrajería"));
        Assert.Contains("Técnicas", row.TextContent);
        Assert.Contains("4", row.TextContent);
        Assert.Contains("MJ 130", row.TextContent);
        Assert.Equal("habilidades/cerrajeria", row.QuerySelector("a")?.GetAttribute("href"));
    }

    [Fact]
    public void Typing_Filters_With_Name_Matches_First_And_A_Snippet_For_Summary_Matches__FR013()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=search-input]").Input("BOTÁN");

        cut.WaitForAssertion(() => Assert.Equal(["Botánica", "Herbalismo"], RowNames(cut)));
        var rows = cut.FindAll("[data-testid=skill-row]");
        Assert.Empty(rows[0].QuerySelectorAll(".snippet"));
        Assert.Contains("botánica", rows[1].QuerySelector(".snippet")?.TextContent);
        Assert.Contains("q=BOT", Navigation.Uri);
        Assert.Contains("2", cut.Find("[data-testid=result-count]").TextContent);
    }

    [Fact]
    public void Marking_Two_Books_And_A_Type_Combines_Them__FR014()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=filter-book] input[value='MJ']").Change(true);
        cut.Find("[data-testid=filter-book] input[value='CG']").Change(true);
        cut.Find("[data-testid=filter-type] input[value='Técnicas']").Change(true);

        Assert.Equal(["Cerrajería", "Herbalismo"], RowNames(cut));
        Assert.Contains("libro=MJ", Navigation.Uri);
        Assert.Contains("libro=CG", Navigation.Uri);
    }

    [Fact]
    public void Class_Filter_Includes_Skills_For_All_Classes__FR015()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=filter-class] input[value='Grupo de ladrones']").Change(true);

        Assert.Equal(["Botánica", "Cerrajería", "Liderazgo básico", "Lucha", "Rastrear", "Zoología"], RowNames(cut));
    }

    [Fact]
    public void Price_Range_Filters_And_Excludes_Skills_Without_Price__FR016()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=filter-price-max]").Change("300");

        Assert.Equal(["Botánica", "Liderazgo básico", "Lucha", "Rastrear", "Zoología"], RowNames(cut));
        Assert.Contains("precioMax=300", Navigation.Uri);
    }

    [Fact]
    public void Active_Filters_Are_Visible_Without_Opening_The_Panel__FR014()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=filter-book] input[value='GMG']").Change(true);

        var active = cut.Find("[data-testid=active-filters]");
        Assert.Contains("GMG", active.TextContent);
        Assert.Null(active.Closest("details"));
    }

    [Fact]
    public void No_Results_Shows_A_Message_And_A_Way_To_Clear_Everything__FR014()
    {
        var cut = RenderAt("habilidades?q=dragon&tipo=T%C3%A9cnicas");

        Assert.Empty(cut.FindAll("[data-testid=skill-row]"));
        Assert.Contains("Ninguna habilidad", cut.Markup);

        cut.Find("[data-testid=clear-filters]").Click();

        Assert.Equal(8, cut.FindAll("[data-testid=skill-row]").Count);
        Assert.Equal(string.Empty, cut.Find("[data-testid=search-input]").GetAttribute("value") ?? string.Empty);
        Assert.DoesNotContain("q=", Navigation.Uri);
    }

    [Fact]
    public void Sort_Selector_Changes_The_Order__FR018()
    {
        var cut = RenderAt("habilidades");

        cut.Find("[data-testid=sort-select]").Change("pc");

        Assert.Equal("Lucha", RowNames(cut)[0]);
        Assert.Contains("orden=pc", Navigation.Uri);
    }

    [Fact]
    public void Address_Parameters_Restore_The_Query__FR014()
    {
        var cut = RenderAt("habilidades?q=lucha&tipo=Artes%20marciales&libro=CG&clase=Monje&precioMax=600&difMin=-5&orden=-precio");

        Assert.Equal(["Lucha"], RowNames(cut));
        Assert.Equal("lucha", cut.Find("[data-testid=search-input]").GetAttribute("value"));
        Assert.True(cut.Find("[data-testid=filter-type] input[value='Artes marciales']").HasAttribute("checked"));
        Assert.Equal("600", cut.Find("[data-testid=filter-price-max]").GetAttribute("value"));
    }

    [Fact]
    public void Address_Sort_Is_Restored__FR018()
    {
        var cut = RenderAt("habilidades?orden=-precio");

        Assert.Equal("Herbalismo", RowNames(cut)[0]);
        Assert.Equal("Cerrajería", RowNames(cut)[^1]);
    }

    [Fact]
    public void Invalid_Address_Parameters_Are_Ignored__FR014()
    {
        var cut = RenderAt("habilidades?tipo=Inexistente&precioMax=mucho&orden=raro&otro=1");

        Assert.Equal(8, cut.FindAll("[data-testid=skill-row]").Count);
        Assert.Equal("Botánica", RowNames(cut)[0]);
    }
}
