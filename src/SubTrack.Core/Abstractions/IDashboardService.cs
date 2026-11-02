using SubTrack.Core.Contracts;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T12 — Servicio del Dashboard. Infrastructure reúne los datos y Core calcula.
/// </summary>
public interface IDashboardService
{
    Task<Result<DashboardSnapshot>> GetAsync(
        GetDashboardRequest request,
        CancellationToken cancellationToken = default);
}
