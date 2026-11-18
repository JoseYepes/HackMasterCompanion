using Bunit;
using HackMaster.Web.Layout;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests;

/// <summary>Guardado en el navegador y qué pasa si no se puede guardar (FR-017, FR-019).</summary>
public class BrowserStoreTests : BunitContext
{
    private BrowserStore NewStore() => new(JSInterop.JSRuntime);

    [Fact]
    public async Task Reads_A_Stored_Value__FR017()
    {
        JSInterop.Setup<string?>("localStorage.getItem", "hackmaster.rolls").SetResult("{\"schemaVersion\":1}");

        var value = await NewStore().GetAsync("hackmaster.rolls");

        Assert.Equal("{\"schemaVersion\":1}", value);
    }

    [Fact]
    public async Task Writes_A_Value__FR017()
    {
        JSInterop.SetupVoid("localStorage.setItem", "hackmaster.suites", "{}").SetVoidResult();
        var store = NewStore();

        await store.SetAsync("hackmaster.suites", "{}");

        JSInterop.VerifyInvoke("localStorage.setItem");
        Assert.True(store.IsAvailable);
    }

    [Fact]
    public async Task Failed_Write_Does_Not_Throw_Keeps_The_Value_In_Memory_And_Warns__FR019()
    {
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("QuotaExceededError"));
        JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetException(new JSException("QuotaExceededError"));
        var store = NewStore();
        var changes = 0;
        store.AvailabilityChanged += () => changes++;

        await store.SetAsync("hackmaster.suites", "{\"a\":1}");

        Assert.False(store.IsAvailable);
        Assert.Equal(1, changes);
        Assert.Equal("{\"a\":1}", await store.GetAsync("hackmaster.suites"));
    }

    [Fact]
    public async Task Failed_Read_Does_Not_Throw_And_Gives_Nothing__FR019()
    {
        JSInterop.Setup<string?>("localStorage.getItem", _ => true).SetException(new JSException("SecurityError"));
        var store = NewStore();

        var value = await store.GetAsync("hackmaster.rolls");

        Assert.Null(value);
        Assert.False(store.IsAvailable);
    }

    [Fact]
    public async Task Layout_Shows_A_Warning_When_Storage_Is_Not_Available__FR019()
    {
        Services.AddSingleton(TestData.Create());
        JSInterop.SetupVoid("localStorage.setItem", _ => true).SetException(new JSException("SecurityError"));
        var store = NewStore();
        Services.AddSingleton(store);
        var cut = Render<MainLayout>();
        Assert.Empty(cut.FindAll("[data-testid=storage-warning]"));

        await store.SetAsync("hackmaster.rolls", "{}");

        cut.WaitForAssertion(() => Assert.Contains("no se pueden guardar", cut.Find("[data-testid=storage-warning]").TextContent, StringComparison.Ordinal));
    }
}
