namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-21, F0-T09 — Corrección del saldo inicial.
/// Conserva el importe y la fecha anteriores junto con los nuevos.
/// </summary>
public sealed record OpeningBalanceCorrection
{
    public required Guid Id { get; init; }
    public required long PreviousAmountCents { get; init; }
    public required long NewAmountCents { get; init; }
    public required DateOnly PreviousStartDate { get; init; }
    public required DateOnly NewStartDate { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
}
