namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-20, F0-T09 — Configuración del saldo inicial único.
/// </summary>
public sealed record SetOpeningBalanceRequest(long AmountCents, DateOnly StartDate);

/// <summary>
/// F0-21, F0-T09 — Corrección del saldo inicial conservando el historial.
/// </summary>
public sealed record CorrectOpeningBalanceRequest(long AmountCents, DateOnly StartDate);
