using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface ILeadService
{
    Task<LeadResponseDto> CreateAsync(string adminId, CreateLeadRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LeadResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<LeadResponseDto> GetByIdAsync(string adminId, string leadId, CancellationToken cancellationToken = default);

    Task<LeadResponseDto> UpdateAsync(string adminId, string leadId, UpdateLeadRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<LeadResponseDto> UpdateStatusAsync(string adminId, string leadId, UpdateLeadStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<LeadResponseDto> AppendFollowUpNoteAsync(string adminId, string leadId, AppendFollowUpNoteRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string leadId, string performedByUserId, CancellationToken cancellationToken = default);
}
