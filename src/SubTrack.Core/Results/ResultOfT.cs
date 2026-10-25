namespace SubTrack.Core.Results;

/// <summary>
/// F0-15 — Resultado de una operación con valor.
/// Solo se puede leer <see cref="Value"/> cuando <see cref="IsSuccess"/> es verdadero.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<DomainError> errors)
    {
        IsSuccess = isSuccess;
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<DomainError> Errors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "No se puede leer Value de un resultado fallido. Compruebe IsSuccess.");

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<DomainError>());

    public static Result<T> Fail(string code, string message) =>
        new(false, default, [new DomainError(code, message)]);

    public static Result<T> Fail(DomainError error) => new(false, default, [error]);

    public static Result<T> Fail(IReadOnlyList<DomainError> errors) =>
        new(false, default, errors);
}
