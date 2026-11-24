using HackMaster.Domain.Sync;

namespace HackMaster.Web.Account;

/// <summary>
/// Cuenta con la sesión iniciada en este dispositivo, o ninguna (F006). Los almacenes la usan para
/// guardar cada cuenta en sus propias claves (FR-009); sin sesión, las de siempre (FR-012).
/// </summary>
public sealed class AccountSession
{
    /// <summary>Identificador (<c>oid</c>) de la cuenta, o null sin sesión.</summary>
    public string? AccountId { get; private set; }

    /// <summary>Se lanza al iniciar o cerrar sesión, o al cambiar de cuenta.</summary>
    public event Action? Changed;

    public void SetAccount(string? accountId)
    {
        if (accountId is not null && !AccountKeys.IsValidAccountId(accountId))
        {
            throw new ArgumentException("Identificador de cuenta no válido.", nameof(accountId));
        }

        if (accountId == AccountId)
        {
            return;
        }

        AccountId = accountId;
        Changed?.Invoke();
    }

    /// <summary>
    /// La clave del dispositivo para la cuenta actual. El modo de pantalla es del dispositivo: lo
    /// lee el script de index.html antes de saber la cuenta (research.md R5).
    /// </summary>
    public string Key(string baseKey) => baseKey == LocalDocs.ThemeKey ? baseKey : AccountKeys.For(baseKey, AccountId);

    /// <summary>Si un cambio de esta clave hay que llevarlo a la cuenta.</summary>
    public bool IsSynced(string baseKey) => AccountId is not null && LocalDocs.SyncedKeys.Contains(baseKey);
}
