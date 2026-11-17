namespace HackMaster.Domain;

/// <summary>Referencia a un libro: acrónimo (MJ, CG, GrG…) y página o número tal como figura en la fuente.</summary>
public sealed record BookRef(string Acronym, string? Page)
{
    /// <summary>Texto para mostrar: acrónimo y página.</summary>
    public override string ToString() => Page is null ? Acronym : $"{Acronym} {Page}";
}
