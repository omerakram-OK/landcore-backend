using System.Text.Json;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class ApprovalService : IApprovalService
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IApprovalRequestRepository _approvalRepository;
    private readonly IPlotService _plotService;
    private readonly IInstallmentPlanService _installmentPlanService;
    private readonly IRepossessionService _repossessionService;
    private readonly IAuditLogger _auditLogger;

    public ApprovalService(
        IApprovalRequestRepository approvalRepository,
        IPlotService plotService,
        IInstallmentPlanService installmentPlanService,
        IRepossessionService repossessionService,
        IAuditLogger auditLogger)
    {
        _approvalRepository = approvalRepository;
        _plotService = plotService;
        _installmentPlanService = installmentPlanService;
        _repossessionService = repossessionService;
        _auditLogger = auditLogger;
    }

    public async Task<ApprovalRequestResponseDto> ProposeAsync(string adminId, string requestedByEmployeeId, ApprovalRequestType type, string? targetPlotId, string justification, string? payloadJson, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(justification))
        {
            throw new ValidationAppException(
                "Justification is required.",
                new Dictionary<string, string[]> { ["Justification"] = ["Justification is required."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var requestedBy = ParseObjectId(requestedByEmployeeId, "requestedByEmployeeId");

        ObjectId? targetPlotObjectId = null;
        if (!string.IsNullOrWhiteSpace(targetPlotId))
        {
            targetPlotObjectId = ParseObjectId(targetPlotId, "targetPlotId");

            await _plotService.GetByIdAsync(adminId, targetPlotId, cancellationToken);
        }
        else if (type == ApprovalRequestType.RepossessionOverride)
        {
            throw new ValidationAppException(
                "TargetPlotId is required for a RepossessionOverride proposal.",
                new Dictionary<string, string[]> { ["TargetPlotId"] = ["TargetPlotId is required for a RepossessionOverride proposal."] });
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ValidationAppException(
                "PayloadJson is required.",
                new Dictionary<string, string[]> { ["PayloadJson"] = ["PayloadJson is required."] });
        }

        try
        {
            using var _ = JsonDocument.Parse(payloadJson);
        }
        catch (JsonException)
        {
            throw new ValidationAppException(
                "PayloadJson is not valid JSON.",
                new Dictionary<string, string[]> { ["PayloadJson"] = ["PayloadJson is not valid JSON."] });
        }

        var now = DateTime.UtcNow;
        var request = new ApprovalRequest
        {
            AdminId = adminObjectId,
            Type = type,
            RequestedByEmployeeId = requestedBy,
            Justification = justification.Trim(),
            Status = ApprovalRequestStatus.PendingApproval,
            TargetPlotId = targetPlotObjectId,
            PayloadJson = payloadJson,
            CreatedAt = now,
            CreatedBy = requestedBy,
            UpdatedAt = now,
            UpdatedBy = requestedBy,
            IsDeleted = false,
        };

        await _approvalRepository.CreateAsync(request, cancellationToken);

        _auditLogger.LogAction(requestedByEmployeeId, "ApprovalRequestProposed", "ApprovalRequest", request.Id.ToString(), adminId,
            new { Type = type.ToString(), TargetPlotId = targetPlotId });

        return MapToDto(request);
    }

    public async Task<IReadOnlyList<ApprovalRequestResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var requests = await _approvalRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return requests.Select(MapToDto).ToList();
    }

    public async Task<ApprovalRequestResponseDto> GetByIdAsync(string adminId, string approvalRequestId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var request = await LoadOrThrowAsync(adminObjectId, approvalRequestId, cancellationToken);
        return MapToDto(request);
    }

    public async Task<ApprovalRequestResponseDto> ApproveAsync(string adminId, string approvalRequestId, string decidedByUserId, string? decisionNotes, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var request = await LoadOrThrowAsync(adminObjectId, approvalRequestId, cancellationToken);

        EnsurePending(request);

        switch (request.Type)
        {
            case ApprovalRequestType.RepossessionOverride:
                await ExecuteRepossessionOverrideAsync(adminId, request, decidedByUserId, cancellationToken);
                break;

            case ApprovalRequestType.Refund:
                await ExecuteRefundAsync(adminId, request, decidedByUserId, cancellationToken);
                break;

            case ApprovalRequestType.MergeSplit:
                await ExecuteMergeSplitAsync(adminId, request, decidedByUserId, cancellationToken);
                break;

            case ApprovalRequestType.LargeDiscount:
                await ExecuteLargeDiscountAsync(adminId, request, decidedByUserId, cancellationToken);
                break;

            default:
                throw new ValidationAppException($"Unsupported ApprovalRequestType '{request.Type}'.");
        }

        var decidedBy = ParseObjectId(decidedByUserId, "decidedByUserId");
        var now = DateTime.UtcNow;

        request.Status = ApprovalRequestStatus.Approved;
        request.DecidedByAdminId = decidedBy;
        request.DecisionNotes = string.IsNullOrWhiteSpace(decisionNotes) ? null : decisionNotes.Trim();
        request.UpdatedAt = now;
        request.UpdatedBy = decidedBy;

        await _approvalRepository.UpdateAsync(request, cancellationToken);

        _auditLogger.LogAction(decidedByUserId, "ApprovalRequestApproved", "ApprovalRequest", request.Id.ToString(), adminId,
            new { Type = request.Type.ToString() });

        return MapToDto(request);
    }

    public async Task<ApprovalRequestResponseDto> RejectAsync(string adminId, string approvalRequestId, string decidedByUserId, string decisionNotes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(decisionNotes))
        {
            throw new ValidationAppException(
                "DecisionNotes (the rejection reason) is required.",
                new Dictionary<string, string[]> { ["DecisionNotes"] = ["DecisionNotes (the rejection reason) is required."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var request = await LoadOrThrowAsync(adminObjectId, approvalRequestId, cancellationToken);

        EnsurePending(request);

        var decidedBy = ParseObjectId(decidedByUserId, "decidedByUserId");
        var now = DateTime.UtcNow;

        request.Status = ApprovalRequestStatus.Rejected;
        request.DecidedByAdminId = decidedBy;
        request.DecisionNotes = decisionNotes.Trim();
        request.UpdatedAt = now;
        request.UpdatedBy = decidedBy;

        await _approvalRepository.UpdateAsync(request, cancellationToken);

        _auditLogger.LogAction(decidedByUserId, "ApprovalRequestRejected", "ApprovalRequest", request.Id.ToString(), adminId,
            new { Type = request.Type.ToString(), Reason = decisionNotes });

        return MapToDto(request);
    }

    private async Task ExecuteRepossessionOverrideAsync(string adminId, ApprovalRequest request, string decidedByUserId, CancellationToken cancellationToken)
    {
        if (request.TargetPlotId is null)
        {
            throw new ValidationAppException("This ApprovalRequest is missing its TargetPlotId.");
        }

        var payload = DeserializePayload<RepossessionOverridePayload>(request.PayloadJson);
        await _repossessionService.ResumePlanAsync(adminId, request.TargetPlotId.Value.ToString(), payload.Notes, decidedByUserId, cancellationToken);
    }

    private async Task ExecuteRefundAsync(string adminId, ApprovalRequest request, string decidedByUserId, CancellationToken cancellationToken)
    {
        var payload = DeserializePayload<RefundPayload>(request.PayloadJson);
        if (string.IsNullOrWhiteSpace(payload.RefundRecordId))
        {
            throw new ValidationAppException("This ApprovalRequest's payload is missing RefundRecordId.");
        }

        await _repossessionService.IssueRefundAsync(adminId, payload.RefundRecordId, decidedByUserId, cancellationToken);
    }

    private async Task ExecuteMergeSplitAsync(string adminId, ApprovalRequest request, string decidedByUserId, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.PayloadJson ?? "{}");
        var root = document.RootElement;

        if (!root.TryGetProperty("Operation", out var operationElement))
        {
            throw new ValidationAppException("This ApprovalRequest's payload is missing Operation.");
        }

        var operation = operationElement.GetString();
        if (!root.TryGetProperty("Request", out var requestElement))
        {
            throw new ValidationAppException("This ApprovalRequest's payload is missing Request.");
        }

        if (string.Equals(operation, "Split", StringComparison.OrdinalIgnoreCase))
        {
            if (!root.TryGetProperty("PlotId", out var plotIdElement) || plotIdElement.GetString() is not { } plotId)
            {
                throw new ValidationAppException("This ApprovalRequest's Split payload is missing PlotId.");
            }

            var splitRequest = requestElement.Deserialize<SplitPlotRequestDto>(PayloadJsonOptions)
                ?? throw new ValidationAppException("This ApprovalRequest's Split payload could not be parsed.");

            await _plotService.SplitAsync(adminId, plotId, splitRequest, decidedByUserId, cancellationToken);
        }
        else if (string.Equals(operation, "Merge", StringComparison.OrdinalIgnoreCase))
        {
            var mergeRequest = requestElement.Deserialize<MergePlotsRequestDto>(PayloadJsonOptions)
                ?? throw new ValidationAppException("This ApprovalRequest's Merge payload could not be parsed.");

            await _plotService.MergeAsync(adminId, mergeRequest, decidedByUserId, cancellationToken);
        }
        else
        {
            throw new ValidationAppException($"Unsupported MergeSplit Operation '{operation}'.");
        }
    }

    private async Task ExecuteLargeDiscountAsync(string adminId, ApprovalRequest request, string decidedByUserId, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.PayloadJson ?? "{}");
        var root = document.RootElement;

        if (!root.TryGetProperty("PlanId", out var planIdElement) || planIdElement.GetString() is not { } planId)
        {
            throw new ValidationAppException("This ApprovalRequest's LargeDiscount payload is missing PlanId.");
        }

        if (!root.TryGetProperty("Request", out var requestElement))
        {
            throw new ValidationAppException("This ApprovalRequest's payload is missing Request.");
        }

        var discountRequest = requestElement.Deserialize<ApplyDiscountRequestDto>(PayloadJsonOptions)
            ?? throw new ValidationAppException("This ApprovalRequest's LargeDiscount payload could not be parsed.");

        await _installmentPlanService.ApplyDiscountAsync(adminId, planId, discountRequest, decidedByUserId, cancellationToken);
    }

    private static T DeserializePayload<T>(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            throw new ValidationAppException("This ApprovalRequest is missing its PayloadJson.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, PayloadJsonOptions)
                ?? throw new ValidationAppException("This ApprovalRequest's payload could not be parsed.");
        }
        catch (JsonException)
        {
            throw new ValidationAppException("This ApprovalRequest's payload is not valid JSON for its Type.");
        }
    }

    private static void EnsurePending(ApprovalRequest request)
    {
        if (request.Status != ApprovalRequestStatus.PendingApproval)
        {
            throw new ValidationAppException(
                $"ApprovalRequest is already '{request.Status}'.",
                new Dictionary<string, string[]> { ["Status"] = [$"ApprovalRequest is already '{request.Status}'."] });
        }
    }

    private async Task<ApprovalRequest> LoadOrThrowAsync(ObjectId adminObjectId, string approvalRequestId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(approvalRequestId, "approvalRequestId");
        var request = await _approvalRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (request is null)
        {
            throw new NotFoundAppException($"ApprovalRequest '{approvalRequestId}' was not found.");
        }

        return request;
    }

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

    private static ApprovalRequestResponseDto MapToDto(ApprovalRequest request) => new(
        request.Id.ToString(),
        request.AdminId.ToString(),
        request.Type.ToString(),
        request.RequestedByEmployeeId.ToString(),
        request.TargetPlotId?.ToString(),
        request.Justification,
        request.Status.ToString(),
        request.DecidedByAdminId?.ToString(),
        request.DecisionNotes,
        request.PayloadJson,
        request.CreatedAt,
        request.UpdatedAt);

    private sealed record RepossessionOverridePayload(string? Notes);

    private sealed record RefundPayload(string? RefundRecordId);
}
