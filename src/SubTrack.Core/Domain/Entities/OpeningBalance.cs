namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-20, F0-T09 — Saldo inicial único vigente.
/// No se registra como ingreso para no duplicarlo en los cálculos.
/// </summary>
public sealed record OpeningBalance
{
    public required long AmountCents { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
