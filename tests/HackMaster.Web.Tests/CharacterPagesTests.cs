using Bunit;
using HackMaster.Domain;
using HackMaster.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace HackMaster.Web.Tests;

/// <summary>Páginas de fichas: lista, editor, exportar, importar, duplicar y borrar (F004).</summary>
public class CharacterPagesTests : BunitContext
{
    private static readonly DateTimeOffset At = new(2026, 11, 19, 12, 0, 0, TimeSpan.FromHours(1));

    private readonly PlayerCharacters _characters;
    private readonly BunitJSModuleInterop _module;
    private readonly QueueRandom _random = new();

    public CharacterPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(IndexedStore.ModulePath);
        Services.AddSingleton(TestData.Create());
        var store = new IndexedStore(JSInterop.JSRuntime);
        Services.AddSingleton(store);
        _characters = new PlayerCharacters(store);
        Services.AddSingleton(_characters);
        Services.AddSingleton(new BrowserStore(JSInterop.JSRuntime));
        Services.AddSingleton<IRandom>(_random);
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private static Character Grimbold(string id = "ficha-1") => new()
    {
        Id = id,
        Name = "Grimbold",
        Race = "Enano",
        Class = "Guerrero",
        Level = 3,
        Abilities = CharacterAbilities.Empty.With(Ability.FUE, new AbilityScore(15, 47)),
        Skills = [new CharacterSkill("rastrear", "Rastrear", 25, 2)],
        Notes = "Notas",
        CreatedAt = At,
        UpdatedAt = At,
    };

    private void Save(Character character) => Assert.Null(_characters.SaveAsync(character).GetAwaiter().GetResult());

    private IRenderedComponent<CharacterEditor> Edit(string? id)
    {
        Navigation.NavigateTo(id is null ? "fichas/nueva" : $"fichas/{id}");
        return Render<CharacterEditor>(parameters => parameters.Add(page => page.Id, id));
    }

    [Fact]
    public void Without_Characters_The_List_Says_So_And_Offers_To_Create_One__FR001()
    {
        var cut = Render<Characters>();

        Assert.Contains("Aún no hay fichas", cut.Find("[data-testid=characters-empty]").TextContent, StringComparison.Ordinal);
        Assert.Equal("fichas/nueva", cut.Find("[data-testid=character-new]").GetAttribute("href"));
    }

    [Fact]
    public void The_List_Shows_Name_Race_Class_And_Level_With_A_Link__FR001()
    {
        Save(Grimbold());

        var cut = Render<Characters>();

        var row = cut.Find("[data-testid=character-row]");
        Assert.Equal("Grimbold", row.QuerySelector(".character-name")!.TextContent.Trim());
        Assert.Equal("Enano · Guerrero · nivel 3", row.QuerySelector(".character-summary")!.TextContent.Trim());
        Assert.Equal("fichas/ficha-1", row.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void A_New_Character_Needs_A_Name_Before_Saving__FR002()
    {
        var cut = Edit(null);

        cut.Find("[data-testid=character-save]").Click();

        Assert.Contains("Escribe un nombre para la ficha.", cut.Find("[data-testid=character-problem]").TextContent, StringComparison.Ordinal);
        Assert.Empty(_characters.Characters);
    }

    [Fact]
    public void Filling_And_Saving_A_New_Character_Keeps_Every_Field__FR002()
    {
        var cut = Edit(null);

        cut.Find("[data-testid=character-name]").Input("Grimbold");
        cut.Find("[data-testid=character-race]").Input("Enano");
        cut.Find("[data-testid=character-class]").Input("Guerrero");
        cut.Find("[data-testid=character-level]").Input("3");
        cut.Find("[data-testid=character-alignment]").Input("Legal neutral");
        cut.Find("[data-testid=character-ability-FUE]").Input("15");
        cut.Find("[data-testid=character-fraction-FUE]").Input("47");
        cut.Find("[data-testid=character-ability-ASP]").Input("9");
        cut.Find("[data-testid=character-hp]").Input("31");
        cut.Find("[data-testid=character-honor]").Input("-2");
        cut.Find("[data-testid=character-equipment]").Input("Hacha");
        cut.Find("[data-testid=character-notes]").Input("Notas");
        cut.Find("[data-testid=character-save]").Click();

        var saved = Assert.Single(_characters.Characters);
        Assert.Equal(("Grimbold", "Enano", "Guerrero", 3, "Legal neutral"), (saved.Name, saved.Race, saved.Class, saved.Level, saved.Alignment));
        Assert.Equal(new AbilityScore(15, 47), saved.Abilities.FUE);
        Assert.Equal(new AbilityScore(9), saved.Abilities.ASP);
        Assert.Equal((31, -2, "Hacha", "Notas"), (saved.HitPoints, saved.Honor, saved.Equipment, saved.Notes));
        Assert.False(string.IsNullOrWhiteSpace(saved.Id));
        Assert.EndsWith("/fichas", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Editing_Shows_The_Saved_Values_And_Saves_The_Change__FR002()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        Assert.Equal("Grimbold", cut.Find("[data-testid=character-name]").GetAttribute("value"));
        Assert.Equal("47", cut.Find("[data-testid=character-fraction-FUE]").GetAttribute("value"));
        cut.Find("[data-testid=character-name]").Input("Grimbold el Rojo");
        cut.Find("[data-testid=character-level]").Input("");
        cut.Find("[data-testid=character-save]").Click();

        var saved = _characters.Find("ficha-1")!;
        Assert.Equal("Grimbold el Rojo", saved.Name);
        Assert.Null(saved.Level);
        Assert.Equal(At, saved.CreatedAt);
        Assert.True(saved.UpdatedAt > At);
    }

    [Fact]
    public void Skills_Are_Found_Without_Accents_Or_Capitals_And_Added_With_Mastery_And_Points__FR003()
    {
        var cut = Edit(null);
        cut.Find("[data-testid=character-name]").Input("Grimbold");

        cut.Find("[data-testid=character-skill-search]").Input("BOTANI");
        var result = Assert.Single(cut.FindAll("[data-testid=character-skill-result]"));
        Assert.Contains("Botánica", result.TextContent, StringComparison.Ordinal);
        result.QuerySelector("button")!.Click();
        cut.Find("[data-testid=character-skill-mastery]").Input("30");
        cut.Find("[data-testid=character-skill-points]").Input("4");
        cut.Find("[data-testid=character-save]").Click();

        Assert.Equal([new CharacterSkill("botanica", "Botánica", 30, 4)], Assert.Single(_characters.Characters).Skills);
    }

    [Fact]
    public void A_Skill_Already_In_The_Character_Is_Not_Added_Twice__FR003()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-skill-search]").Input("rastrear");
        cut.Find("[data-testid=character-skill-result] button").Click();

        Assert.Single(cut.FindAll("[data-testid=character-skill]"));
    }

    [Fact]
    public void A_Skill_Can_Be_Removed__FR003()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-skill] [data-testid=character-skill-remove]").Click();
        cut.Find("[data-testid=character-save]").Click();

        Assert.Empty(_characters.Find("ficha-1")!.Skills);
    }

    [Fact]
    public void A_Skill_That_No_Longer_Exists_Is_Kept_And_Marked__FR003()
    {
        Save(Grimbold() with { Skills = [new CharacterSkill("desaparecida", "Habilidad antigua", 10, 1)] });

        var cut = Edit("ficha-1");

        var skill = cut.Find("[data-testid=character-skill]");
        Assert.Contains("Habilidad antigua", skill.TextContent, StringComparison.Ordinal);
        Assert.NotNull(skill.QuerySelector("[data-testid=character-skill-missing]"));
        cut.Find("[data-testid=character-save]").Click();
        Assert.Equal("desaparecida", Assert.Single(_characters.Find("ficha-1")!.Skills).Slug);
    }

    [Fact]
    public void An_Unknown_Character_Shows_Not_Found__FR002()
    {
        var cut = Edit("no-existe");

        Assert.Contains("Página no encontrada", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_Downloads_The_Character_File__FR007()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-export]").Click();

        var download = _module.Invocations["download"].Single();
        Assert.Equal("grimbold.hackmaster.json", download.Arguments[0]);
        Assert.True(Grimbold().HasSameData(StoredData.ReadCharacterFile((string)download.Arguments[1]!).Value!.Character));
    }

    [Fact]
    public void Duplicate_Creates_A_Copy_And_Opens_It__FR005()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-duplicate]").Click();

        Assert.Equal(["Grimbold", "Grimbold (copia)"], _characters.Characters.Select(character => character.Name));
        var copy = _characters.Characters[1];
        Assert.EndsWith($"/fichas/{copy.Id}", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Delete_Asks_First_And_Only_Deletes_If_Confirmed__FR006()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-delete]").Click();
        Assert.Contains("¿Borrar la ficha «Grimbold»?", cut.Find("[data-testid=character-delete-question]").TextContent, StringComparison.Ordinal);
        cut.Find("[data-testid=character-delete-cancel]").Click();
        Assert.Single(_characters.Characters);

        cut.Find("[data-testid=character-delete]").Click();
        cut.Find("[data-testid=character-delete-confirm]").Click();

        Assert.Empty(_characters.Characters);
        Assert.EndsWith("/fichas", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Importing_A_New_Character_Adds_It_To_The_List__FR007()
    {
        var cut = Render<Characters>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(StoredData.WriteCharacterFile(Grimbold()), "grimbold.hackmaster.json"));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid=character-row]")));
        Assert.True(Grimbold().HasSameData(Assert.Single(_characters.Characters)));
        Assert.Contains("Grimbold", cut.Find("[data-testid=character-import-done]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Importing_An_Existing_Character_Asks_To_Replace_Or_Copy__FR008()
    {
        Save(Grimbold());
        var cut = Render<Characters>();
        var changed = Grimbold() with { Notes = "Otra versión" };

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(StoredData.WriteCharacterFile(changed), "grimbold.hackmaster.json"));
        cut.WaitForElement("[data-testid=character-import-conflict]");
        cut.Find("[data-testid=character-import-copy]").Click();

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("[data-testid=character-row]").Count));
        Assert.Equal("Notas", _characters.Find("ficha-1")!.Notes);

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(StoredData.WriteCharacterFile(changed), "grimbold.hackmaster.json"));
        cut.WaitForElement("[data-testid=character-import-conflict]");
        cut.Find("[data-testid=character-import-replace]").Click();

        cut.WaitForAssertion(() => Assert.Equal("Otra versión", _characters.Find("ficha-1")!.Notes));
        Assert.Equal(2, _characters.Characters.Count);
    }

    [Fact]
    public void Importing_A_Bad_File_Shows_The_Reason_And_Changes_Nothing__FR009()
    {
        Save(Grimbold());
        var cut = Render<Characters>();

        cut.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("esto no es una ficha", "nota.txt"));

        cut.WaitForAssertion(() => Assert.Contains("dañado", cut.Find("[data-testid=character-import-error]").TextContent, StringComparison.Ordinal));
        Assert.True(Grimbold().HasSameData(Assert.Single(_characters.Characters)));
    }

    [Fact]
    public void If_The_Saved_Characters_Cannot_Be_Read_It_Is_Reported__FR010()
    {
        _module.Setup<string?>("get", "hackmaster.characters").SetResult("""{ "schemaVersion": 9, "characters": [] }""");

        var cut = Render<Characters>();

        Assert.Contains("No se pudieron leer tus fichas", cut.Find("[data-testid=characters-error]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_Uses_What_Is_On_Screen_Even_If_Not_Saved_Yet__Review()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");
        cut.Find("[data-testid=character-notes]").Input("Sin guardar todavía");

        cut.Find("[data-testid=character-export]").Click();

        var exported = StoredData.ReadCharacterFile((string)_module.Invocations["download"].Single().Arguments[1]!).Value!.Character;
        Assert.Equal("Sin guardar todavía", exported.Notes);
    }

    [Fact]
    public void Duplicate_Saves_The_Changes_On_Screen_First__Review()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");
        cut.Find("[data-testid=character-notes]").Input("Cambio antes de copiar");

        cut.Find("[data-testid=character-duplicate]").Click();

        Assert.All(_characters.Characters, character => Assert.Equal("Cambio antes de copiar", character.Notes));
        Assert.Equal(2, _characters.Characters.Count);
    }

    [Theory]
    [InlineData("character-level", "1.5", "«Nivel» debe ser un número entero.")]
    [InlineData("character-hp", "abc", "«PG» debe ser un número entero.")]
    [InlineData("character-fraction-FUE", "99999999999", "«Fracción de Fuerza» debe ser un número entero.")]
    public void A_Number_That_Is_Not_Whole_Is_Reported_Instead_Of_Being_Lost__Review(string field, string text, string problem)
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        cut.Find($"[data-testid={field}]").Input(text);
        cut.Find("[data-testid=character-save]").Click();

        Assert.Contains(problem, cut.Find("[data-testid=character-problem]").TextContent, StringComparison.Ordinal);
        Assert.True(Grimbold().HasSameData(_characters.Find("ficha-1")!));
    }

    [Fact]
    public void Repeated_Skills_From_A_File_Are_Changed_And_Removed_One_By_One__Review()
    {
        var skill = new CharacterSkill("rastrear", "Rastrear", 10, 1);
        Save(Grimbold() with { Skills = [skill, skill] });
        var cut = Edit("ficha-1");

        cut.FindAll("[data-testid=character-skill-mastery]")[1].Input("40");
        cut.FindAll("[data-testid=character-skill-remove]")[0].Click();
        cut.Find("[data-testid=character-save]").Click();

        Assert.Equal([skill with { Mastery = 40 }], _characters.Find("ficha-1")!.Skills);
    }

    [Fact]
    public void Skill_Controls_Say_Which_Skill_They_Belong_To__Review()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");
        cut.Find("[data-testid=character-skill-search]").Input("botani");

        Assert.Equal("Añadir Botánica", cut.Find("[data-testid=character-skill-result] button").GetAttribute("aria-label"));
        Assert.Equal("Maestría escrita de Rastrear", cut.Find("[data-testid=character-skill-mastery]").GetAttribute("aria-label"));
        Assert.Equal("Puntos de Rastrear", cut.Find("[data-testid=character-skill-points]").GetAttribute("aria-label"));
        Assert.Equal("Quitar Rastrear", cut.Find("[data-testid=character-skill-remove]").GetAttribute("aria-label"));
    }

    [Fact]
    public void A_Character_Added_By_Another_Tab_Appears_In_The_List__Review()
    {
        _module.Setup<string?>("get", "hackmaster.characters").SetResult(StoredData.Write(new CharactersFile(2, [Grimbold()])));

        var cut = Render<Characters>();

        Assert.Single(cut.FindAll("[data-testid=character-row]"));
    }

    // F005: maestría calculada, mejoras y prueba de habilidad desde la ficha.
    private static Character Scholar(params CharacterSkill[] skills) => Grimbold() with
    {
        Abilities = CharacterAbilities.Empty.With(Ability.INT, new AbilityScore(14, 30)),
        HonorDie = new HonorDie(12, 1),
        Skills = skills,
    };

    private static CharacterSkill Botanica(params SkillImprovement[] improvements) =>
        new("botanica", "Botánica") { Improvements = improvements };

    [Fact]
    public void The_Honor_Die_Is_Chosen_From_Table_5C_And_Saved__F005_MJ85()
    {
        Save(Grimbold());
        var cut = Edit("ficha-1");

        var options = cut.FindAll("[data-testid=character-honor-die] option").Select(option => option.TextContent.Trim()).ToList();
        cut.Find("[data-testid=character-honor-die]").Change("d12+1");
        cut.Find("[data-testid=character-save]").Click();

        Assert.Equal(["Sin elegir", .. HonorDie.All.Select(die => die.ToString())], options);
        Assert.Equal(new HonorDie(12, 1), _characters.Find("ficha-1")!.HonorDie);
    }

    [Fact]
    public void A_New_Skill_Starts_At_Its_Ability_And_Shows_Its_Level__F005_MJ307()
    {
        Save(Scholar());
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-skill-search]").Input("botani");
        cut.Find("[data-testid=character-skill-result] button").Click();

        var value = cut.Find("[data-testid=character-skill-value]").TextContent;
        Assert.Contains("14 %", value, StringComparison.Ordinal);
        Assert.Contains("principiante", value, StringComparison.Ordinal);
        Assert.Contains("INT 14", cut.Find("[data-testid=character-skill-breakdown]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Written_Down_Improvement_Adds_To_The_Mastery_And_Is_Saved__F005_MJ100()
    {
        Save(Scholar(Botanica()));
        var cut = Edit("ficha-1");

        cut.Find("[data-testid=character-skill-new-mastery]").Input("3");
        cut.Find("[data-testid=character-skill-new-honor]").Input("8");
        cut.Find("[data-testid=character-skill-add-improvement]").Click();

        Assert.Contains("25 %", cut.Find("[data-testid=character-skill-value]").TextContent, StringComparison.Ordinal);
        cut.Find("[data-testid=character-save]").Click();
        Assert.Equal([new SkillImprovement(3, 8)], _characters.Find("ficha-1")!.Skills[0].Improvements);
    }

    [Fact]
    public void An_Improvement_Can_Be_Removed__F005()
    {
        Save(Scholar(Botanica(new SkillImprovement(3, 8), new SkillImprovement(2, 2))));
        var cut = Edit("ficha-1");

        cut.FindAll("[data-testid=character-skill-improvement-remove]")[0].Click();
        cut.Find("[data-testid=character-save]").Click();

        Assert.Equal([new SkillImprovement(2, 2)], _characters.Find("ficha-1")!.Skills[0].Improvements);
    }

    [Fact]
    public void Rolling_An_Improvement_Uses_The_Mastery_Die_And_The_Honor_Die__F005_MJ101()
    {
        Save(Scholar(Botanica()));
        var cut = Edit("ficha-1");
        _random.Enqueue(5, 7);

        cut.Find("[data-testid=character-skill-roll-improvement]").Click();

        var rolled = cut.Find("[data-testid=character-skill-rolled]").TextContent;
        Assert.Contains("d8: 5", rolled, StringComparison.Ordinal);
        Assert.Contains("d12+1: 8", rolled, StringComparison.Ordinal);
        Assert.Contains("27 %", cut.Find("[data-testid=character-skill-value]").TextContent, StringComparison.Ordinal);
        Assert.Equal([(1, 8), (1, 12)], _random.Requests);
    }

    [Fact]
    public void Without_An_Honor_Die_The_Improvement_Cannot_Be_Rolled__F005()
    {
        Save(Scholar(Botanica()) with { HonorDie = null });
        var cut = Edit("ficha-1");

        Assert.True(cut.Find("[data-testid=character-skill-roll-improvement]").HasAttribute("disabled"));
        Assert.Contains("Elige tu Dado de Honor", cut.Find("[data-testid=character-skill-roll-note]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void A_Mastery_Written_In_F004_Is_Kept_Until_Cleared__F005_FR006()
    {
        Save(Scholar(new CharacterSkill("botanica", "Botánica", 40, 2)));
        var cut = Edit("ficha-1");

        Assert.Contains("40 %", cut.Find("[data-testid=character-skill-value]").TextContent, StringComparison.Ordinal);
        Assert.Contains("escrita a mano", cut.Find("[data-testid=character-skill-breakdown]").TextContent, StringComparison.Ordinal);

        cut.Find("[data-testid=character-skill-mastery]").Input("");

        Assert.Contains("14 %", cut.Find("[data-testid=character-skill-value]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_The_Relevant_Ability_The_Mastery_Is_Pending__F005_MJ307()
    {
        Save(Grimbold() with { Skills = [Botanica()] });
        var cut = Edit("ficha-1");

        var value = cut.Find("[data-testid=character-skill-value]").TextContent;
        Assert.Contains("pendiente", value, StringComparison.Ordinal);
        Assert.Contains("INT", value, StringComparison.Ordinal);
        Assert.True(cut.Find("[data-testid=character-skill-check]").HasAttribute("disabled"));
    }

    [Fact]
    public void A_Skill_Check_Shows_The_Roll_And_Is_Kept_In_The_Dice_History__F005_MJ356()
    {
        Save(Scholar(Botanica(new SkillImprovement(3, 8))));
        var cut = Edit("ficha-1");
        _random.Enqueue(30);

        cut.Find("[data-testid=character-skill-modifier]").Input("-10");
        cut.Find("[data-testid=character-skill-check]").Click();

        var result = cut.Find("[data-testid=character-skill-check-result]").TextContent;
        Assert.Contains("d100: 30", result, StringComparison.Ordinal);
        Assert.Contains("total 20", result, StringComparison.Ordinal);
        Assert.Contains("25 %", result, StringComparison.Ordinal);
        Assert.Contains("Éxito", result, StringComparison.Ordinal);
        var saved = (string)JSInterop.Invocations["localStorage.setItem"].Single(call => (string)call.Arguments[0]! == "hackmaster.rolls").Arguments[1]!;
        var roll = Assert.Single(StoredData.ReadRolls(saved).Value!.Rolls);
        Assert.Equal(("1d100-10", 20), (roll.Formula, roll.Total));
    }

    [Theory]
    [InlineData(3, "Éxito automático (01–05)")]
    [InlineData(99, "Fallo automático (98–00)")]
    [InlineData(80, "Fallo")]
    public void A_Skill_Check_Says_Whether_It_Succeeded__F005_MJ356(int roll, string outcome)
    {
        Save(Scholar(Botanica(new SkillImprovement(3, 8))));
        var cut = Edit("ficha-1");
        _random.Enqueue(roll);

        cut.Find("[data-testid=character-skill-check]").Click();

        Assert.Equal(outcome, cut.Find("[data-testid=character-skill-check-result] strong").TextContent.Trim());
    }
}

/// <summary>Correcciones de la revisión independiente de F005.</summary>
public class CharacterRulesReviewTests : BunitContext
{
    private static readonly DateTimeOffset At = new(2026, 11, 20, 12, 0, 0, TimeSpan.FromHours(1));

    private readonly PlayerCharacters _characters;
    private readonly QueueRandom _random = new();

    public CharacterRulesReviewTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule(IndexedStore.ModulePath);
        Services.AddSingleton(TestData.Create());
        var store = new IndexedStore(JSInterop.JSRuntime);
        Services.AddSingleton(store);
        _characters = new PlayerCharacters(store);
        Services.AddSingleton(_characters);
        Services.AddSingleton(new BrowserStore(JSInterop.JSRuntime));
        Services.AddSingleton<IRandom>(_random);
    }

    private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

    private static Character Scholar(string id = "ficha-1", params CharacterSkill[] skills) => new()
    {
        Id = id,
        Name = "Erudito",
        Abilities = CharacterAbilities.Empty.With(Ability.INT, new AbilityScore(14)),
        HonorDie = new HonorDie(12, 1),
        Skills = skills.Length == 0 ? [new CharacterSkill("botanica", "Botánica") { Improvements = [new SkillImprovement(3, 8)] }] : skills,
        CreatedAt = At,
        UpdatedAt = At,
    };

    private IRenderedComponent<CharacterEditor> Edit(Character character)
    {
        Assert.Null(_characters.SaveAsync(character).GetAwaiter().GetResult());
        Navigation.NavigateTo($"fichas/{character.Id}");
        return Render<CharacterEditor>(parameters => parameters.Add(page => page.Id, character.Id));
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("abc")]
    [InlineData("20000")]
    [InlineData("-1001")]
    public void A_Modifier_That_Is_Not_A_Whole_Number_Up_To_1000_Blocks_The_Roll__Review(string text)
    {
        var cut = Edit(Scholar());

        cut.Find("[data-testid=character-skill-modifier]").Input(text);

        Assert.True(cut.Find("[data-testid=character-skill-check]").HasAttribute("disabled"));
        Assert.Contains("entre −1000 y 1000", cut.Find("[data-testid=character-skill-modifier-error]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Modifier_Says_It_Is_Added_To_The_Roll__Review_MJ356()
    {
        var cut = Edit(Scholar());

        Assert.Contains("se suma a la tirada", cut.Find("[data-testid=character-skill-modifier-help]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Result_Always_Says_Which_Modifier_Was_Used__Review()
    {
        var cut = Edit(Scholar());
        _random.Enqueue(30);

        cut.Find("[data-testid=character-skill-check]").Click();

        Assert.Contains("sin modificador", cut.Find("[data-testid=character-skill-check-result]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void While_There_Is_A_Written_Mastery_Improvements_Are_Disabled_And_It_Says_Why__Review()
    {
        var cut = Edit(Scholar(skills: new CharacterSkill("botanica", "Botánica", 40)));

        Assert.True(cut.Find("[data-testid=character-skill-roll-improvement]").HasAttribute("disabled"));
        Assert.True(cut.Find("[data-testid=character-skill-add-improvement]").HasAttribute("disabled"));
        Assert.Contains("maestría escrita", cut.Find("[data-testid=character-skill-roll-note]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Removing_An_Improvement_Clears_The_Message_Of_The_Rolled_One__Review()
    {
        var cut = Edit(Scholar());
        _random.Enqueue(5, 7);
        cut.Find("[data-testid=character-skill-roll-improvement]").Click();

        cut.FindAll("[data-testid=character-skill-improvement-remove]")[1].Click();

        Assert.Empty(cut.FindAll("[data-testid=character-skill-rolled]"));
    }

    [Fact]
    public async Task Opening_Another_Character_Clears_The_Results_Of_The_Previous_One__Review()
    {
        Assert.Null(await _characters.SaveAsync(Scholar("ficha-2")));
        var cut = Edit(Scholar());
        _random.Enqueue(30);
        cut.Find("[data-testid=character-skill-check]").Click();

        cut.Render(parameters => parameters.Add(page => page.Id, "ficha-2"));

        Assert.Empty(cut.FindAll("[data-testid=character-skill-check-result]"));
    }

    [Fact]
    public void If_The_Dice_History_Cannot_Be_Read_The_Result_Says_It_Was_Not_Saved__Review()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls").SetResult("{ dañado");
        var cut = Edit(Scholar());
        _random.Enqueue(30);

        cut.Find("[data-testid=character-skill-check]").Click();

        Assert.Contains("no se guardó en el historial", cut.Find("[data-testid=character-skill-check-result]").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void The_Result_Is_Announced_In_A_Live_Region_That_Exists_Before_Rolling__Review()
    {
        var cut = Edit(Scholar());

        Assert.Equal("polite", cut.Find("[data-testid=character-skill-live]").GetAttribute("aria-live"));
    }

    [Fact]
    public void Leaving_With_Unsaved_Changes_Asks_First__Review()
    {
        var cut = Edit(Scholar());
        JSInterop.Setup<bool>("confirm", _ => true).SetResult(false);
        cut.Find("[data-testid=character-notes]").Input("Sin guardar");

        Navigation.NavigateTo("fichas");

        Assert.EndsWith("/fichas/ficha-1", Navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void Saving_Leaves_Without_Asking__Review()
    {
        var cut = Edit(Scholar());
        cut.Find("[data-testid=character-notes]").Input("Guardada");

        cut.Find("[data-testid=character-save]").Click();

        Assert.EndsWith("/fichas", Navigation.Uri, StringComparison.Ordinal);
        Assert.Empty(JSInterop.Invocations["confirm"]);
    }
}

/// <summary>Azar de prueba: devuelve los valores que se le ponen en cola y apunta los intervalos pedidos.</summary>
public sealed class QueueRandom : IRandom
{
    private readonly Queue<int> _values = new();

    public List<(int Min, int Max)> Requests { get; } = [];

    public void Enqueue(params int[] values)
    {
        foreach (var value in values)
        {
            _values.Enqueue(value);
        }
    }

    public int Next(int minInclusive, int maxInclusive)
    {
        Requests.Add((minInclusive, maxInclusive));
        Assert.True(_values.Count > 0, "Se pidieron más números de los preparados.");
        return Math.Clamp(_values.Dequeue(), minInclusive, maxInclusive);
    }
}
