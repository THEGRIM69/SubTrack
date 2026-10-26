namespace SubTrack.Core.Results;

/// <summary>
/// F0-15 — Error de negocio identificable.
/// <paramref name="Code"/> es estable; <paramref name="Message"/> está listo para mostrarse.
/// </summary>
/// <param name="Code">Código estable definido en <see cref="ErrorCodes"/>.</param>
/// <param name="Message">Mensaje comprensible para el usuario.</param>
/// <param name="Field">Campo del formulario al que se refiere, si aplica.</param>
public sealed record DomainError(string Code, string Message, string? Field = null);
