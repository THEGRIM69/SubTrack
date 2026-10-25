using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-07, F0-12, F0-T08 — Alta de gasto.
/// Sin <c>ActualDate</c> el gasto queda planificado; con ella queda confirmado.
/// </summary>
public sealed record CreateExpenseRequest(
    string Concept,
    Guid CategoryId,
    long AmountCents,
    DateOnly? PlannedDate,
    DateOnly? ActualDate,
    Guid OperationId);

/// <summary>
/// F0-14, F0-T10 — Corrección de gasto con historial.
/// </summary>
public sealed record UpdateExpenseRequest(
    Guid ExpenseId,
    string Concept,
    Guid CategoryId,
    long AmountCents,
    DateOnly PlannedDate);

/// <summary>
/// F0-19, F0-T08 — Confirmación de un gasto planificado sin duplicar el importe.
/// </summary>
public sealed record ConfirmExpenseRequest(
    Guid ExpenseId,
    DateOnly ActualDate);

/// <summary>
/// F0-T02 — Anulación de gasto con motivo obligatorio.
/// </summary>
public sealed record CancelExpenseRequest(
    Guid ExpenseId,
    string Reason);

/// <summary>
/// F0-T29 — Filtros y paginación del historial de gastos.
/// </summary>
public sealed record ExpenseHistoryRequest(
    PageRequest Page,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? CategoryId = null,
    ExpenseStatus? Status = null);
