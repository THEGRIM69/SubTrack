namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-13, F0-T11 — Categoría de gasto predefinida o personalizada.
/// Una categoría con gastos asociados puede desactivarse pero no eliminarse.
/// </summary>
public sealed record ExpenseCategory
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
