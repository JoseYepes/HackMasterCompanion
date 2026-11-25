using System.Text.Json;
using System.Text.Json.Nodes;

namespace HackMaster.Api.Tests;

[Collection(EmulatorCollection.Name)]
public class IsolationAndInvitationTests(EmulatorFixture fixture)
{
    private static string Doc(string id, string name, string? userId = null)
    {
        var doc = new JsonObject
        {
            ["id"] = id,
            ["kind"] = "character",
            ["schemaVersion"] = 3,
            ["updatedAt"] = "2026-11-24T10:00:00+00:00",
            ["deleted"] = false,
            ["payload"] = $$"""{"name":"{{name}}"}""",
        };
        if (userId is not null)
        {
            doc["userId"] = userId;
        }

        return doc.ToJsonString();
    }

    private static List<JsonElement> Items(ApiResult result) =>
        JsonDocument.Parse(result.Json!).RootElement.GetProperty("items").EnumerateArray().ToList();

    [EmulatorFact]
    public async Task One_Account_Never_Sees_Another__F006_SC003()
    {
        var a = await fixture.InvitedAccountAsync();
        var b = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        await api.PutAsync(a, "character:c1", null, "*", Doc("character:c1", "De A"));

        Assert.Empty(Items(await api.GetDataAsync(b, null)));
        Assert.Single(Items(await api.GetDataAsync(a, null)));
    }

    [EmulatorFact]
    public async Task Same_Id_In_Another_Account_Does_Not_Touch_The_First__F006_FR008()
    {
        var a = await fixture.InvitedAccountAsync();
        var b = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        await api.PutAsync(a, "character:c1", null, "*", Doc("character:c1", "De A"));

        var put = await api.PutAsync(b, "character:c1", null, "*", Doc("character:c1", "De B"));

        Assert.Equal(200, put.Status);
        Assert.Contains("De A", Assert.Single(Items(await api.GetDataAsync(a, null))).GetProperty("payload").GetString());
    }

    [EmulatorFact]
    public async Task User_Id_In_The_Body_Is_Ignored__F006_FR008()
    {
        var aId = Guid.NewGuid().ToString();
        var aEmail = $"{aId}@pruebas.example";
        await fixture.Store.InviteAsync(aEmail, EmulatorFixture.AdminEmail);
        var a = fixture.Tokens.Bearer(aId, aEmail);
        var b = await fixture.InvitedAccountAsync();
        var api = fixture.Api();

        await api.PutAsync(b, "character:c9", null, "*", Doc("character:c9", "Intruso", userId: aId));

        Assert.Empty(Items(await api.GetDataAsync(a, null)));
        Assert.Single(Items(await api.GetDataAsync(b, null)));
    }

    [EmulatorFact]
    public async Task Not_Invited_Gets_403_Everywhere__F006_FR003()
    {
        var stranger = fixture.Tokens.Bearer(Guid.NewGuid().ToString(), "desconocido@pruebas.example");
        var api = fixture.Api();

        foreach (var result in new[]
        {
            await api.MeAsync(stranger),
            await api.GetDataAsync(stranger, null),
            await api.PutAsync(stranger, "character:c1", null, "*", Doc("character:c1", "x")),
            await api.ExportAsync(stranger),
            await api.DeleteAccountAsync(stranger),
        })
        {
            Assert.Equal(403, result.Status);
            Assert.Contains("not-invited", result.Json);
        }
    }

    [EmulatorFact]
    public async Task Invitation_Email_Is_Case_Insensitive__F006_FR003()
    {
        var oid = Guid.NewGuid().ToString();
        await fixture.Store.InviteAsync($"Mayus-{oid}@Pruebas.Example", EmulatorFixture.AdminEmail);

        var result = await fixture.Api().MeAsync(fixture.Tokens.Bearer(oid, $"mayus-{oid}@pruebas.example"));

        Assert.Equal(200, result.Status);
    }

    [EmulatorFact]
    public async Task Only_Admins_Manage_Invitations__F006_FR003()
    {
        var player = await fixture.InvitedAccountAsync();
        var admin = fixture.AdminBearer();
        var api = fixture.Api();
        var email = $"{Guid.NewGuid()}@pruebas.example";

        Assert.Equal(403, (await api.ListInvitationsAsync(player)).Status);
        Assert.Equal(403, (await api.InviteAsync(player, email)).Status);
        Assert.Equal(403, (await api.UninviteAsync(player, email)).Status);

        Assert.Equal(204, (await api.InviteAsync(admin, email)).Status);
        Assert.Contains(email, (await api.ListInvitationsAsync(admin)).Json);
        Assert.Equal(204, (await api.UninviteAsync(admin, email)).Status);
        Assert.DoesNotContain(email, (await api.ListInvitationsAsync(admin)).Json);
    }

    [EmulatorFact]
    public async Task Invalid_Invitation_Email_Is_Rejected__F006_FR003()
    {
        var admin = fixture.AdminBearer();

        Assert.Equal(400, (await fixture.Api().InviteAsync(admin, "no-es-un-correo")).Status);
    }

    [EmulatorFact]
    public async Task Removed_Invitation_Blocks_The_Next_Request__F006_FR003()
    {
        var email = $"{Guid.NewGuid()}@pruebas.example";
        var player = await fixture.InvitedAccountAsync(email);
        var admin = fixture.AdminBearer();
        var api = fixture.Api();
        Assert.Equal(200, (await api.GetDataAsync(player, null)).Status);

        await api.UninviteAsync(admin, email);

        Assert.Equal(403, (await api.GetDataAsync(player, null)).Status);
    }

    [EmulatorFact]
    public async Task Export_Has_Everything_And_Delete_Removes_Only_This_Account__F006_FR011()
    {
        var a = await fixture.InvitedAccountAsync();
        var b = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        await api.PutAsync(a, "character:c1", null, "*", Doc("character:c1", "De A"));
        await api.PutAsync(a, "character:c2", null, "*", Doc("character:c2", "Otra de A"));
        await api.PutAsync(b, "character:c1", null, "*", Doc("character:c1", "De B"));

        var export = await api.ExportAsync(a);
        var delete = await api.DeleteAccountAsync(a);

        Assert.Equal(200, export.Status);
        Assert.Equal("hackmaster-cuenta.json", export.FileName);
        var root = JsonDocument.Parse(export.Json!).RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(2, root.GetProperty("items").GetArrayLength());
        Assert.Equal(204, delete.Status);
        Assert.Empty(Items(await api.GetDataAsync(a, null)));
        Assert.Single(Items(await api.GetDataAsync(b, null)));
    }
}
