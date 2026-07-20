using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IInstallmentPlanService
{
    Task<InstallmentPlanResponseDto> CreateAsync(string adminId, CreateInstallmentPlanRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InstallmentPlanResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<InstallmentPlanResponseDto> GetByIdAsync(string adminId, string planId, CancellationToken cancellationToken = default);

    Task<InstallmentPlanResponseDto> GetByBookingIdAsync(string adminId, string bookingId, CancellationToken cancellationToken = default);

    Task<InstallmentPlanResponseDto> ApplyDiscountAsync(string adminId, string planId, ApplyDiscountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);
}
