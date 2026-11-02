using Microsoft.Data.Sqlite;
using SubTrack.Core.Domain.Entities;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a suscripciones y a su historial de versiones (F0-T06).
/// El día ancla se lee siempre de la última versión vigente.
/// </summary>
internal sealed class SubscriptionRepository
{
    private const string SelectSubscriptions = """
        SELECT s.id, s.nombre, s.operacion_id, s.monto_centavos, s.tipo_importe,
               s.frecuencia, s.fecha_inicio, s.proximo_cobro, s.activa,
               s.desactivada_en, s.creado_en, s.actualizado_en,
               COALESCE(v.dia_ancla, CAST(substr(s.fecha_inicio, 9, 2) AS INTEGER)) AS dia_ancla
        FROM suscripciones s
        LEFT JOIN suscripcion_versiones v
               ON v.suscripcion_id = s.id
              AND v.vigente_desde = (
                  SELECT MAX(v2.vigente_desde)
                  FROM suscripcion_versiones v2
                  WHERE v2.suscripcion_id = s.id)
        """;

    private const string SelectVersions = """
        SELECT id, suscripcion_id, numero_version, vigente_desde, monto_centavos,
               tipo_importe, frecuencia, dia_ancla, creado_en
        FROM suscripcion_versiones
        WHERE suscripcion_id = $suscripcion
        ORDER BY vigente_desde ASC, numero_version ASC;
        """;

    public async Task<Subscription?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectSubscriptions} WHERE s.id = $id;")
            .WithParameters(("$id", subscriptionId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapSubscription(reader)
            : null;
    }

    public async Task<Subscription?> GetByOperationIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectSubscriptions} WHERE s.operacion_id = $operacion;")
            .WithParameters(("$operacion", operationId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapSubscription(reader)
            : null;
    }

    public async Task<IReadOnlyList<Subscription>> GetAllAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            $"{SelectSubscriptions} ORDER BY s.activa DESC, s.proximo_cobro ASC, s.nombre ASC;");

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var subscriptions = new List<Subscription>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            subscriptions.Add(MapSubscription(reader));
        }

        return subscriptions;
    }

    public async Task<IReadOnlyList<SubscriptionVersion>> GetVersionsAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid subscriptionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, SelectVersions)
            .WithParameters(("$suscripcion", subscriptionId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var versions = new List<SubscriptionVersion>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            versions.Add(MapVersion(reader));
        }

        return versions;
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO suscripciones
                    (id, nombre, operacion_id, monto_centavos, tipo_importe, frecuencia,
                     fecha_inicio, proximo_cobro, activa, desactivada_en, creado_en, actualizado_en)
                VALUES
                    ($id, $nombre, $operacion, $monto, $tipo, $frecuencia,
                     $inicio, $proximo, $activa, $desactivada, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", subscription.Id.ToString()),
                ("$nombre", subscription.Name),
                ("$operacion", subscription.OperationId.ToString()),
                ("$monto", subscription.AmountCents),
                ("$tipo", SqliteEnumCodes.ToCode(subscription.AmountType)),
                ("$frecuencia", SqliteEnumCodes.ToCode(subscription.Frequency)),
                ("$inicio", subscription.StartDate.ToStorage()),
                ("$proximo", subscription.NextChargeDate?.ToStorage()),
                ("$activa", subscription.IsActive ? 1 : 0),
                ("$desactivada", subscription.DeactivatedOn?.ToStorage()),
                ("$creado", subscription.CreatedAt.ToStorage()),
                ("$actualizado", subscription.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task InsertVersionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        SubscriptionVersion version,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO suscripcion_versiones
                    (id, suscripcion_id, numero_version, vigente_desde, monto_centavos,
                     tipo_importe, frecuencia, dia_ancla, creado_en)
                VALUES
                    ($id, $suscripcion, $numero, $desde, $monto,
                     $tipo, $frecuencia, $ancla, $creado);
                """)
            .WithParameters(
                ("$id", version.Id.ToString()),
                ("$suscripcion", version.SubscriptionId.ToString()),
                ("$numero", version.VersionNumber),
                ("$desde", version.EffectiveFrom.ToStorage()),
                ("$monto", version.AmountCents),
                ("$tipo", SqliteEnumCodes.ToCode(version.AmountType)),
                ("$frecuencia", SqliteEnumCodes.ToCode(version.Frequency)),
                ("$ancla", version.AnchorDay),
                ("$creado", version.CreatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Subscription subscription,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE suscripciones
                SET nombre = $nombre,
                    monto_centavos = $monto,
                    tipo_importe = $tipo,
                    frecuencia = $frecuencia,
                    proximo_cobro = $proximo,
                    actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", subscription.Id.ToString()),
                ("$nombre", subscription.Name),
                ("$monto", subscription.AmountCents),
                ("$tipo", SqliteEnumCodes.ToCode(subscription.AmountType)),
                ("$frecuencia", SqliteEnumCodes.ToCode(subscription.Frequency)),
                ("$proximo", subscription.NextChargeDate?.ToStorage()),
                ("$actualizado", subscription.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeactivateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid subscriptionId,
        DateOnly deactivatedOn,
        DateOnly? nextChargeDate,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE suscripciones
                SET activa = 0,
                    desactivada_en = $desde,
                    proximo_cobro = $proximo,
                    actualizado_en = $actualizado
                WHERE id = $id AND activa = 1;
                """)
            .WithParameters(
                ("$id", subscriptionId.ToString()),
                ("$desde", deactivatedOn.ToStorage()),
                ("$proximo", nextChargeDate?.ToStorage()),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateNextChargeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid subscriptionId,
        DateOnly? nextChargeDate,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE suscripciones
                SET proximo_cobro = $proximo,
                    actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", subscriptionId.ToString()),
                ("$proximo", nextChargeDate?.ToStorage()),
                ("$actualizado", updatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Subscription MapSubscription(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Name = reader.GetText("nombre"),
        OperationId = reader.GetGuid("operacion_id"),
        AmountCents = reader.GetCents("monto_centavos"),
        AmountType = SqliteEnumCodes.ParseAmountType(reader.GetText("tipo_importe")),
        Frequency = SqliteEnumCodes.ParseFrequency(reader.GetText("frecuencia")),
        StartDate = reader.GetDateOnly("fecha_inicio"),
        AnchorDay = reader.GetInt32("dia_ancla"),
        NextChargeDate = reader.GetNullableDateOnly("proximo_cobro"),
        IsActive = reader.GetBoolean("activa"),
        DeactivatedOn = reader.GetNullableDateOnly("desactivada_en"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };

    private static SubscriptionVersion MapVersion(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        SubscriptionId = reader.GetGuid("suscripcion_id"),
        VersionNumber = reader.GetInt32("numero_version"),
        EffectiveFrom = reader.GetDateOnly("vigente_desde"),
        AmountCents = reader.GetCents("monto_centavos"),
        AmountType = SqliteEnumCodes.ParseAmountType(reader.GetText("tipo_importe")),
        Frequency = SqliteEnumCodes.ParseFrequency(reader.GetText("frecuencia")),
        AnchorDay = reader.GetInt32("dia_ancla"),
        CreatedAt = reader.GetTimestamp("creado_en")
    };
}
