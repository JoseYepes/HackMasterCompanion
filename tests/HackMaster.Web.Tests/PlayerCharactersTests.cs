using Bunit;
using HackMaster.Domain;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests;

/// <summary>Fichas guardadas en el dispositivo: guardar, copiar, borrar, exportar e importar (F004).</summary>
public class PlayerCharactersTests : BunitContext
{
    private const string Key = "hackmaster.characters";
    private static readonly DateTimeOffset At = new(2026, 11, 19, 12, 0, 0, TimeSpan.FromHours(1));

    private readonly BunitJSModuleInterop _module;

    public PlayerCharactersTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _module = JSInterop.SetupModule(IndexedStore.ModulePath);
    }

    private PlayerCharacters NewService() => new(new IndexedStore(JSInterop.JSRuntime));

    private static Character Sample(string id = "ficha-1", string name = "Grimbold") => new()
    {
        Id = id,
        Name = name,
        Class = "Guerrero",
        Level = 3,
        Abilities = CharacterAbilities.Empty.With(Ability.FUE, new AbilityScore(15, 47)),
        Skills = [new CharacterSkill("rastrear", "Rastrear", 25, 2)],
        CreatedAt = At,
        UpdatedAt = At,
    };

    private string LastSaved => (string)_module.Invocations["set"].Last().Arguments[1]!;

    [Fact]
    public async Task A_Saved_Character_Is_Written_To_The_Device_And_Listed__FR004()
    {
        var service = NewService();

        var problem = await service.SaveAsync(Sample());

        Assert.Null(problem);
        Assert.Equal("Grimbold", Assert.Single(service.Characters).Name);
        Assert.Equal(Key, _module.Invocations["set"].Last().Arguments[0]);
        Assert.True(Sample().HasSameData(Assert.Single(StoredData.ReadCharacters(LastSaved).Value!.Characters)));
    }

    [Fact]
    public async Task Saving_Again_Replaces_The_Character_With_The_Same_Id__FR002()
    {
        var service = NewService();
        await service.SaveAsync(Sample());

        await service.SaveAsync(Sample() with { Name = "Grimbold el Rojo" });

        Assert.Equal("Grimbold el Rojo", Assert.Single(service.Characters).Name);
    }

    [Fact]
    public async Task A_Character_With_Problems_Is_Not_Saved__FR002()
    {
        var service = NewService();

        var problem = await service.SaveAsync(Sample() with { Name = " " });

        Assert.Equal("Escribe un nombre para la ficha.", problem);
        Assert.Empty(service.Characters);
        Assert.Empty(_module.Invocations["set"]);
    }

    [Fact]
    public async Task Characters_Are_Listed_By_Name_And_Names_Can_Repeat__FR001()
    {
        var service = NewService();
        await service.SaveAsync(Sample("c", "Zora"));
        await service.SaveAsync(Sample("a", "Ásgar"));
        await service.SaveAsync(Sample("b", "Ásgar"));

        Assert.Equal(["a", "b", "c"], service.Characters.Select(character => character.Id));
    }

    [Fact]
    public async Task Saved_Characters_Are_Loaded_From_The_Device__FR004()
    {
        _module.Setup<string?>("get", Key).SetResult(StoredData.Write(new CharactersFile(2, [Sample()])));
        var service = NewService();

        await service.LoadAsync();

        Assert.Null(service.LoadError);
        Assert.True(Sample().HasSameData(service.Find("ficha-1")!));
    }

    [Fact]
    public async Task Saving_Rereads_The_Device_First_So_Another_Tab_Is_Not_Lost__FR004()
    {
        var service = NewService();
        await service.LoadAsync();
        _module.Setup<string?>("get", Key).SetResult(StoredData.Write(new CharactersFile(2, [Sample("otra", "De otra pestaña")])));

        await service.SaveAsync(Sample());

        Assert.Equal(["De otra pestaña", "Grimbold"], StoredData.ReadCharacters(LastSaved).Value!.Characters.Select(character => character.Name).Order());
    }

    [Fact]
    public async Task If_The_Saved_Characters_Cannot_Be_Read_Nothing_Is_Written__FR010()
    {
        _module.Setup<string?>("get", Key).SetResult("""{ "schemaVersion": 9, "characters": [] }""");
        var service = NewService();
        await service.LoadAsync();

        var problem = await service.SaveAsync(Sample());

        Assert.NotNull(service.LoadError);
        Assert.Contains("No se guarda", problem, StringComparison.Ordinal);
        Assert.Empty(_module.Invocations["set"]);
    }

    [Fact]
    public async Task If_The_Browser_Cannot_Save_The_Characters_Stay_In_Memory_And_It_Is_Reported__FR004()
    {
        _module.SetupVoid("set", _ => true).SetException(new JSException("QuotaExceededError"));
        var store = new IndexedStore(JSInterop.JSRuntime);
        var service = new PlayerCharacters(store);
        var reported = false;
        store.AvailabilityChanged += () => reported = true;

        await service.SaveAsync(Sample());

        Assert.True(reported);
        Assert.False(store.IsAvailable);
        Assert.Equal("Grimbold", Assert.Single(service.Characters).Name);
    }

    [Fact]
    public async Task A_Duplicate_Is_A_New_Independent_Character__FR005()
    {
        var service = NewService();
        await service.SaveAsync(Sample());

        var (copy, _) = await service.DuplicateAsync("ficha-1", "ficha-2", At.AddHours(1));

        Assert.Equal("Grimbold (copia)", copy!.Name);
        Assert.Equal(["Grimbold", "Grimbold (copia)"], service.Characters.Select(character => character.Name));
        Assert.Equal(2, StoredData.ReadCharacters(LastSaved).Value!.Characters.Count);
    }

    [Fact]
    public async Task A_Deleted_Character_Disappears_From_The_Device__FR006()
    {
        var service = NewService();
        await service.SaveAsync(Sample());
        await service.SaveAsync(Sample("ficha-2", "Otra"));

        await service.DeleteAsync("ficha-1");

        Assert.Equal("Otra", Assert.Single(service.Characters).Name);
        Assert.Equal("Otra", Assert.Single(StoredData.ReadCharacters(LastSaved).Value!.Characters).Name);
    }

    [Fact]
    public async Task Export_Downloads_The_Character_File_Named_After_The_Character__FR007()
    {
        var service = NewService();

        await service.ExportAsync(Sample() with { Name = "Grimbold el Rojo" });

        var download = _module.Invocations["download"].Single();
        Assert.Equal("grimbold-el-rojo.hackmaster.json", download.Arguments[0]);
        Assert.True((Sample() with { Name = "Grimbold el Rojo" }).HasSameData(StoredData.ReadCharacterFile((string)download.Arguments[1]!).Value!.Character));
    }

    [Fact]
    public async Task A_Character_Without_A_Usable_Name_Is_Exported_As_Ficha__FR007()
    {
        await NewService().ExportAsync(Sample() with { Name = "¡¿?!" });

        Assert.Equal("ficha.hackmaster.json", _module.Invocations["download"].Single().Arguments[0]);
    }

    [Fact]
    public async Task Importing_A_New_Character_Adds_It_Identical__FR007()
    {
        var service = NewService();
        await service.LoadAsync();

        var import = await service.ReadImportAsync(StoredData.WriteCharacterFile(Sample()));
        Assert.Null(import.Error);
        Assert.False(import.AlreadyExists);
        await service.ImportAsync(import.Character!, asCopy: false, "nueva", At.AddDays(1), replace: false);

        Assert.True(Sample().HasSameData(Assert.Single(service.Characters)));
    }

    [Fact]
    public async Task Importing_An_Existing_Character_Can_Replace_It_Or_Add_A_Copy__FR008()
    {
        var service = NewService();
        await service.SaveAsync(Sample());
        var changed = Sample() with { Notes = "Cambiada en otro dispositivo" };

        var import = await service.ReadImportAsync(StoredData.WriteCharacterFile(changed));
        Assert.True(import.AlreadyExists);
        await service.ImportAsync(import.Character!, asCopy: true, "ficha-2", At.AddDays(1), replace: false);
        Assert.Equal(["Grimbold", "Grimbold (copia)"], service.Characters.Select(character => character.Name));
        Assert.Equal("Cambiada en otro dispositivo", service.Find("ficha-2")!.Notes);

        await service.ImportAsync(import.Character!, asCopy: false, "no-se-usa", At.AddDays(1), replace: true);
        Assert.True(changed.HasSameData(service.Find("ficha-1")!));
        Assert.Equal(2, service.Characters.Count);
    }

    [Fact]
    public async Task A_Bad_File_Is_Reported_And_Nothing_Changes__FR009()
    {
        var service = NewService();
        await service.SaveAsync(Sample());
        var writes = _module.Invocations["set"].Count;

        var import = await service.ReadImportAsync("""{ "schemaVersion": 2, "kind": "otra-cosa" }""");

        Assert.Null(import.Character);
        Assert.Equal("El archivo no es una ficha de HackMaster Companion.", import.Error);
        Assert.Single(service.Characters);
        Assert.Equal(writes, _module.Invocations["set"].Count);
    }

    // Revisión independiente de F004 (H1, H2, H5): otra pestaña puede haber cambiado lo guardado.
    private void OtherTabSaved(params Character[] characters) =>
        _module.Setup<string?>("get", Key).SetResult(StoredData.Write(new CharactersFile(2, characters)));

    [Fact]
    public async Task Loading_Again_Shows_What_Another_Tab_Saved__Review()
    {
        var service = NewService();
        await service.LoadAsync();
        OtherTabSaved(Sample("otra", "De otra pestaña"));

        await service.LoadAsync();

        Assert.Equal("De otra pestaña", Assert.Single(service.Characters).Name);
    }

    [Fact]
    public async Task Importing_Asks_Even_If_Another_Tab_Added_The_Character_Meanwhile__FR008_Review()
    {
        var service = NewService();
        await service.LoadAsync();
        OtherTabSaved(Sample() with { Notes = "Editada en otra pestaña" });

        var import = await service.ReadImportAsync(StoredData.WriteCharacterFile(Sample()));

        Assert.True(import.AlreadyExists);
    }

    [Fact]
    public async Task A_Plain_Import_Never_Overwrites_A_Character_That_Appeared_Before_Writing__FR008_Review()
    {
        var service = NewService();
        var import = await service.ReadImportAsync(StoredData.WriteCharacterFile(Sample()));
        Assert.False(import.AlreadyExists);
        OtherTabSaved(Sample() with { Notes = "Editada en otra pestaña" });

        var problem = await service.ImportAsync(import.Character!, asCopy: false, "nueva", At, replace: false);

        Assert.Equal(PlayerCharacters.AlreadyExists, problem);
        Assert.Equal("Editada en otra pestaña", service.Find("ficha-1")!.Notes);
    }

    [Fact]
    public async Task Saving_A_Character_Deleted_In_Another_Tab_Does_Not_Bring_It_Back__Review()
    {
        var service = NewService();
        await service.SaveAsync(Sample(), SaveMode.Create);
        OtherTabSaved();

        var problem = await service.SaveAsync(Sample() with { Notes = "Cambio" }, SaveMode.Update);

        Assert.Contains("se borró", problem, StringComparison.Ordinal);
        Assert.Empty(service.Characters);
    }

    [Fact]
    public async Task Creating_Twice_With_The_Same_Id_Keeps_A_Single_Character__Review()
    {
        var service = NewService();

        Assert.Null(await service.SaveAsync(Sample(), SaveMode.Create));
        Assert.Equal(PlayerCharacters.AlreadyExists, await service.SaveAsync(Sample(), SaveMode.Create));

        Assert.Single(service.Characters);
    }

    [Fact]
    public async Task If_The_Browser_Cannot_Read_The_Saved_Characters_It_Says_So_And_Writes_Nothing__FR010_Review()
    {
        _module.Setup<string?>("get", _ => true).SetException(new JSException("InvalidStateError"));
        var service = NewService();

        await service.LoadAsync();
        var problem = await service.SaveAsync(Sample());

        Assert.Contains("no dejó leer", service.LoadError, StringComparison.Ordinal);
        Assert.NotNull(problem);
        Assert.Empty(_module.Invocations["set"]);
    }

    [Fact]
    public async Task A_Failed_Export_Is_Reported_Instead_Of_Breaking_The_Page__Review()
    {
        _module.SetupVoid("download", _ => true).SetException(new JSException("Error"));

        var problem = await NewService().ExportAsync(Sample());

        Assert.Contains("No se pudo exportar", problem, StringComparison.Ordinal);
    }
}
