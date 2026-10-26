using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// Utilidades compartidas por los servicios: rollback seguro y validación de páginas.
/// </summary>
internal static class ServiceSupport
{
    public static async Task SafeRollbackAsync(
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // La transacción ya estaba cerrada; no hay nada que revertir.
        }
        catch (SqliteException)
        {
            // La conexión ya no está disponible; el archivo queda íntegro por WAL.
        }
    }

    public static bool IsValidPage(PageRequest page) =>
        page.Page >= 1 &&
        page.PageSize >= 1 &&
        page.PageSize <= PageRequest.MaxPageSize;
}
