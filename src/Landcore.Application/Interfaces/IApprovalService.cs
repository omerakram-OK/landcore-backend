using Landcore.Application.DTOs;
using Landcore.Domain.Enums;

namespace Landcore.Application.Interfaces;

public interface IApprovalService
{
    Task<ApprovalRequestResponseDto> ProposeAsync(string adminId, string requestedByEmployeeId, ApprovalRequestType type, string? targetPlotId, string justification, string? payloadJson, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalRequestResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<ApprovalRequestResponseDto> GetByIdAsync(string adminId, string approvalRequestId, CancellationToken cancellationToken = default);

    Task<ApprovalRequestResponseDto> ApproveAsync(string adminId, string approvalRequestId, string decidedByUserId, string? decisionNotes, CancellationToken cancellationToken = default);

    Task<ApprovalRequestResponseDto> RejectAsync(string adminId, string approvalRequestId, string decidedByUserId, string decisionNotes, CancellationToken cancellationToken = default);
}
