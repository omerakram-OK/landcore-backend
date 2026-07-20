using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IValidator<CreateAgentRequestDto> _createValidator;
    private readonly IValidator<UpdateAgentRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public AgentService(
        IAgentRepository agentRepository,
        IValidator<CreateAgentRequestDto> createValidator,
        IValidator<UpdateAgentRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _agentRepository = agentRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<AgentResponseDto> CreateAsync(string adminId, CreateAgentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var agent = new Agent
        {
            AdminId = adminObjectId,
            FullName = request.FullName.Trim(),
            CNIC = request.CNIC.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Address = request.Address.Trim(),
            CommissionType = Enum.Parse<CommissionType>(request.CommissionType, ignoreCase: true),
            CommissionValue = (Decimal128)request.CommissionValue,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _agentRepository.CreateAsync(agent, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "AgentCreated", "Agent", agent.Id.ToString(), adminId);

        return MapToDto(agent);
    }

    public async Task<IReadOnlyList<AgentResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var agents = await _agentRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return agents.Select(MapToDto).ToList();
    }

    public async Task<AgentResponseDto> GetByIdAsync(string adminId, string agentId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var agent = await LoadAgentOrThrowAsync(adminObjectId, agentId, cancellationToken);
        return MapToDto(agent);
    }

    public async Task<AgentResponseDto> UpdateAsync(string adminId, string agentId, UpdateAgentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var agent = await LoadAgentOrThrowAsync(adminObjectId, agentId, cancellationToken);

        agent.FullName = request.FullName.Trim();
        agent.CNIC = request.CNIC.Trim();
        agent.Phone = request.Phone.Trim();
        agent.Email = request.Email.Trim();
        agent.Address = request.Address.Trim();

        agent.CommissionType = Enum.Parse<CommissionType>(request.CommissionType, ignoreCase: true);
        agent.CommissionValue = (Decimal128)request.CommissionValue;

        agent.UpdatedAt = DateTime.UtcNow;
        agent.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _agentRepository.UpdateAsync(agent, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "AgentUpdated", "Agent", agent.Id.ToString(), adminId);

        return MapToDto(agent);
    }

    public async Task DeleteAsync(string adminId, string agentId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var agent = await LoadAgentOrThrowAsync(adminObjectId, agentId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _agentRepository.SoftDeleteAsync(adminObjectId, agent.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Agent '{agentId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "AgentDeleted", "Agent", agent.Id.ToString(), adminId);
    }

    private async Task<Agent> LoadAgentOrThrowAsync(ObjectId adminObjectId, string agentId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(agentId, "agentId");
        var agent = await _agentRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (agent is null)
        {
            throw new NotFoundAppException($"Agent '{agentId}' was not found.");
        }

        return agent;
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

    private static AgentResponseDto MapToDto(Agent agent) => new(
        agent.Id.ToString(),
        agent.AdminId.ToString(),
        agent.FullName,
        agent.CNIC,
        agent.Phone,
        agent.Email,
        agent.Address,
        agent.CommissionType.ToString(),
        (decimal)agent.CommissionValue,
        agent.CreatedAt,
        agent.UpdatedAt);
}
