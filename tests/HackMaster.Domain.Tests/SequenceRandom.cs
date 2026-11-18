using HackMaster.Domain;

namespace HackMaster.Domain.Tests;

/// <summary>
/// Azar de prueba: devuelve los valores indicados, en orden, y comprueba que cada uno está dentro
/// del intervalo pedido. Si se acaban, la prueba falla.
/// </summary>
public sealed class SequenceRandom(params int[] values) : IRandom
{
    private readonly Queue<int> _values = new(values);

    /// <summary>Intervalos pedidos, en orden.</summary>
    public List<(int Min, int Max)> Requests { get; } = [];

    public int Next(int minInclusive, int maxInclusive)
    {
        Requests.Add((minInclusive, maxInclusive));
        Assert.True(_values.Count > 0, "Se pidieron más números de los preparados.");
        var value = _values.Dequeue();
        Assert.InRange(value, minInclusive, maxInclusive);
        return value;
    }
}

/// <summary>Azar con semilla fija, para pruebas estadísticas repetibles.</summary>
public sealed class SeededRandom(int seed) : IRandom
{
    private readonly Random _random = new(seed);

    public int Next(int minInclusive, int maxInclusive) => _random.Next(minInclusive, maxInclusive + 1);
}
