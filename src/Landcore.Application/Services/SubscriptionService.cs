using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IValidator<CreateSubscriptionRequestDto> _createValidator;
    private readonly IValidator<UpdateSubscriptionRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public SubscriptionService(
        ISubscriptionRepository subscriptionRepository,
        IAdminRepository adminRepository,
        IValidator<CreateSubscriptionRequestDto> createValidator,
        IValidator<UpdateSubscriptionRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _subscriptionRepository = subscriptionRepository;
        _adminRepository = adminRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<SubscriptionResponseDto> CreateAsync(CreateSubscriptionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminId = ParseObjectId(request.AdminId, "AdminId");
        var admin = await _adminRepository.GetByIdAsync(adminId, cancellationToken);
        if (admin is null)
        {
            throw new NotFoundAppException($"Admin '{request.AdminId}' was not found.");
        }

        var existing = await _subscriptionRepository.GetByAdminIdAsync(adminId, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "This Admin already has a Subscription. Use update/activate/suspend/reactivate instead of creating a new one.",
                new Dictionary<string, string[]> { ["AdminId"] = ["This Admin already has a Subscription."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var plan = Enum.Parse<SubscriptionPlan>(request.Plan, ignoreCase: true);
        var status = request.Status is null ? SubscriptionStatus.Active : Enum.Parse<SubscriptionStatus>(request.Status, ignoreCase: true);

        var subscription = new Subscription
        {
            AdminId = adminId,
            Plan = plan,
            FeeAmount = (Decimal128)request.FeeAmount,
            StartDate = request.StartDate,
            NextDueDate = request.NextDueDate,
            Status = status,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _subscriptionRepository.CreateAsync(subscription, cancellationToken);

        await _adminRepository.SetSubscriptionIdAsync(adminId, subscription.Id, performedBy, cancellationToken);
        await _adminRepository.UpdateStatusAsync(adminId, MapAdminStatus(status), performedBy, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "SubscriptionCreated", "Subscription", subscription.Id.ToString(), adminId.ToString());

        return MapToDto(subscription);
    }

    public async Task<IReadOnlyList<SubscriptionResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _subscriptionRepository.GetAllAsync(cancellationToken);
        return subscriptions.Select(MapToDto).ToList();
    }

    public async Task<SubscriptionResponseDto> GetByIdAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await LoadSubscriptionOrThrowAsync(subscriptionId, cancellationToken);
        return MapToDto(subscription);
    }

    public async Task<SubscriptionResponseDto> GetByAdminIdAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var id = ParseObjectId(adminId, "adminId");
        var subscription = await _subscriptionRepository.GetByAdminIdAsync(id, cancellationToken);
        if (subscription is null)
        {
            throw new NotFoundAppException($"Admin '{adminId}' does not have a Subscription yet.");
        }

        return MapToDto(subscription);
    }

    public async Task<SubscriptionResponseDto> UpdateAsync(string subscriptionId, UpdateSubscriptionRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var subscription = await LoadSubscriptionOrThrowAsync(subscriptionId, cancellationToken);

        subscription.Plan = Enum.Parse<SubscriptionPlan>(request.Plan, ignoreCase: true);
        subscription.FeeAmount = (Decimal128)request.FeeAmount;
        subscription.StartDate = request.StartDate;
        subscription.NextDueDate = request.NextDueDate;
        subscription.UpdatedAt = DateTime.UtcNow;
        subscription.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _subscriptionRepository.UpdateAsync(subscription, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "SubscriptionUpdated", "Subscription", subscription.Id.ToString(), subscription.AdminId.ToString());

        return MapToDto(subscription);
    }

    public Task<SubscriptionResponseDto> ActivateAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(subscriptionId, SubscriptionStatus.Active, "SubscriptionActivated", performedByUserId, cancellationToken);

    public Task<SubscriptionResponseDto> SuspendAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(subscriptionId, SubscriptionStatus.Suspended, "SubscriptionSuspended", performedByUserId, cancellationToken);

    public Task<SubscriptionResponseDto> ReactivateAsync(string subscriptionId, string performedByUserId, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(subscriptionId, SubscriptionStatus.Active, "SubscriptionReactivated", performedByUserId, cancellationToken);

    private async Task<SubscriptionResponseDto> ChangeStatusAsync(
        string subscriptionId, SubscriptionStatus newStatus, string auditAction, string performedByUserId, CancellationToken cancellationToken)
    {
        var subscription = await LoadSubscriptionOrThrowAsync(subscriptionId, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _subscriptionRepository.UpdateStatusAsync(subscription.Id, newStatus, performedBy, cancellationToken);

        await _adminRepository.UpdateStatusAsync(subscription.AdminId, MapAdminStatus(newStatus), performedBy, cancellationToken);

        _auditLogger.LogAction(performedByUserId, auditAction, "Subscription", subscription.Id.ToString(), subscription.AdminId.ToString());

        subscription.Status = newStatus;
        return MapToDto(subscription);
    }

    private async Task<Subscription> LoadSubscriptionOrThrowAsync(string subscriptionId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(subscriptionId, "subscriptionId");
        var subscription = await _subscriptionRepository.GetByIdAsync(id, cancellationToken);
        if (subscription is null)
        {
            throw new NotFoundAppException($"Subscription '{subscriptionId}' was not found.");
        }

        return subscription;
    }

    private static AdminStatus MapAdminStatus(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Active => AdminStatus.Active,
        _ => AdminStatus.Suspended,
    };

    private static ObjectId ParseObjectId(string value, string fieldName)
    {
        if (!ObjectId.TryParse(value, out var id))
        {
            throw new ValidationAppException(
                $"'{fieldName}' is not a valid identifier.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid identifier."] });
        }

        return id;
    }

    private static SubscriptionResponseDto MapToDto(Subscription subscription) => new(
        subscription.Id.ToString(),
        subscription.AdminId.ToString(),
        subscription.Plan.ToString(),
        (decimal)subscription.FeeAmount,
        subscription.StartDate,
        subscription.NextDueDate,
        subscription.Status.ToString());
}
