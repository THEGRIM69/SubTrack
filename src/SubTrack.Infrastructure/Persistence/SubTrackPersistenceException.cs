namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Error controlado de persistencia. La ruta y el código del proveedor se conservan
/// para diagnóstico técnico, pero el mensaje está pensado para la capa de presentación.
/// </summary>
public sealed class SubTrackPersistenceException : Exception
{
    public SubTrackPersistenceException(
        string message,
        PersistenceFailureKind kind,
        string? databasePath = null,
        int? providerErrorCode = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Kind = kind;
        DatabasePath = databasePath;
        ProviderErrorCode = providerErrorCode;
    }

    public PersistenceFailureKind Kind { get; }

    public string? DatabasePath { get; }

    public int? ProviderErrorCode { get; }
}
