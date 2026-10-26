namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-14, F0-T10 — Corrección registrada de un gasto.
/// Conserva los valores anteriores y los nuevos; el Dashboard usa solo el vigente.
/// </summary>
public sealed record ExpenseCorrection
{
    public required Guid Id { get; init; }
    public required Guid ExpenseId { get; init; }
    public string? PreviousConcept { get; init; }
    public string? NewConcept { get; init; }
    public long? PreviousAmountCents { get; init; }
    public long? NewAmountCents { get; init; }
    public Guid? PreviousCategoryId { get; init; }
    public Guid? NewCategoryId { get; init; }
    public DateOnly? PreviousDate { get; init; }
    public DateOnly? NewDate { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
}
