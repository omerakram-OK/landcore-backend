using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _designationRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IValidator<CreateDesignationRequestDto> _createValidator;
    private readonly IValidator<UpdateDesignationRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public DesignationService(
        IDesignationRepository designationRepository,
        IEmployeeRepository employeeRepository,
        IValidator<CreateDesignationRequestDto> createValidator,
        IValidator<UpdateDesignationRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _designationRepository = designationRepository;
        _employeeRepository = employeeRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<DesignationResponseDto> CreateAsync(string adminId, CreateDesignationRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var name = request.Name.Trim();

        var existing = await _designationRepository.GetByNameAsync(adminObjectId, name, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "A Designation with this name already exists.",
                new Dictionary<string, string[]> { ["Name"] = ["A Designation with this name already exists."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var designation = new Designation
        {
            AdminId = adminObjectId,
            Name = name,
            Permissions = MapToDomainPermissions(request.Permissions),
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _designationRepository.CreateAsync(designation, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "DesignationCreated", "Designation", designation.Id.ToString(), adminId);

        return MapToDto(designation);
    }

    public async Task<IReadOnlyList<DesignationResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var designations = await _designationRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return designations.Select(MapToDto).ToList();
    }

    public async Task<DesignationResponseDto> GetByIdAsync(string adminId, string designationId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var designation = await LoadDesignationOrThrowAsync(adminObjectId, designationId, cancellationToken);
        return MapToDto(designation);
    }

    public async Task<DesignationResponseDto> UpdateAsync(string adminId, string designationId, UpdateDesignationRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var designation = await LoadDesignationOrThrowAsync(adminObjectId, designationId, cancellationToken);

        var newName = request.Name.Trim();
        if (!string.Equals(newName, designation.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _designationRepository.GetByNameAsync(adminObjectId, newName, cancellationToken);
            if (existing is not null && existing.Id != designation.Id)
            {
                throw new ValidationAppException(
                    "A Designation with this name already exists.",
                    new Dictionary<string, string[]> { ["Name"] = ["A Designation with this name already exists."] });
            }
        }

        designation.Name = newName;
        designation.Permissions = MapToDomainPermissions(request.Permissions);
        designation.UpdatedAt = DateTime.UtcNow;
        designation.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _designationRepository.UpdateAsync(designation, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "DesignationUpdated", "Designation", designation.Id.ToString(), adminId);

        return MapToDto(designation);
    }

    public async Task DeleteAsync(string adminId, string designationId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var designation = await LoadDesignationOrThrowAsync(adminObjectId, designationId, cancellationToken);

        var activeEmployeeCount = await _employeeRepository.CountActiveByDesignationIdAsync(adminObjectId, designation.Id, cancellationToken);
        if (activeEmployeeCount > 0)
        {
            throw new ValidationAppException(
                "This Designation is still assigned to one or more active Employees and cannot be deleted. Reassign those Employees to a different Designation first.",
                new Dictionary<string, string[]> { ["DesignationId"] = ["This Designation is still assigned to one or more active Employees."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _designationRepository.SoftDeleteAsync(adminObjectId, designation.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Designation '{designationId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "DesignationDeleted", "Designation", designation.Id.ToString(), adminId);
    }

    private async Task<Designation> LoadDesignationOrThrowAsync(ObjectId adminObjectId, string designationId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(designationId, "designationId");
        var designation = await _designationRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (designation is null)
        {
            throw new NotFoundAppException($"Designation '{designationId}' was not found.");
        }

        return designation;
    }

    private static List<Permission> MapToDomainPermissions(List<PermissionDto> permissions) =>
        permissions
            .Select(permission => new Permission
            {
                Module = permission.Module.Trim(),
                Actions = permission.Actions.Select(action => action.Trim()).ToList(),
            })
            .ToList();

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

    private static DesignationResponseDto MapToDto(Designation designation) => new(
        designation.Id.ToString(),
        designation.AdminId.ToString(),
        designation.Name,
        designation.Permissions.Select(permission => new PermissionDto(permission.Module, permission.Actions.ToList())).ToList(),
        designation.CreatedAt,
        designation.UpdatedAt);
}
