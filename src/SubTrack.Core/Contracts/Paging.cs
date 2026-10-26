namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-T29 — Página solicitada. Por defecto 50 registros, del más reciente al más antiguo.
/// </summary>
public sealed record PageRequest(int Page = 1, int PageSize = 50)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
}

/// <summary>
/// F0-T29 — Página de resultados con su total, para no cargar historiales completos.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (TotalCount + PageSize - 1) / PageSize;

    public bool HasNextPage => Page < TotalPages;
}
