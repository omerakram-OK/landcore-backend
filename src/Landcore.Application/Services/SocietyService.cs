using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class SocietyService : ISocietyService
{
    private readonly ISocietyRepository _societyRepository;
    private readonly IValidator<CreateSocietyRequestDto> _createValidator;
    private readonly IValidator<UpdateSocietyRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public SocietyService(
        ISocietyRepository societyRepository,
        IValidator<CreateSocietyRequestDto> createValidator,
        IValidator<UpdateSocietyRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _societyRepository = societyRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<SocietyResponseDto> CreateAsync(string adminId, CreateSocietyRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var name = request.Name.Trim();

        var existing = await _societyRepository.GetByNameAsync(adminObjectId, name, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "A Society with this name already exists.",
                new Dictionary<string, string[]> { ["Name"] = ["A Society with this name already exists."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var society = new Society
        {
            AdminId = adminObjectId,
            Name = name,
            Address = request.Address.Trim(),
            Description = request.Description.Trim(),
            TotalPlots = request.TotalPlots,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _societyRepository.CreateAsync(society, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "SocietyCreated", "Society", society.Id.ToString(), adminId);

        return MapToDto(society);
    }

    public async Task<IReadOnlyList<SocietyResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var societies = await _societyRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return societies.Select(MapToDto).ToList();
    }

    public async Task<SocietyResponseDto> GetByIdAsync(string adminId, string societyId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var society = await LoadSocietyOrThrowAsync(adminObjectId, societyId, cancellationToken);
        return MapToDto(society);
    }

    public async Task<SocietyResponseDto> UpdateAsync(string adminId, string societyId, UpdateSocietyRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var society = await LoadSocietyOrThrowAsync(adminObjectId, societyId, cancellationToken);

        var newName = request.Name.Trim();
        if (!string.Equals(newName, society.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _societyRepository.GetByNameAsync(adminObjectId, newName, cancellationToken);
            if (existing is not null && existing.Id != society.Id)
            {
                throw new ValidationAppException(
                    "A Society with this name already exists.",
                    new Dictionary<string, string[]> { ["Name"] = ["A Society with this name already exists."] });
            }
        }

        society.Name = newName;
        society.Address = request.Address.Trim();
        society.Description = request.Description.Trim();
        society.TotalPlots = request.TotalPlots;
        society.UpdatedAt = DateTime.UtcNow;
        society.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _societyRepository.UpdateAsync(society, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "SocietyUpdated", "Society", society.Id.ToString(), adminId);

        return MapToDto(society);
    }

    public async Task DeleteAsync(string adminId, string societyId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var society = await LoadSocietyOrThrowAsync(adminObjectId, societyId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _societyRepository.SoftDeleteAsync(adminObjectId, society.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Society '{societyId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "SocietyDeleted", "Society", society.Id.ToString(), adminId);
    }

    private async Task<Society> LoadSocietyOrThrowAsync(ObjectId adminObjectId, string societyId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(societyId, "societyId");
        var society = await _societyRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (society is null)
        {
            throw new NotFoundAppException($"Society '{societyId}' was not found.");
        }

        return society;
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

    private static SocietyResponseDto MapToDto(Society society) => new(
        society.Id.ToString(),
        society.AdminId.ToString(),
        society.Name,
        society.Address,
        society.Description,
        society.TotalPlots,
        society.CreatedAt,
        society.UpdatedAt);
}
