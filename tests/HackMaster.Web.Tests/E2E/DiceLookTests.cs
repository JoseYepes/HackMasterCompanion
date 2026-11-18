using System.Text.Json;
using Microsoft.Playwright;

namespace HackMaster.Web.Tests.E2E;

/// <summary>
/// Dados personalizables en un navegador real (F008): aspecto, efectos al penetrar, sonido y calidad
/// (specs/006-dados-personalizables/contracts/dice3d-look.md).
/// </summary>
[Trait("Category", "E2E")]
public sealed class DiceLookTests(BrowserFixture browser) : IClassFixture<BrowserFixture>
{
    /// <summary>Historial versión 3 con los dados 3D activados y el aspecto indicado.</summary>
    private static string History(string material, string effect = "Explosion", bool sound = true, string quality = "High", bool penetrating = false) =>
        JsonSerializer.Serialize(new
        {
            schemaVersion = 3,
            penetrating,
            dice3d = true,
            look = new { material, color = "#2f5a46", ink = "#f1e5c4", effect, sound, quality },
            rolls = Array.Empty<object>(),
        });

    private static async Task<IPage> OpenAsync(BrowserFixture browser, ViewportSize viewport, string history, string formula = "2d6+1d20")
    {
        var page = await browser.NewPageAsync(viewport);
        await BrowserFixture.GoToAsync(page, "dados");
        await page.EvaluateAsync("value => localStorage.setItem('hackmaster.rolls', value)", history);
        await BrowserFixture.GoToAsync(page, "dados?f=" + Uri.EscapeDataString(formula));
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        return page;
    }

    /// <summary>Tira con el botón y espera al total; devuelve los dados en pantalla.</summary>
    private static async Task<JsonElement> RollAsync(IPage page) =>
        await page.EvaluateAsync<JsonElement>("""
            async () => {
              document.querySelector('[data-testid=dice-roll]').click();
              const started = performance.now();
              while (!document.querySelector('[data-testid=dice-total]') && performance.now() - started < 10000) {
                await new Promise(resolve => setTimeout(resolve, 20));
              }
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              return JSON.parse(JSON.stringify(dice3d.shownDice(board)));
            }
            """);

    [E2EFact]
    public async Task The_Dice_Are_Drawn_With_The_Saved_Material_Also_After_Reloading__FR001_FR012()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Marble"));

        var dice = await RollAsync(page);
        Assert.Equal(3, dice.GetArrayLength());
        Assert.All(dice.EnumerateArray(), die => Assert.Equal("Marble", die.GetProperty("material").GetString()));

        await page.ReloadAsync();
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        var again = await RollAsync(page);
        Assert.All(again.EnumerateArray(), die => Assert.Equal("Marble", die.GetProperty("material").GetString()));
    }

    [E2EFact]
    public async Task Choosing_A_Material_In_The_Panel_Changes_The_Next_Roll__FR001()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        await page.Locator("[data-testid=look-open]").ClickAsync();
        await page.Locator("[data-testid=look-material-Crystal]").ClickAsync();
        var dice = await RollAsync(page);

        Assert.All(dice.EnumerateArray(), die => Assert.Equal("Crystal", die.GetProperty("material").GetString()));
    }

    /// <summary>Tira en la mesa un d6 que saca 6 y penetra (extra 2) con el aspecto indicado; devuelve efectos y dados.</summary>
    private const string PenetratingRoll = """
        async look => {
          const dice3d = await import('./js/dice3d.js');
          const board = document.querySelector('[data-testid=dice-board]');
          const started = performance.now();
          await dice3d.roll(board, { dice: [
            { shape: 'D6', face: '6', wave: 0, penetration: false, parent: null },
            { shape: 'D6', face: '6', wave: 1, penetration: true, parent: 0 },
            { shape: 'D6', face: '2', wave: 2, penetration: true, parent: 1 },
          ] }, look);
          return JSON.parse(JSON.stringify({
            ms: performance.now() - started,
            effects: dice3d.shownEffects(board),
            dice: dice3d.shownDice(board),
          }));
        }
        """;

    private static object Look(string effect, string quality = "High") =>
        new { material = "Ivory", color = "#f4efe6", ink = "#1f2328", effect, sound = false, quality };

    [E2EFact]
    public async Task Each_Penetration_Shows_The_Chosen_Effect_On_Its_Die_And_Extra_Dice_Glow__FR007_FR017()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var result = await page.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Fire"));

        Assert.Equal(
            [("Fire", 0), ("Fire", 1)],
            result.GetProperty("effects").EnumerateArray().Select(effect => (effect.GetProperty("effect").GetString(), effect.GetProperty("parent").GetInt32())));
        // Revisión de F008: shownEffects dice lo que se lanzó de verdad en pantalla, no lo planeado;
        // el primero sale con la animación en marcha (el último podría coincidir con el final).
        Assert.True(result.GetProperty("effects")[0].GetProperty("launched").GetBoolean(), "El efecto no llegó a lanzarse en pantalla.");
        Assert.Equal([false, true, true], result.GetProperty("dice").EnumerateArray().Select(die => die.GetProperty("aura").GetBoolean()));
        Assert.InRange(result.GetProperty("ms").GetDouble(), 0, 4000);
    }

    [E2EFact]
    public async Task With_No_Effect_The_Extra_Dice_Still_Glow__FR007_FR017()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var result = await page.EvaluateAsync<JsonElement>(PenetratingRoll, Look("None"));

        Assert.Equal(0, result.GetProperty("effects").GetArrayLength());
        Assert.Equal([false, true, true], result.GetProperty("dice").EnumerateArray().Select(die => die.GetProperty("aura").GetBoolean()));
    }

    [E2EFact]
    public async Task With_Low_Quality_Or_Reduced_Motion_The_Effect_Is_A_Short_Flash__FR018()
    {
        var low = await OpenAsync(browser, Viewports.Phone, History("Ivory"));
        var lowResult = await low.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Blood", "Low"));
        Assert.All(lowResult.GetProperty("effects").EnumerateArray(), effect => Assert.Equal("Flash", effect.GetProperty("effect").GetString()));

        var calm = await browser.NewPageAsync(Viewports.Phone);
        await calm.EmulateMediaAsync(new PageEmulateMediaOptions { ReducedMotion = ReducedMotion.Reduce });
        await BrowserFixture.GoToAsync(calm, "dados");
        await calm.EvaluateAsync("value => localStorage.setItem('hackmaster.rolls', value)", History("Ivory"));
        await BrowserFixture.GoToAsync(calm, "dados");
        await calm.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();
        var calmResult = await calm.EvaluateAsync<JsonElement>(PenetratingRoll, Look("Explosion"));
        Assert.Equal(2, calmResult.GetProperty("effects").GetArrayLength());
        Assert.All(calmResult.GetProperty("effects").EnumerateArray(), effect => Assert.Equal("Flash", effect.GetProperty("effect").GetString()));
    }

    /// <summary>Tira 6d6 (con una penetración) en la mesa con el aspecto indicado; devuelve los sonidos programados.</summary>
    private const string NoisyRoll = """
        async look => {
          const dice3d = await import('./js/dice3d.js');
          const board = document.querySelector('[data-testid=dice-board]');
          dice3d.unlockAudio();
          const dice = [1, 2, 3, 4, 5, 6].map(face => ({ shape: 'D6', face: String(face), wave: 0, penetration: false, parent: null }));
          dice.push({ shape: 'D6', face: '3', wave: 1, penetration: true, parent: 5 });
          const started = performance.now();
          await dice3d.roll(board, { dice }, look);
          return JSON.parse(JSON.stringify({ ms: performance.now() - started, sounds: dice3d.heardSounds(board), faces: dice3d.shownFaces(board) }));
        }
        """;

    private static object SoundLook(bool sound, string material = "BlackIron", string effect = "Explosion") =>
        new { material, color = "#4a4c52", ink = "#e8b54a", effect, sound, quality = "High" };

    [E2EFact]
    public async Task Collisions_Sound_With_Their_Strength_And_Material_At_Most_4_Every_50_Ms__FR008()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("BlackIron"));

        var result = await page.EvaluateAsync<JsonElement>(NoisyRoll, SoundLook(sound: true));

        var sounds = result.GetProperty("sounds").EnumerateArray().ToList();
        var hits = sounds.Where(sound => sound.GetProperty("kind").GetString() == "BlackIron").ToList();
        Assert.NotEmpty(hits);
        Assert.All(hits, hit => Assert.InRange(hit.GetProperty("volume").GetDouble(), 0.01, 1));
        Assert.True(hits.Select(hit => hit.GetProperty("volume").GetDouble()).Distinct().Count() > 1, "Todos los golpes suenan igual de fuerte.");
        var times = sounds.Select(sound => sound.GetProperty("at").GetDouble()).Order().ToList();
        for (var i = 4; i < times.Count; i++)
        {
            Assert.True(times[i] - times[i - 4] >= 0.05, $"Más de 4 sonidos en 50 ms alrededor de {times[i]:0.000} s.");
        }

        Assert.Contains(sounds, sound => sound.GetProperty("kind").GetString() == "Explosion");
        Assert.InRange(result.GetProperty("ms").GetDouble(), 0, 4000);
    }

    [E2EFact]
    public async Task One_Die_Landing_Sounds_Once_Per_Moment_Not_Once_Per_Corner__Review()
    {
        // Revisión de F008: cada esquina que toca la mesa y cada choque entre dos dados daban un
        // sonido aparte en el mismo instante.
        var page = await OpenAsync(browser, Viewports.Phone, History("BlackIron"));

        var sounds = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              await dice3d.roll(board, { dice: [{ shape: 'D6', face: '4', wave: 0, penetration: false, parent: null }] },
                { material: 'Stone', color: '#8c8a84', ink: '#1f1d1a', effect: 'None', sound: true, quality: 'High' });
              return JSON.parse(JSON.stringify(dice3d.heardSounds(board)));
            }
            """);

        var times = sounds.EnumerateArray().Select(sound => sound.GetProperty("at").GetDouble()).ToList();
        Assert.NotEmpty(times);
        Assert.Equal(times.Count, times.Distinct().Count());
    }

    [E2EFact]
    public async Task A_Penetrating_D100_Shows_One_Effect_Not_Two__FR017()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var effects = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              await dice3d.roll(board, { dice: [
                { shape: 'D10Tens', face: '00', wave: 0, penetration: false, parent: null },
                { shape: 'D10', face: '0', wave: 0, penetration: false, parent: null },
                { shape: 'D10Tens', face: '30', wave: 1, penetration: true, parent: 1 },
                { shape: 'D10', face: '7', wave: 1, penetration: true, parent: 1 },
              ] }, { material: 'Ivory', color: '#f4efe6', ink: '#1f2328', effect: 'Explosion', sound: true, quality: 'High' });
              return JSON.parse(JSON.stringify({ effects: dice3d.shownEffects(board), sounds: dice3d.heardSounds(board).filter(s => s.kind === 'Explosion') }));
            }
            """);

        Assert.Equal(1, effects.GetProperty("effects").GetArrayLength());
        Assert.Equal(1, effects.GetProperty("sounds").GetArrayLength());
    }

    [E2EFact]
    public async Task The_Aura_Stands_Out_Also_On_Gold_Dice__FR007()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        var dice = await page.EvaluateAsync<JsonElement>("""
            async () => {
              const dice3d = await import('./js/dice3d.js');
              const board = document.querySelector('[data-testid=dice-board]');
              const roll = color => dice3d.roll(board, { dice: [
                { shape: 'D6', face: '6', wave: 0, penetration: false, parent: null },
                { shape: 'D6', face: '1', wave: 1, penetration: true, parent: 0 },
              ] }, { material: 'OldGold', color, ink: '#2a1608', effect: 'None', sound: false, quality: 'High' }).then(() => dice3d.shownDice(board)[1].auraColor);
              return JSON.parse(JSON.stringify([await roll('#d4a842'), await roll('#2f5a46')]));
            }
            """);

        Assert.NotEqual(dice[0].GetString(), dice[1].GetString());
    }

    [E2EFact]
    public async Task Muted_Dice_Make_No_Sound__FR009()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("BlackIron"));

        var result = await page.EvaluateAsync<JsonElement>(NoisyRoll, SoundLook(sound: false));

        Assert.Equal(0, result.GetProperty("sounds").GetArrayLength());
    }

    [E2EFact]
    public async Task If_Audio_Fails_The_Roll_Still_Ends__FR010()
    {
        var page = await browser.NewPageAsync(Viewports.Phone);
        await page.AddInitScriptAsync("window.AudioContext = function () { throw new Error('NotAllowedError'); }; window.webkitAudioContext = undefined;");
        await BrowserFixture.GoToAsync(page, "dados");
        await page.EvaluateAsync("value => localStorage.setItem('hackmaster.rolls', value)", History("BlackIron"));
        await BrowserFixture.GoToAsync(page, "dados");
        await page.Locator("[data-testid=dice3d-toggle][data-ready=true]").WaitForAsync();

        var result = await page.EvaluateAsync<JsonElement>(NoisyRoll, SoundLook(sound: true));

        Assert.Equal(7, result.GetProperty("faces").GetArrayLength());
    }

    [E2EFact]
    public async Task Low_Quality_Draws_Plain_Colors_Without_Textures_Reflections_Or_Transmission__FR011()
    {
        var page = await OpenAsync(browser, Viewports.Phone, History("Ivory"));

        foreach (var material in new[] { "Marble", "Crystal", "OldGold" })
        {
            var dice = await page.EvaluateAsync<JsonElement>("""
                async material => {
                  const dice3d = await import('./js/dice3d.js');
                  const board = document.querySelector('[data-testid=dice-board]');
                  await dice3d.roll(board, { dice: [{ shape: 'D20', face: '13', wave: 0, penetration: false, parent: null }] },
                    { material, color: '#808080', ink: '#000000', effect: 'None', sound: false, quality: 'Low' });
                  return JSON.parse(JSON.stringify(dice3d.shownDice(board)));
                }
                """, material);

            var die = dice[0];
            Assert.Equal("13", die.GetProperty("face").GetString());
            Assert.False(die.GetProperty("textured").GetBoolean(), $"{material} con calidad baja lleva textura.");
            Assert.False(die.GetProperty("transmission").GetBoolean(), $"{material} con calidad baja es translúcido.");
            Assert.False(die.GetProperty("reflective").GetBoolean(), $"{material} con calidad baja tiene reflejos (cuerpo o números).");
        }
    }

    [E2EFact]
    public async Task The_Open_Panel_Does_Not_Overflow_And_Its_Buttons_Measure_44_Pixels__FR016()
    {
        foreach (var viewport in Viewports.All)
        {
            var page = await OpenAsync(browser, viewport, History("Ivory"));
            await page.Locator("[data-testid=look-open]").ClickAsync();
            await page.Locator("[data-testid=look-preview] canvas").WaitForAsync();

            var result = await page.EvaluateAsync<JsonElement>("""
                () => ({
                  overflow: document.documentElement.scrollWidth > document.documentElement.clientWidth,
                  small: [...document.querySelectorAll('.dice-look button')]
                    .filter(button => button.offsetParent !== null)
                    .map(button => button.getBoundingClientRect())
                    .filter(box => box.width < 44 || box.height < 44).length,
                })
                """);

            Assert.False(result.GetProperty("overflow").GetBoolean(), $"Desborda a {viewport.Width} px con el panel abierto.");
            Assert.Equal(0, result.GetProperty("small").GetInt32());
            await page.CloseAsync();
        }
    }
}
