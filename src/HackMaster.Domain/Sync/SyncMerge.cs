namespace HackMaster.Domain.Sync;

/// <summary>Resultado de un conflicto: el documento que se queda y el aviso para el jugador.</summary>
public sealed record ConflictResult(SyncDoc Winner, bool LocalWins, string Warning);

/// <summary>Resultado de aplicar lo que trae la nube: lo que hay que guardar en el dispositivo, el nuevo estado y los avisos.</summary>
public sealed record IncomingResult(IReadOnlyList<SyncDoc> Apply, SyncState State, IReadOnlyList<string> Warnings);

/// <summary>
/// Reglas de fusión entre el dispositivo y la nube (data-model.md de F006, FR-006 y FR-007): por
/// documento, se queda el cambio más reciente y, si empatan, el de la nube.
/// </summary>
public static class SyncMerge
{
    /// <summary>Resuelve un conflicto entre un cambio del dispositivo y la versión de la nube.</summary>
    public static ConflictResult Resolve(SyncDoc local, SyncDoc remote)
    {
        var localWins = local.UpdatedAt > remote.UpdatedAt;
        var winner = localWins ? local with { ETag = remote.ETag } : remote;
        var loser = localWins ? remote : local;
        var label = winner.Deleted ? loser.Label : winner.Label;
        return new(winner, localWins, $"Se ha quedado la versión más reciente de «{label}».");
    }

    /// <summary>Aplica los documentos que trae la nube al estado del dispositivo.</summary>
    public static IncomingResult ApplyIncoming(IEnumerable<SyncDoc> incoming, SyncState state)
    {
        var apply = new List<SyncDoc>();
        var warnings = new List<string>();

        foreach (var remote in incoming)
        {
            var current = SyncKinds.CurrentVersion(remote.Kind);
            if (current is null || remote.SchemaVersion > current)
            {
                warnings.Add($"No se ha podido leer «{remote.Label}»: es de una versión más nueva de la aplicación.");
                continue;
            }

            var pending = state.Pending.FirstOrDefault(p => p.Id == remote.Id);
            if (pending is null)
            {
                apply.Add(remote);
                state = remote.ETag is null ? state : state.WithKnown(remote.Id, remote.ETag);
                continue;
            }

            if (pending.ETag is not null && pending.ETag == remote.ETag)
            {
                // El cambio pendiente se basa en esta misma versión: no hay conflicto.
                continue;
            }

            var conflict = Resolve(pending, remote);
            warnings.Add(conflict.Warning);
            state = remote.ETag is null ? state : state.WithKnown(remote.Id, remote.ETag);
            if (conflict.LocalWins)
            {
                state = state with { Pending = [.. state.Pending.Select(p => p.Id == remote.Id ? conflict.Winner : p)] };
            }
            else
            {
                state = state.WithoutPending(remote.Id);
                apply.Add(remote);
            }
        }

        return new(apply, state, warnings);
    }
}
