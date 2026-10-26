using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-T07 — Alta de plantilla de ingreso recurrente.
/// </summary>
public sealed record CreateIncomeTemplateRequest(
    string Concept,
    long AmountCents,
    IncomeType Frequency,
    int AnchorDay,
    DateOnly StartDate,
    Guid OperationId);

/// <summary>
/// F0-T07 — Generación de los ingresos previstos de una plantilla hasta una fecha.
/// </summary>
public sealed record GeneratePlannedIncomesRequest(
    Guid TemplateId,
    DateOnly Until);

/// <summary>
/// F0-06, F0-12 — Alta de ingreso.
/// Sin <c>ActualDate</c> queda previsto; con ella queda confirmado.
/// </summary>
public sealed record CreateIncomeRequest(
    string Concept,
    long AmountCents,
    IncomeType Type,
    DateOnly? PlannedDate,
    DateOnly? ActualDate,
    Guid? TemplateId,
    Guid OperationId);

/// <summary>
/// F0-14, F0-T10 — Corrección de ingreso con historial.
/// </summary>
public sealed record UpdateIncomeRequest(
    Guid IncomeId,
    string Concept,
    long AmountCents,
    DateOnly PlannedDate);

/// <summary>
/// F0-06, F0-19 — Confirmación de un ingreso previsto.
/// </summary>
public sealed record ConfirmIncomeRequest(
    Guid IncomeId,
    DateOnly ActualDate);

/// <summary>
/// F0-T02 — Anulación de ingreso con motivo obligatorio.
/// </summary>
public sealed record CancelIncomeRequest(
    Guid IncomeId,
    string Reason);

/// <summary>
/// F0-T29 — Filtros y paginación del historial de ingresos.
/// </summary>
public sealed record IncomeHistoryRequest(
    PageRequest Page,
    DateOnly? From = null,
    DateOnly? To = null,
    IncomeStatus? Status = null);
