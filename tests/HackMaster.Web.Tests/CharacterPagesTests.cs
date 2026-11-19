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

    public CharacterPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(IndexedStore.ModulePath);
        Services.AddSingleton(TestData.Create());
        var store = new IndexedStore(JSInterop.JSRuntime);
        Services.AddSingleton(store);
        _characters = new PlayerCharacters(store);
        Services.AddSingleton(_characters);
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
}
