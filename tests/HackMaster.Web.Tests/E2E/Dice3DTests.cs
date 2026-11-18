using System.Text.Json;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Dados 3D en un navegador real: la cara que queda arriba es siempre la que manda el tirador
/// (SC-001, FR-001). Usa la simulación sin dibujar del módulo (contracts/dice3d-js.md).
/// </summary>
[Trait("Category", "E2E")]
public sealed class Dice3DTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    /// <summary>
    /// Genera en el navegador 1 000 planes al azar (semilla fija) con todas las formas, simula cada
    /// uno y devuelve las tiradas en las que alguna cara de arriba no coincide con la pedida.
    /// </summary>
    private const string ThousandRolls = """
        async () => {
          const dice3d = await import('./js/dice3d.js');
          let state = 20261118;
          const next = () => { state = (state * 1664525 + 1013904223) >>> 0; return state / 4294967296; };
          const pick = values => values[Math.floor(next() * values.length)];
          const range = (from, to) => Array.from({ length: to - from + 1 }, (_, i) => String(from + i));
          const faces = {
            D4: range(1, 4), D6: range(1, 6), D8: range(1, 8), D10: range(1, 10),
            D12: range(1, 12), D20: range(1, 20),
          };
          const shapes = Object.keys(faces);
          const failures = [];
          for (let roll = 0; roll < 1000; roll++) {
            const dice = [];
            const count = 1 + Math.floor(next() * 6);
            for (let i = 0; i < count; i++) {
              if (next() < 0.15) {
                dice.push({ shape: 'D10Tens', face: pick(['00', '10', '20', '30', '40', '50', '60', '70', '80', '90']), wave: 0, penetration: false });
                dice.push({ shape: 'D10', face: pick(range(0, 9)), wave: 0, penetration: false });
              } else {
                const shape = pick(shapes);
                dice.push({ shape, face: pick(faces[shape]), wave: 0, penetration: false });
              }
            }
            // Además de la cara, comprueba lo que la hace legible y fiable: el dado queda plano
            // (la cara de arriba mira hacia arriba), dentro de la mesa, con tantas caras como
            // etiquetas y sin etiquetas repetidas. F008: la malla que se ve es redondeada (muchos
            // más vértices que el poliedro) y sus caras están en los mismos planos que las del
            // poliedro con el que rueda, así que se apoya igual.
            const result = dice3d.inspect({ dice }, roll + 1);
            result.dice.forEach((shown, i) => {
              const ok = shown.face === dice[i].face && shown.up > 0.99 && shown.inside && shown.unique && shown.slots === shown.labels
                && shown.roundedVertices > 10 * shown.slots && shown.planeError < 0.005;
              if (!ok) failures.push({ roll, die: dice[i], shown });
            });
          }
          return JSON.stringify(failures.slice(0, 5)) + ' / ' + failures.length;
        }
        """;

    [E2EFact]
    public async Task In_1000_Simulated_Rolls_Every_Die_Stops_Flat_On_The_Wanted_Face__SC001()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");

        var result = await page.EvaluateAsync<string>(ThousandRolls);

        Assert.Equal("[] / 0", result);
    }

    [E2EFact]
    public async Task Penetration_Waves_Are_Simulated_With_Their_Faces__FR006()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");

        var shown = await page.EvaluateAsync<string[]>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const plan = { dice: [
                { shape: 'D6', face: '6', wave: 0, penetration: false },
                { shape: 'D6', face: '2', wave: 0, penetration: false },
                { shape: 'D6', face: '6', wave: 1, penetration: true },
                { shape: 'D6', face: '3', wave: 2, penetration: true },
              ] };
              return dice3d.simulate(plan, 7);
            }
            """);

        Assert.Equal(["6", "2", "6", "3"], shown);
    }

    /// <summary>Activa los dados 3D antes de abrir el tirador, como si el jugador ya lo hubiera elegido.</summary>
    private static async Task<IPage> OpenWith3DAsync(BrowserFixture browser, ViewportSize viewport, string formula)
    {
        var page = await browser.NewPageAsync(viewport);
        await BrowserFixture.GoToAsync(page, "dados");
        await page.EvaluateAsync("""() => localStorage.setItem('hackmaster.rolls', '{"schemaVersion":2,"penetrating":false,"dice3d":true,"rolls":[]}')""");
        await BrowserFixture.GoToAsync(page, "dados?f=" + Uri.EscapeDataString(formula));
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        return page;
    }

    [E2EFact]
    public async Task Extra_Dice_Are_Marked_On_Screen__FR006()
    {
        var page = await OpenWith3DAsync(browser, Viewports.Phone, "1d6");

        var dice = await page.EvaluateAsync<string>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              await dice3d.roll(board, { dice: [
                { shape: 'D6', face: '6', wave: 0, penetration: false },
                { shape: 'D6', face: '2', wave: 1, penetration: true },
              ] });
              return JSON.stringify(dice3d.shownDice(board).map(die => ({ face: die.face, penetration: die.penetration })));
            }
            """);

        // F008 añade a cada dado su material; aquí solo importan la cara y la marca de dado extra.
        Assert.Equal("""[{"face":"6","penetration":false},{"face":"2","penetration":true}]""", dice);
    }

    [E2EFact]
    public async Task A_Roll_On_Screen_Ends_In_Under_4_Seconds_Showing_The_Detail_Values__SC004()
    {
        var page = await OpenWith3DAsync(browser, Viewports.Phone, "1d4+1d6+1d8+1d10+1d12+1d20+1d100");

        var result = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const started = performance.now();
              document.querySelector('[data-testid=dice-roll]').click();
              while (!document.querySelector('[data-testid=dice-total]') && performance.now() - started < 10000) {
                await new Promise(resolve => setTimeout(resolve, 20));
              }
              const ms = performance.now() - started;
              const shown = dice3d.shownFaces(document.querySelector('[data-testid=dice-board]'));
              const detail = [...document.querySelectorAll('[data-testid=dice-detail] .die')].map(die => die.textContent.trim());
              const percentile = (Number(shown[6]) + Number(shown[7])) || 100;
              return { ms, shown: [...shown.slice(0, 6), String(percentile)], detail };
            }
            """);

        Assert.InRange(result.GetProperty("ms").GetDouble(), 0, 4000);
        Assert.Equal(result.GetProperty("detail").GetRawText(), result.GetProperty("shown").GetRawText());
    }

    [E2EFact]
    public async Task Without_3D_Nothing_Of_The_3D_Part_Is_Downloaded__FR016()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        var requested = new List<string>();
        page.Request += (_, request) => requested.Add(request.Url);

        await BrowserFixture.GoToAsync(page, "habilidades");
        await BrowserFixture.GoToAsync(page, "dados");
        await page.Locator("[data-testid=dice-formula]").WaitForAsync();

        Assert.DoesNotContain(requested, url => url.Contains("/lib/", StringComparison.Ordinal) || url.Contains("dice3d", StringComparison.Ordinal));
    }

    [E2EFact]
    public async Task Tray_Buttons_Are_At_Least_44_Pixels_At_390__FR022()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");
        await page.Locator("[data-testid=tray-die-6]").WaitForAsync();

        var smallest = await page.EvaluateAsync<double>("""
            () => Math.min(...[...document.querySelectorAll('.tray-die, .tray-action')]
              .map(button => Math.min(button.getBoundingClientRect().width, button.getBoundingClientRect().height)))
            """);

        Assert.True(smallest >= 44, $"El botón más pequeño mide {smallest} px.");
    }

    [E2EFact]
    public async Task The_Module_Reports_Whether_3D_Is_Available__FR011()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");

        var supported = await page.EvaluateAsync<JsonElement>("async () => (await import('./js/dice3d.js')).isSupported()");

        Assert.Equal(JsonValueKind.True, supported.ValueKind);
    }
}
