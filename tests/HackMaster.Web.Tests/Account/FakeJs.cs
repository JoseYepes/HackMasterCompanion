using System.Diagnostics.CodeAnalysis;
using Microsoft.JSInterop;

namespace HackMaster.Web.Tests.Account;

/// <summary>
/// Navegador falso con memoria: <c>localStorage</c>, el módulo de IndexedDB y el script del modo
/// de pantalla. Recuerda lo guardado, a diferencia de las respuestas fijas de bUnit.
/// </summary>
public sealed class FakeJs : IJSRuntime, IJSObjectReference
{
    public Dictionary<string, string> Local { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> Indexed { get; } = new(StringComparer.Ordinal);

    public string Theme { get; set; } = "light";

    public List<string> Downloads { get; } = [];

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        args ??= [];
        object? result = identifier switch
        {
            "localStorage.getItem" => Local.GetValueOrDefault((string)args[0]!),
            "localStorage.setItem" => Set(Local, args),
            "import" => this,
            "get" => Indexed.GetValueOrDefault((string)args[0]!),
            "set" => Set(Indexed, args),
            "download" => Download(args),
            "hackmasterTheme.current" => Theme,
            "hackmasterTheme.set" => SetTheme(args),
            _ => throw new JSException($"Función no simulada: {identifier}"),
        };
        return ValueTask.FromResult((TValue)result!);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static object? Set(Dictionary<string, string> store, object?[] args)
    {
        store[(string)args[0]!] = (string)args[1]!;
        return null;
    }

    private object? Download(object?[] args)
    {
        Downloads.Add((string)args[0]!);
        return null;
    }

    private object? SetTheme(object?[] args)
    {
        Theme = (string)args[0]!;
        Local["hackmaster.theme"] = $$"""{"schemaVersion":1,"mode":"{{Theme}}"}""";
        return null;
    }
}
