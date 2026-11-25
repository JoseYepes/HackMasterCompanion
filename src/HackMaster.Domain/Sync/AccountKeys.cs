namespace HackMaster.Domain.Sync;

/// <summary>
/// Claves del dispositivo de cada cuenta (FR-009, research.md R5). Sin sesión se usan las de
/// siempre; con sesión llevan el identificador de la cuenta, para que nadie vea lo de otro.
/// </summary>
public static class AccountKeys
{
    private const string Prefix = "hackmaster.";

    /// <summary>
    /// Un identificador de cuenta (<c>oid</c>) válido: letras, cifras y guiones, hasta 64. Así no
    /// puede formar la clave de otra cuenta ni salirse del prefijo.
    /// </summary>
    public static bool IsValidAccountId(string? accountId) =>
        !string.IsNullOrEmpty(accountId)
        && accountId.Length <= 64
        && accountId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

    /// <summary>La clave de <paramref name="baseKey"/> (p. ej. <c>hackmaster.characters</c>) para la cuenta, o la misma sin sesión.</summary>
    public static string For(string baseKey, string? accountId)
    {
        if (!baseKey.StartsWith(Prefix, StringComparison.Ordinal) || baseKey.Length == Prefix.Length)
        {
            throw new ArgumentException($"No es una clave de la aplicación: {baseKey}", nameof(baseKey));
        }

        if (accountId is null)
        {
            return baseKey;
        }

        if (!IsValidAccountId(accountId))
        {
            throw new ArgumentException("Identificador de cuenta no válido.", nameof(accountId));
        }

        return $"{Prefix}u.{accountId}.{baseKey[Prefix.Length..]}";
    }

    /// <summary>Clave del estado de sincronización de la cuenta.</summary>
    public static string SyncState(string accountId) => For(Prefix + "sync", accountId);
}
