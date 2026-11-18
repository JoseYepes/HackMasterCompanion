namespace HackMaster.Domain;

/// <summary>Fuente de azar. El dominio no crea números aleatorios por su cuenta (principio III).</summary>
public interface IRandom
{
    /// <summary>Entero uniforme entre <paramref name="minInclusive"/> y <paramref name="maxInclusive"/>, ambos incluidos.</summary>
    int Next(int minInclusive, int maxInclusive);
}
