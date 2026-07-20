using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IPlotService
{
    Task<PlotResponseDto> CreateAsync(string adminId, CreatePlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlotResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> GetByIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> UpdateAsync(string adminId, string plotId, UpdatePlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string plotId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> AddOrUpdateChargeAsync(string adminId, string plotId, AddOrUpdatePlotChargeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> RemoveChargeAsync(string adminId, string plotId, string chargeType, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> SetAnnualMaintenanceChargeAsync(string adminId, string plotId, SetAnnualMaintenanceChargeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> ChangeStatusAsync(string adminId, string plotId, ChangePlotStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> UpdatePossessionStatusAsync(string adminId, string plotId, UpdatePlotPossessionStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlotResponseDto>> SplitAsync(string adminId, string plotId, SplitPlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<PlotResponseDto> MergeAsync(string adminId, MergePlotsRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<BulkImportPlotsResultDto> BulkImportAsync(string adminId, string fileContent, string performedByUserId, CancellationToken cancellationToken = default);
}
