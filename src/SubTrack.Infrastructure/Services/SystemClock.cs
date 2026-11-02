using SubTrack.Core.Abstractions;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-T32 — Reloj del sistema. Usa la fecha local de Windows para las reglas
/// de negocio y UTC para las marcas técnicas.
/// </summary>
internal sealed class SystemClock : IDateProvider
{
    private readonly TimeProvider _timeProvider;

    public SystemClock(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();
}
