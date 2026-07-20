using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IAgentService
{
    Task<AgentResponseDto> CreateAsync(string adminId, CreateAgentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AgentResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<AgentResponseDto> GetByIdAsync(string adminId, string agentId, CancellationToken cancellationToken = default);

    Task<AgentResponseDto> UpdateAsync(string adminId, string agentId, UpdateAgentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string agentId, string performedByUserId, CancellationToken cancellationToken = default);
}
