using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface ISubscriptionService
{
    Task<SubscriptionResponseDto> CreateAsync(CreateSubscriptionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> GetByIdAsync(string subscriptionId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> GetByAdminIdAsync(string adminId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> UpdateAsync(string subscriptionId, UpdateSubscriptionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> ActivateAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> SuspendAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<SubscriptionResponseDto> ReactivateAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default);
}
