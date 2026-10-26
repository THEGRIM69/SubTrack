namespace SubTrack.Core.Contracts;

/// <summary>
/// F0-13 — Alta de categoría de gasto propia.
/// </summary>
public sealed record CreateExpenseCategoryRequest(string Name);

/// <summary>
/// F0-T11 — Cambio de nombre de una categoría con historial protegido.
/// </summary>
public sealed record UpdateExpenseCategoryRequest(Guid CategoryId, string Name);
