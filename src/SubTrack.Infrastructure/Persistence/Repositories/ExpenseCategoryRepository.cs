using Microsoft.Data.Sqlite;
using SubTrack.Core.Domain.Entities;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a categorías de gasto (F0-13, F0-T11).
/// </summary>
internal sealed class ExpenseCategoryRepository
{
    private const string SelectCategories = """
        SELECT id, nombre, activa, creado_en, actualizado_en
        FROM categorias_gasto
        """;

    public async Task<ExpenseCategory?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectCategories} WHERE id = $id;")
            .WithParameters(("$id", categoryId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    /// <summary>Búsqueda por nombre sin distinguir mayúsculas ni espacios exteriores.</summary>
    public async Task<ExpenseCategory?> GetByNameAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string name,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                $"""
                {SelectCategories}
                WHERE lower(nombre) = lower($nombre)
                  AND ($excluir IS NULL OR id <> $excluir)
                LIMIT 1;
                """)
            .WithParameters(
                ("$nombre", name.Trim()),
                ("$excluir", excludeId?.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<IReadOnlyList<ExpenseCategory>> GetAllAsync(
        SqliteConnection connection,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            $"""
            {SelectCategories}
            WHERE $incluirInactivas = 1 OR activa = 1
            ORDER BY nombre ASC;
            """);

        command.Parameters.AddWithValue("$incluirInactivas", includeInactive ? 1 : 0);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var categories = new List<ExpenseCategory>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            categories.Add(Map(reader));
        }

        return categories;
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ExpenseCategory category,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO categorias_gasto (id, nombre, activa, creado_en, actualizado_en)
                VALUES ($id, $nombre, $activa, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", category.Id.ToString()),
                ("$nombre", category.Name),
                ("$activa", category.IsActive ? 1 : 0),
                ("$creado", category.CreatedAt.ToStorage()),
                ("$actualizado", category.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RenameAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid categoryId,
        string name,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE categorias_gasto
                SET nombre = $nombre, actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", categoryId.ToString()),
                ("$nombre", name.Trim()),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SetActiveAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid categoryId,
        bool isActive,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE categorias_gasto
                SET activa = $activa, actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", categoryId.ToString()),
                ("$activa", isActive ? 1 : 0),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                "DELETE FROM categorias_gasto WHERE id = $id;")
            .WithParameters(("$id", categoryId.ToString()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> HasExpensesAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                "SELECT 1 FROM gastos WHERE categoria_id = $categoria LIMIT 1;")
            .WithParameters(("$categoria", categoryId.ToString()));

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    private static ExpenseCategory Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Name = reader.GetText("nombre"),
        IsActive = reader.GetBoolean("activa"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };
}
