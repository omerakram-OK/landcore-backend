using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _adminRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateAdminRequestDto> _createValidator;
    private readonly IValidator<UpdateAdminRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public AdminService(
        IAdminRepository adminRepository,
        ISubscriptionRepository subscriptionRepository,
        IPasswordHasher passwordHasher,
        IValidator<CreateAdminRequestDto> createValidator,
        IValidator<UpdateAdminRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _adminRepository = adminRepository;
        _subscriptionRepository = subscriptionRepository;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<AdminResponseDto> CreateAsync(CreateAdminRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var contactEmail = request.ContactEmail.Trim();
        var existing = await _adminRepository.GetByContactEmailAsync(contactEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "An Admin with this contact email already exists.",
                new Dictionary<string, string[]> { ["ContactEmail"] = ["An Admin with this contact email already exists."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var admin = new Admin
        {
            SocietyName = request.SocietyName.Trim(),
            ContactEmail = contactEmail,
            PasswordHash = _passwordHasher.Hash(request.InitialPassword),
            SubscriptionId = ObjectId.Empty,
            Status = AdminStatus.Active,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _adminRepository.CreateAsync(admin, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "AdminCreated", "Admin", admin.Id.ToString(), admin.Id.ToString());

        return MapToDto(admin);
    }

    public async Task<IReadOnlyList<AdminResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var admins = await _adminRepository.GetAllAsync(cancellationToken);
        return admins.Select(MapToDto).ToList();
    }

    public async Task<AdminResponseDto> GetByIdAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var admin = await LoadAdminOrThrowAsync(adminId, cancellationToken);
        return MapToDto(admin);
    }

    public async Task<AdminResponseDto> UpdateAsync(string adminId, UpdateAdminRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var admin = await LoadAdminOrThrowAsync(adminId, cancellationToken);

        var newContactEmail = request.ContactEmail.Trim();
        if (!string.Equals(newContactEmail, admin.ContactEmail, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _adminRepository.GetByContactEmailAsync(newContactEmail, cancellationToken);
            if (existing is not null && existing.Id != admin.Id)
            {
                throw new ValidationAppException(
                    "An Admin with this contact email already exists.",
                    new Dictionary<string, string[]> { ["ContactEmail"] = ["An Admin with this contact email already exists."] });
            }
        }

        admin.SocietyName = request.SocietyName.Trim();
        admin.ContactEmail = newContactEmail;
        admin.UpdatedAt = DateTime.UtcNow;
        admin.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _adminRepository.UpdateAsync(admin, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "AdminUpdated", "Admin", admin.Id.ToString(), admin.Id.ToString());

        return MapToDto(admin);
    }

    public async Task<AdminResponseDto> SuspendAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var admin = await LoadAdminOrThrowAsync(adminId, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _adminRepository.UpdateStatusAsync(admin.Id, AdminStatus.Suspended, performedBy, cancellationToken);

        if (admin.SubscriptionId != ObjectId.Empty)
        {
            await _subscriptionRepository.UpdateStatusAsync(admin.SubscriptionId, SubscriptionStatus.Suspended, performedBy, cancellationToken);
        }

        _auditLogger.LogAction(performedByUserId, "AdminSuspended", "Admin", admin.Id.ToString(), admin.Id.ToString());

        admin.Status = AdminStatus.Suspended;
        return MapToDto(admin);
    }

    public async Task<AdminResponseDto> ReactivateAsync(string adminId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var admin = await LoadAdminOrThrowAsync(adminId, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _adminRepository.UpdateStatusAsync(admin.Id, AdminStatus.Active, performedBy, cancellationToken);

        if (admin.SubscriptionId != ObjectId.Empty)
        {
            await _subscriptionRepository.UpdateStatusAsync(admin.SubscriptionId, SubscriptionStatus.Active, performedBy, cancellationToken);
        }

        _auditLogger.LogAction(performedByUserId, "AdminReactivated", "Admin", admin.Id.ToString(), admin.Id.ToString());

        admin.Status = AdminStatus.Active;
        return MapToDto(admin);
    }

    private async Task<Admin> LoadAdminOrThrowAsync(string adminId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(adminId, "adminId");
        var admin = await _adminRepository.GetByIdAsync(id, cancellationToken);
        if (admin is null)
        {
            throw new NotFoundAppException($"Admin '{adminId}' was not found.");
        }

        return admin;
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

    private static AdminResponseDto MapToDto(Admin admin) => new(
        admin.Id.ToString(),
        admin.SocietyName,
        admin.ContactEmail,
        admin.Status.ToString(),
        admin.SubscriptionId == ObjectId.Empty ? null : admin.SubscriptionId.ToString(),
        admin.CreatedAt,
        admin.UpdatedAt);
}
