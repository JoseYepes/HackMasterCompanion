using HackMaster.Domain;

namespace HackMaster.Web;

/// <summary>Azar del navegador con <see cref="Random.Shared"/>: uniforme y suficiente para el juego.</summary>
public sealed class SystemRandom : IRandom
{
    /// <inheritdoc />
    public int Next(int minInclusive, int maxInclusive) => Random.Shared.Next(minInclusive, maxInclusive + 1);
}
