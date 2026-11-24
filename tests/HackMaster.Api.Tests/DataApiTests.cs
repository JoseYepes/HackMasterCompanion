using System.Text.Json;
using System.Text.Json.Nodes;

namespace HackMaster.Api.Tests;

[Collection(EmulatorCollection.Name)]
public class DataApiTests(EmulatorFixture fixture)
{
    private static string Doc(string id, string kind = "character", string name = "Brunilda", string updatedAt = "2026-11-24T10:00:00+00:00", int schemaVersion = 3, bool deleted = false, string? payload = null) =>
        new JsonObject
        {
            ["id"] = id,
            ["kind"] = kind,
            ["schemaVersion"] = schemaVersion,
            ["updatedAt"] = updatedAt,
            ["deleted"] = deleted,
            ["payload"] = deleted ? null : payload ?? $$"""{"id":"{{id}}","name":"{{name}}"}""",
        }.ToJsonString();

    private static JsonElement Body(ApiResult result) => JsonDocument.Parse(result.Json!).RootElement;

    [EmulatorFact]
    public async Task Me_Returns_Name_Email_And_Admin_Flag__F006_FR001()
    {
        var auth = await fixture.InvitedAccountAsync("Ana@Pruebas.Example");

        var result = await fixture.Api().MeAsync(auth);

        Assert.Equal(200, result.Status);
        Assert.Equal("ana@pruebas.example", Body(result).GetProperty("email").GetString());
        Assert.False(Body(result).GetProperty("isAdmin").GetBoolean());
    }

    [EmulatorFact]
    public async Task Admin_Is_Allowed_Without_Invitation__F006_FR003()
    {
        var result = await fixture.Api().MeAsync(fixture.Tokens.Bearer(Guid.NewGuid().ToString(), EmulatorFixture.AdminEmail));

        Assert.Equal(200, result.Status);
        Assert.True(Body(result).GetProperty("isAdmin").GetBoolean());
    }

    [EmulatorFact]
    public async Task Requests_Without_Valid_Token_Get_401__F006_FR008()
    {
        var api = fixture.Api();

        Assert.Equal(401, (await api.MeAsync(null)).Status);
        Assert.Equal(401, (await api.GetDataAsync("Bearer basura", null)).Status);
        Assert.Equal(401, (await api.PutAsync(null, "character:c1", null, "*", Doc("character:c1"))).Status);
    }

    [EmulatorFact]
    public async Task Create_Then_Read_Back__F006_FR004()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();

        var put = await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c1"));
        var get = await api.GetDataAsync(auth, null);

        Assert.Equal(200, put.Status);
        Assert.False(string.IsNullOrEmpty(Body(put).GetProperty("etag").GetString()));
        var item = Assert.Single(Body(get).GetProperty("items").EnumerateArray());
        Assert.Equal("character:c1", item.GetProperty("id").GetString());
        Assert.Contains("Brunilda", item.GetProperty("payload").GetString());
        Assert.True(Body(get).GetProperty("serverTime").GetInt64() > 0);
    }

    [EmulatorFact]
    public async Task Update_With_Current_Version_Succeeds__F006_FR004()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        var etag = Body(await api.PutAsync(auth, "suite:s1", null, "*", Doc("suite:s1", "suite", schemaVersion: 1))).GetProperty("etag").GetString();

        var update = await api.PutAsync(auth, "suite:s1", etag, null, Doc("suite:s1", "suite", "Otro", schemaVersion: 1));

        Assert.Equal(200, update.Status);
        Assert.NotEqual(etag, Body(update).GetProperty("etag").GetString());
    }

    [EmulatorFact]
    public async Task Stale_Version_Gets_409_With_Cloud_Copy__F006_FR007()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        var first = Body(await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c1"))).GetProperty("etag").GetString();
        await api.PutAsync(auth, "character:c1", first, null, Doc("character:c1", name: "Nube"));

        var stale = await api.PutAsync(auth, "character:c1", first, null, Doc("character:c1", name: "Viejo"));
        var duplicate = await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c1", name: "Nuevo"));

        Assert.Equal(409, stale.Status);
        Assert.Contains("Nube", Body(stale).GetProperty("payload").GetString());
        Assert.Equal(409, duplicate.Status);
    }

    [EmulatorFact]
    public async Task Since_Returns_Only_Later_Changes__F006_FR006()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        await api.PutAsync(auth, "character:a", null, "*", Doc("character:a"));
        var serverTime = Body(await api.GetDataAsync(auth, null)).GetProperty("serverTime").GetInt64();
        await Task.Delay(1100);
        await api.PutAsync(auth, "character:b", null, "*", Doc("character:b"));

        var items = Body(await api.GetDataAsync(auth, serverTime + 1)).GetProperty("items").EnumerateArray().ToList();

        Assert.Equal("character:b", Assert.Single(items).GetProperty("id").GetString());
    }

    [EmulatorFact]
    public async Task Deletion_Marks_Are_Kept__F006_FR007()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        var etag = Body(await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c1"))).GetProperty("etag").GetString();

        await api.PutAsync(auth, "character:c1", etag, null, Doc("character:c1", deleted: true));

        var item = Assert.Single(Body(await api.GetDataAsync(auth, null)).GetProperty("items").EnumerateArray());
        Assert.True(item.GetProperty("deleted").GetBoolean());
    }

    [EmulatorFact]
    public async Task Oversized_Or_Invalid_Documents_Are_Rejected__F006_DataModel()
    {
        var auth = await fixture.InvitedAccountAsync();
        var api = fixture.Api();
        var big = JsonSerializer.Serialize(new string('x', 2 * 1024 * 1024));

        Assert.Equal(413, (await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c1", payload: big))).Status);
        Assert.Equal(400, (await api.PutAsync(auth, "character:c1", null, "*", "no es json")).Status);
        Assert.Equal(400, (await api.PutAsync(auth, "character:c1", null, "*", Doc("character:c2"))).Status);
        Assert.Equal(400, (await api.PutAsync(auth, "otro:c1", null, "*", Doc("otro:c1", "otro"))).Status);
    }
}
