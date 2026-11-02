using SubTrack.Core.Results;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-T33 — Validaciones de negocio compartidas por los servicios.
/// WinForms valida para dar mensajes tempranos; estas reglas son la segunda capa.
/// </summary>
internal static class DomainValidation
{
    public const int MaxNameLength = 200;
    public const int MaxCategoryNameLength = 100;
    public const int MaxReasonLength = 500;

    public static DomainError? Name(string? value, int maxLength = MaxNameLength) =>
        string.IsNullOrWhiteSpace(value)
            ? new DomainError(ErrorCodes.NombreRequerido, "Debe escribir un nombre.", "Name")
            : value.Trim().Length > maxLength
                ? new DomainError(
                    ErrorCodes.NombreDemasiadoLargo,
                    $"El nombre no puede superar los {maxLength} caracteres.",
                    "Name")
                : null;

    public static DomainError? Amount(long cents) =>
        cents <= 0
            ? new DomainError(
                ErrorCodes.ImporteInvalido,
                "El importe debe ser mayor que cero.",
                "AmountCents")
            : null;

    public static DomainError? DateBeforeToday(DateOnly date, DateOnly today, string field) =>
        date >= today
            ? new DomainError(
                ErrorCodes.FechaAnteriorRequerida,
                "La fecha del cobro anterior debe ser anterior a hoy.",
                field)
            : null;

    public static DomainError? Reason(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? new DomainError(ErrorCodes.MotivoRequerido, "Debe indicar el motivo.", "Reason")
            : value.Trim().Length > MaxReasonLength
                ? new DomainError(
                    ErrorCodes.MotivoRequerido,
                    $"El motivo no puede superar los {MaxReasonLength} caracteres.",
                    "Reason")
                : null;

    public static DomainError? AnchorDay(int day) =>
        day is < 1 or > 31
            ? new DomainError(
                ErrorCodes.DiaAnclaInvalido,
                "El día de cobro debe estar entre 1 y 31.",
                "AnchorDay")
            : null;

    public static DomainError? BiweeklyAnchorDay(int day) =>
        day is not (15 or 31)
            ? new DomainError(
                ErrorCodes.DiasDeIngresoInvalidos,
                "El ingreso quincenal solo puede pagarse el día 15 o el último día del mes.",
                "AnchorDay")
            : null;
}
