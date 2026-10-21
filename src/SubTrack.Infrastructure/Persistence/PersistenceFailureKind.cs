namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Clasificación de fallos de persistencia para que la capa de presentación
/// pueda mostrar mensajes claros sin exponer detalles de SQLite.
/// </summary>
public enum PersistenceFailureKind
{
    Unknown = 0,
    Busy,
    Locked,
    ReadOnly,
    Constraint,
    IoError,
    Corrupt,
    DiskFull,
    CannotOpen,
    NotADatabase,
    ForeignDatabase
}
