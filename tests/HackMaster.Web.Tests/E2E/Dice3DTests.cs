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
            const shown = dice3d.simulate({ dice }, roll + 1);
            const wanted = dice.map(die => die.face);
            if (JSON.stringify(shown) !== JSON.stringify(wanted)) {
              failures.push({ roll, wanted, shown });
            }
          }
          return JSON.stringify(failures.slice(0, 5)) + ' / ' + failures.length;
        }
        """;

    [E2EFact]
    public async Task In_1000_Simulated_Rolls_Every_Die_Stops_On_The_Wanted_Face__SC001()
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

    [E2EFact]
    public async Task The_Module_Reports_Whether_3D_Is_Available__FR011()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await BrowserFixture.GoToAsync(page, "dados");

        var supported = await page.EvaluateAsync<JsonElement>("async () => (await import('./js/dice3d.js')).isSupported()");

        Assert.Equal(JsonValueKind.True, supported.ValueKind);
    }
}
