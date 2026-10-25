namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T32 — Fecha local usada para vencimientos y alertas.
/// Se abstrae para poder probar las reglas dependientes de «hoy».
/// </summary>
public interface IDateProvider
{
    DateOnly Today { get; }
}
