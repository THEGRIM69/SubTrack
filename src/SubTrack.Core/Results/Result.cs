namespace SubTrack.Core.Results;

/// <summary>
/// F0-15 — Resultado de una operación sin valor de retorno.
/// Un resultado fallido nunca contiene valor y no se lanza como excepción.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, IReadOnlyList<DomainError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public IReadOnlyList<DomainError> Errors { get; }

    public static Result Ok() => new(true, Array.Empty<DomainError>());

    public static Result Fail(string code, string message) =>
        new(false, [new DomainError(code, message)]);

    public static Result Fail(DomainError error) => new(false, [error]);

    public static Result Fail(IReadOnlyList<DomainError> errors) => new(false, errors);
}
