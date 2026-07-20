using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IValidator<CreateClientRequestDto> _createValidator;
    private readonly IValidator<UpdateClientRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public ClientService(
        IClientRepository clientRepository,
        IAgentRepository agentRepository,
        IValidator<CreateClientRequestDto> createValidator,
        IValidator<UpdateClientRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _clientRepository = clientRepository;
        _agentRepository = agentRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<ClientResponseDto> CreateAsync(string adminId, CreateClientRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");

        var linkedAgentId = await ResolveLinkedAgentIdAsync(adminObjectId, request.LinkedAgentId, cancellationToken);
        var coOwnerIds = await ResolveCoOwnerIdsAsync(adminObjectId, request.CoOwnerClientIds, selfId: null, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var client = new Client
        {
            AdminId = adminObjectId,
            FullName = request.FullName.Trim(),
            CNIC = request.CNIC.Trim(),
            Phones = request.Phones.Select(phone => phone.Trim()).ToList(),
            Email = request.Email.Trim(),
            Address = request.Address.Trim(),
            EmergencyContact = request.EmergencyContact?.Trim() ?? string.Empty,
            LinkedAgentId = linkedAgentId,
            CoOwnerClientIds = coOwnerIds,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _clientRepository.CreateAsync(client, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "ClientCreated", "Client", client.Id.ToString(), adminId);

        return MapToDto(client);
    }

    public async Task<IReadOnlyList<ClientResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var clients = await _clientRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return clients.Select(MapToDto).ToList();
    }

    public async Task<ClientResponseDto> GetByIdAsync(string adminId, string clientId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var client = await LoadClientOrThrowAsync(adminObjectId, clientId, cancellationToken);
        return MapToDto(client);
    }

    public async Task<ClientResponseDto> UpdateAsync(string adminId, string clientId, UpdateClientRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var client = await LoadClientOrThrowAsync(adminObjectId, clientId, cancellationToken);

        var linkedAgentId = await ResolveLinkedAgentIdAsync(adminObjectId, request.LinkedAgentId, cancellationToken);
        var coOwnerIds = await ResolveCoOwnerIdsAsync(adminObjectId, request.CoOwnerClientIds, selfId: client.Id, cancellationToken);

        client.FullName = request.FullName.Trim();
        client.CNIC = request.CNIC.Trim();
        client.Phones = request.Phones.Select(phone => phone.Trim()).ToList();
        client.Email = request.Email.Trim();
        client.Address = request.Address.Trim();
        client.EmergencyContact = request.EmergencyContact?.Trim() ?? string.Empty;
        client.LinkedAgentId = linkedAgentId;
        client.CoOwnerClientIds = coOwnerIds;
        client.UpdatedAt = DateTime.UtcNow;
        client.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _clientRepository.UpdateAsync(client, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "ClientUpdated", "Client", client.Id.ToString(), adminId);

        return MapToDto(client);
    }

    public async Task DeleteAsync(string adminId, string clientId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var client = await LoadClientOrThrowAsync(adminObjectId, clientId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _clientRepository.SoftDeleteAsync(adminObjectId, client.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Client '{clientId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "ClientDeleted", "Client", client.Id.ToString(), adminId);
    }

    private async Task<ObjectId?> ResolveLinkedAgentIdAsync(ObjectId adminObjectId, string? linkedAgentId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(linkedAgentId))
        {
            return null;
        }

        var agentId = ParseObjectId(linkedAgentId, "LinkedAgentId");
        var agent = await _agentRepository.GetByIdAsync(adminObjectId, agentId, cancellationToken);
        if (agent is null)
        {
            throw new ValidationAppException(
                "The linked Agent was not found for this Admin.",
                new Dictionary<string, string[]> { ["LinkedAgentId"] = ["The linked Agent was not found for this Admin."] });
        }

        return agentId;
    }

    private async Task<List<ObjectId>> ResolveCoOwnerIdsAsync(ObjectId adminObjectId, List<string>? coOwnerClientIds, ObjectId? selfId, CancellationToken cancellationToken)
    {
        if (coOwnerClientIds is null || coOwnerClientIds.Count == 0)
        {
            return new List<ObjectId>();
        }

        var resolved = new List<ObjectId>(coOwnerClientIds.Count);
        foreach (var rawId in coOwnerClientIds)
        {
            var coOwnerId = ParseObjectId(rawId, "CoOwnerClientIds");

            if (selfId is not null && coOwnerId == selfId.Value)
            {
                throw new ValidationAppException(
                    "A Client cannot be listed as its own co-owner.",
                    new Dictionary<string, string[]> { ["CoOwnerClientIds"] = ["A Client cannot be listed as its own co-owner."] });
            }

            var coOwner = await _clientRepository.GetByIdAsync(adminObjectId, coOwnerId, cancellationToken);
            if (coOwner is null)
            {
                throw new ValidationAppException(
                    "One or more co-owner Clients were not found for this Admin.",
                    new Dictionary<string, string[]> { ["CoOwnerClientIds"] = ["One or more co-owner Clients were not found for this Admin."] });
            }

            resolved.Add(coOwnerId);
        }

        return resolved;
    }

    private async Task<Client> LoadClientOrThrowAsync(ObjectId adminObjectId, string clientId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(clientId, "clientId");
        var client = await _clientRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (client is null)
        {
            throw new NotFoundAppException($"Client '{clientId}' was not found.");
        }

        return client;
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

    private static ClientResponseDto MapToDto(Client client) => new(
        client.Id.ToString(),
        client.AdminId.ToString(),
        client.FullName,
        client.CNIC,
        client.Phones.ToList(),
        client.Email,
        client.Address,
        client.EmergencyContact,
        client.LinkedAgentId?.ToString(),
        client.CoOwnerClientIds.Select(id => id.ToString()).ToList(),
        client.CreatedAt,
        client.UpdatedAt);
}
