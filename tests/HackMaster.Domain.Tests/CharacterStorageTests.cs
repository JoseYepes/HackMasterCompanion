using System.Text.Json.Nodes;
using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>Formatos de las fichas: lo guardado en el dispositivo y el archivo de exportar (F004, principio V).</summary>
public class CharacterStorageTests
{
    /// <summary>Archivo de una ficha en la versión 1 (borrador de PRODUCTO.md B3: seis características).</summary>
    private const string Version1File = """
        {
          "schemaVersion": 1,
          "kind": "hackmaster.character",
          "character": {
            "id": "ficha-v1",
            "name": "Grimbold",
            "race": "Enano",
            "class": "Guerrero",
            "level": 3,
            "alignment": "Legal neutral",
            "abilities": {
              "FUE": { "score": 15, "fraction": 47 },
              "DES": { "score": 11 },
              "CON": { "score": 14 },
              "INT": { "score": 9 },
              "SAB": { "score": 10 },
              "CAR": { "score": 8 }
            },
            "hitPoints": 31,
            "honor": 12,
            "skills": [ { "slug": "actuar", "name": "Actuar", "mastery": 25, "points": 2 } ],
            "equipment": "Hacha",
            "notes": "Notas",
            "createdAt": "2026-11-19T10:00:00+01:00",
            "updatedAt": "2026-11-19T12:00:00+01:00"
          }
        }
        """;

    [Fact]
    public void An_Exported_Character_Is_Imported_Identical__SC001()
    {
        var original = CharacterTests.Full();

        var read = StoredData.ReadCharacterFile(StoredData.WriteCharacterFile(original));

        Assert.Null(read.Error);
        Assert.True(original.HasSameData(read.Value!.Character), "La ficha importada no es igual a la exportada.");
    }

    [Fact]
    public void An_Empty_Character_Is_Also_Imported_Identical__SC001()
    {
        var original = CharacterTests.Full() with
        {
            Race = null,
            Class = null,
            Level = null,
            Alignment = null,
            Abilities = CharacterAbilities.Empty,
            HitPoints = null,
            Honor = null,
            Skills = [],
            Equipment = null,
            Notes = null,
        };

        var read = StoredData.ReadCharacterFile(StoredData.WriteCharacterFile(original));

        Assert.Null(read.Error);
        Assert.True(original.HasSameData(read.Value!.Character));
    }

    [Fact]
    public void Saved_Characters_Survive_A_Write_And_Read__FR004()
    {
        var first = CharacterTests.Full();
        var second = first.Duplicate("ficha-2", first.UpdatedAt);

        var read = StoredData.ReadCharacters(StoredData.Write(new CharactersFile(StoredData.CharactersVersion, [first, second])));

        Assert.Null(read.Error);
        Assert.Equal(2, read.Value!.Characters.Count);
        Assert.True(first.HasSameData(read.Value.Characters[0]));
        Assert.True(second.HasSameData(read.Value.Characters[1]));
    }

    [Fact]
    public void Nothing_Saved_Is_An_Empty_List__FR004()
    {
        var read = StoredData.ReadCharacters(null);

        Assert.Null(read.Error);
        Assert.Empty(read.Value!.Characters);
    }

    [Fact]
    public void Both_Formats_Are_Written_With_Version_3__F005_FR006()
    {
        var character = CharacterTests.Full();

        var file = JsonNode.Parse(StoredData.WriteCharacterFile(character))!;
        var saved = JsonNode.Parse(StoredData.Write(new CharactersFile(1, [character])))!;

        Assert.Equal(3, StoredData.CharactersVersion);
        Assert.Equal(3, (int)file["schemaVersion"]!);
        Assert.Equal("hackmaster.character", (string)file["kind"]!);
        Assert.Equal(3, (int)saved["schemaVersion"]!);
    }

    [Fact]
    public void A_Version_1_File_Is_Migrated_With_An_Empty_Aspect_And_Everything_Else_Equal__FR009()
    {
        var read = StoredData.ReadCharacterFile(Version1File);

        Assert.Null(read.Error);
        var character = read.Value!.Character;
        Assert.Equal(StoredData.CharactersVersion, read.Value.SchemaVersion);
        Assert.Equal("ficha-v1", character.Id);
        Assert.Equal("Grimbold", character.Name);
        Assert.Equal("Enano", character.Race);
        Assert.Equal("Guerrero", character.Class);
        Assert.Equal(3, character.Level);
        Assert.Equal("Legal neutral", character.Alignment);
        Assert.Equal(new AbilityScore(15, 47), character.Abilities.FUE);
        Assert.Equal(new AbilityScore(11), character.Abilities.DES);
        Assert.Equal(new AbilityScore(14), character.Abilities.CON);
        Assert.Equal(new AbilityScore(9), character.Abilities.INT);
        Assert.Equal(new AbilityScore(10), character.Abilities.SAB);
        Assert.Equal(new AbilityScore(8), character.Abilities.CAR);
        Assert.Equal(AbilityScore.Empty, character.Abilities.ASP);
        Assert.Equal(31, character.HitPoints);
        Assert.Equal(12, character.Honor);
        Assert.Equal([new CharacterSkill("actuar", "Actuar", 25, 2)], character.Skills);
        Assert.Equal("Hacha", character.Equipment);
        Assert.Equal("Notas", character.Notes);
        Assert.Equal(new DateTimeOffset(2026, 11, 19, 12, 0, 0, TimeSpan.FromHours(1)), character.UpdatedAt);
    }

    [Fact]
    public void Saved_Characters_Of_Version_1_Are_Migrated_Too__FR009()
    {
        var file = JsonNode.Parse(Version1File)!;
        var saved = new JsonObject { ["schemaVersion"] = 1, ["characters"] = new JsonArray(file["character"]!.DeepClone()) };

        var read = StoredData.ReadCharacters(saved.ToJsonString());

        Assert.Null(read.Error);
        var character = Assert.Single(read.Value!.Characters);
        Assert.Equal(AbilityScore.Empty, character.Abilities.ASP);
        Assert.Equal(new AbilityScore(15, 47), character.Abilities.FUE);
    }

    [Fact]
    public void Missing_Abilities_Are_Read_As_Empty__FR009()
    {
        var file = JsonNode.Parse(StoredData.WriteCharacterFile(CharacterTests.Full()))!;
        file["character"]!["abilities"] = new JsonObject { ["FUE"] = new JsonObject { ["score"] = 3 } };

        var read = StoredData.ReadCharacterFile(file.ToJsonString());

        Assert.Null(read.Error);
        Assert.Equal(new AbilityScore(3), read.Value!.Character.Abilities.FUE);
        Assert.All(Enum.GetValues<Ability>().Where(ability => ability != Ability.FUE), ability => Assert.Equal(AbilityScore.Empty, read.Value.Character.Abilities[ability]));
    }

    [Fact]
    public void Abilities_Written_As_Null_Are_Read_As_Empty__FR009()
    {
        var file = JsonNode.Parse(StoredData.WriteCharacterFile(CharacterTests.Full()))!;
        file["character"]!["abilities"] = new JsonObject { ["FUE"] = null, ["ASP"] = null };
        var withoutAbilities = JsonNode.Parse(file.ToJsonString())!;
        withoutAbilities["character"]!["abilities"] = null;

        var read = StoredData.ReadCharacterFile(file.ToJsonString());
        var readWithout = StoredData.ReadCharacterFile(withoutAbilities.ToJsonString());

        Assert.Null(read.Error);
        Assert.Equal(CharacterAbilities.Empty, read.Value!.Character.Abilities);
        Assert.Null(readWithout.Error);
        Assert.Equal(CharacterAbilities.Empty, readWithout.Value!.Character.Abilities);
    }

    [Theory]
    [InlineData("""{ "schemaVersion": 4, "kind": "hackmaster.character", "character": { "id": "a", "name": "b" } }""", "versión más reciente")]
    [InlineData("""{ "kind": "hackmaster.character", "character": { "id": "a", "name": "b" } }""", "versión")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.suites", "character": { "id": "a", "name": "b" } }""", "no es una ficha")]
    [InlineData("""{ "schemaVersion": 2, "character": { "id": "a", "name": "b" } }""", "no es una ficha")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "id": null, "name": "b", "createdAt": "2026-11-19T10:00:00Z", "updatedAt": "2026-11-19T10:00:00Z" } }""", "incompleto")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "id": "a", "name": "  ", "createdAt": "2026-11-19T10:00:00Z", "updatedAt": "2026-11-19T10:00:00Z" } }""", "incompleto")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character" }""", "incompleto")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "id": "a", "name": "b", "skills": [ { "slug": "x" } ], "createdAt": "2026-11-19T10:00:00Z", "updatedAt": "2026-11-19T10:00:00Z" } }""", "incompleto")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "id": "a", "name": "b", "level": "tres" } }""", "dañado")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "name": "b" } }""", "dañado")]
    [InlineData("no es json", "dañado")]
    [InlineData("", "vacío")]
    [InlineData("[1, 2]", "versión")]
    public void A_Bad_File_Is_Rejected_With_A_Message__FR009(string json, string expected)
    {
        var read = StoredData.ReadCharacterFile(json);

        Assert.Null(read.Value);
        Assert.Contains(expected, read.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Saved_Characters_From_A_Newer_Version_Are_Not_Read__FR010()
    {
        var read = StoredData.ReadCharacters("""{ "schemaVersion": 9, "characters": [] }""");

        Assert.Null(read.Value);
        Assert.Contains("versión más reciente", read.Error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{ "kind": "hackmaster.character", "character": {} }""")]
    [InlineData("""{ "schemaVersion": 4, "kind": "hackmaster.character" }""")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character" }""")]
    [InlineData("""{ "schemaVersion": 2, "kind": "hackmaster.character", "character": { "id": "a", "name": "b", "level": "tres" } }""")]
    public void Import_Messages_Talk_About_The_File_Not_The_Saved_Data__Review(string json)
    {
        var read = StoredData.ReadCharacterFile(json);

        Assert.StartsWith("El archivo ", read.Error, StringComparison.Ordinal);
    }

    // F005: versión 3 con el Dado de Honor y las mejoras de cada habilidad.
    private static Character WithRules() => CharacterTests.Full() with
    {
        HonorDie = new HonorDie(12, 1),
        Skills =
        [
            new CharacterSkill("corte-a-la-yugular", "Corte a la yugular") { Improvements = [new SkillImprovement(3, 8), new SkillImprovement(1, 4)] },
            new CharacterSkill("actuar", "Actuar", 25, 2),
        ],
    };

    [Fact]
    public void The_Honor_Die_And_The_Improvements_Survive_Export_And_Import__F005_FR006()
    {
        var original = WithRules();

        var read = StoredData.ReadCharacterFile(StoredData.WriteCharacterFile(original));

        Assert.Null(read.Error);
        Assert.True(original.HasSameData(read.Value!.Character));
        Assert.Equal(new HonorDie(12, 1), read.Value.Character.HonorDie);
        Assert.Equal([new SkillImprovement(3, 8), new SkillImprovement(1, 4)], read.Value.Character.Skills[0].Improvements);
        Assert.Contains("\"honorDie\": \"d12+1\"", StoredData.WriteCharacterFile(original), StringComparison.Ordinal);
    }

    [Fact]
    public void Different_Improvements_Make_Different_Characters__F005_SC001()
    {
        var original = WithRules();
        var changed = original with { Skills = [original.Skills[0] with { Improvements = [new SkillImprovement(3, 8)] }, original.Skills[1]] };

        Assert.False(original.HasSameData(changed));
        Assert.True(original.HasSameData(original with { Skills = [original.Skills[0] with { Improvements = [.. original.Skills[0].Improvements] }, original.Skills[1]] }));
    }

    [Fact]
    public void A_Version_2_Character_Keeps_Its_Written_Mastery_And_Has_No_Improvements_Or_Honor_Die__F005_FR006()
    {
        var file = JsonNode.Parse(StoredData.WriteCharacterFile(CharacterTests.Full()))!;
        file["schemaVersion"] = 2;
        file["character"]!.AsObject().Remove("honorDie");
        foreach (var skill in file["character"]!["skills"]!.AsArray())
        {
            skill!.AsObject().Remove("improvements");
        }

        var read = StoredData.ReadCharacterFile(file.ToJsonString());

        Assert.Null(read.Error);
        var character = read.Value!.Character;
        Assert.Null(character.HonorDie);
        Assert.Equal(25, character.Skills[0].Mastery);
        Assert.All(character.Skills, skill => Assert.Empty(skill.Improvements));
        Assert.True(CharacterTests.Full().HasSameData(character));
    }

    [Fact]
    public void A_Version_1_File_Is_Migrated_Up_To_Version_3__F005_FR006()
    {
        var read = StoredData.ReadCharacterFile(Version1File);

        Assert.Null(read.Error);
        Assert.Equal(3, read.Value!.SchemaVersion);
        Assert.Empty(read.Value.Character.Skills[0].Improvements);
    }

    [Theory]
    [InlineData("\"d5\"")]
    [InlineData("\"d20+1\"")]
    [InlineData("12")]
    public void An_Honor_Die_That_Is_Not_In_Table_5C_Is_Read_As_Empty__MJ85(string honorDie)
    {
        var file = JsonNode.Parse(StoredData.WriteCharacterFile(WithRules()))!;
        file["character"]!["honorDie"] = JsonNode.Parse(honorDie);

        var read = StoredData.ReadCharacterFile(file.ToJsonString());

        Assert.Null(read.Error);
        Assert.Null(read.Value!.Character.HonorDie);
    }

    [Fact]
    public void Improvements_Written_As_Null_Are_Read_As_None__F005_FR006()
    {
        var file = JsonNode.Parse(StoredData.WriteCharacterFile(WithRules()))!;
        file["character"]!["skills"]![0]!["improvements"] = null;

        var read = StoredData.ReadCharacterFile(file.ToJsonString());

        Assert.Null(read.Error);
        Assert.Empty(read.Value!.Character.Skills[0].Improvements);
    }
}
