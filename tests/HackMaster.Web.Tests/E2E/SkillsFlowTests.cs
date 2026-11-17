using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>Flujos críticos de la consulta de habilidades con los datos reales (SC-001, SC-002).</summary>
[Trait("Category", "E2E")]
public sealed partial class SkillsFlowTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    private static ILocator Rows(IPage page) => page.Locator("[data-testid=skill-row]");

    private static ILocator Count(IPage page) => page.Locator("[data-testid=result-count]");

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingNumber();

    private static async Task<int> ShownCountAsync(IPage page) =>
        int.Parse(LeadingNumber().Match(await Count(page).InnerTextAsync()).Value, System.Globalization.CultureInfo.InvariantCulture);

    [E2EFact]
    public async Task Shows_All_343_Skills_On_Entry__SC001()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");

        await Assertions.Expect(Count(page)).ToHaveTextAsync("343 habilidades");
        Assert.Equal(343, await Rows(page).CountAsync());
    }

    [E2EFact]
    public async Task Search_Without_Accents_Shows_Botanica_First__FR013()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");

        await page.Locator("[data-testid=search-input]").FillAsync("botanica");

        await Assertions.Expect(Rows(page).First.Locator(".skill-name")).ToHaveTextAsync("Botánica");
    }

    [E2EFact]
    public async Task Two_Books_And_A_Type_Combine__FR014()
    {
        var page = await browser.NewPageAsync(Viewports.Tablet);
        await BrowserFixture.GoToAsync(page, "habilidades");
        await page.Locator("details.filters summary").ClickAsync();

        await page.Locator("[data-testid=filter-book] input[value='MJ']").CheckAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("libro=MJ"));
        var oneBook = await ShownCountAsync(page);

        await page.Locator("[data-testid=filter-book] input[value='CG']").CheckAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("libro=CG.*libro=MJ"));
        var twoBooks = await ShownCountAsync(page);

        await page.Locator("[data-testid=filter-type] input").First.CheckAsync();
        await Assertions.Expect(page).ToHaveURLAsync(new Regex("tipo="));
        var withType = await ShownCountAsync(page);

        Assert.True(twoBooks > oneBook, "Marcar también CG debe sumar las habilidades de ese libro.");
        Assert.True(withType > 0 && withType < twoBooks, "Añadir un tipo debe dejar solo una parte de los resultados.");
        Assert.Equal(withType, await Rows(page).CountAsync());
    }

    [E2EFact]
    public async Task Back_From_A_Skill_Page_Keeps_The_Query__US2()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades?q=bot");
        var listUri = page.Url;

        await Rows(page).First.Locator("a").ClickAsync();
        await page.WaitForURLAsync(new Regex("/habilidades/[^?]+$"));
        await page.GoBackAsync();

        await page.WaitForURLAsync(listUri);
        await Assertions.Expect(page.Locator("[data-testid=search-input]")).ToHaveValueAsync("bot");
    }

    [E2EFact]
    public async Task Reloading_A_Skill_Page_Loads_The_Same_Skill__US2()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "habilidades");
        var name = (await Rows(page).First.Locator(".skill-name").InnerTextAsync()).Trim();
        await Rows(page).First.Locator("a").ClickAsync();
        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(name);

        await page.ReloadAsync();

        await Assertions.Expect(page.Locator("h1")).ToHaveTextAsync(name);
    }
}
