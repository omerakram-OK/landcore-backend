using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateEmployeeRequestDto> _createValidator;
    private readonly IValidator<UpdateEmployeeRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        IDesignationRepository designationRepository,
        IPasswordHasher passwordHasher,
        IValidator<CreateEmployeeRequestDto> createValidator,
        IValidator<UpdateEmployeeRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _employeeRepository = employeeRepository;
        _designationRepository = designationRepository;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<EmployeeResponseDto> CreateAsync(string adminId, CreateEmployeeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var designationId = ParseObjectId(request.DesignationId, "DesignationId");

        var designation = await _designationRepository.GetByIdAsync(adminObjectId, designationId, cancellationToken);
        if (designation is null)
        {
            throw new ValidationAppException(
                "The selected Designation was not found for this Admin.",
                new Dictionary<string, string[]> { ["DesignationId"] = ["The selected Designation was not found for this Admin."] });
        }

        var email = request.Email.Trim();
        var existing = await _employeeRepository.GetByEmailAsync(adminObjectId, email, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "An Employee with this email already exists.",
                new Dictionary<string, string[]> { ["Email"] = ["An Employee with this email already exists."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var employee = new Employee
        {
            AdminId = adminObjectId,
            FullName = request.FullName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            PasswordHash = _passwordHasher.Hash(request.InitialPassword),
            DesignationId = designationId,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _employeeRepository.CreateAsync(employee, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "EmployeeCreated", "Employee", employee.Id.ToString(), adminId);

        return MapToDto(employee, designation.Name);
    }

    public async Task<IReadOnlyList<EmployeeResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var employees = await _employeeRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);

        var designationNames = new Dictionary<ObjectId, string>();
        var result = new List<EmployeeResponseDto>(employees.Count);

        foreach (var employee in employees)
        {
            if (!designationNames.TryGetValue(employee.DesignationId, out var name))
            {
                var designation = await _designationRepository.GetByIdAsync(adminObjectId, employee.DesignationId, cancellationToken);
                name = designation?.Name ?? "(deleted Designation)";
                designationNames[employee.DesignationId] = name;
            }

            result.Add(MapToDto(employee, name));
        }

        return result;
    }

    public async Task<EmployeeResponseDto> GetByIdAsync(string adminId, string employeeId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var employee = await LoadEmployeeOrThrowAsync(adminObjectId, employeeId, cancellationToken);
        var designation = await _designationRepository.GetByIdAsync(adminObjectId, employee.DesignationId, cancellationToken);
        return MapToDto(employee, designation?.Name ?? "(deleted Designation)");
    }

    public async Task<EmployeeResponseDto> UpdateAsync(string adminId, string employeeId, UpdateEmployeeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var employee = await LoadEmployeeOrThrowAsync(adminObjectId, employeeId, cancellationToken);

        var designationId = ParseObjectId(request.DesignationId, "DesignationId");
        var designation = await _designationRepository.GetByIdAsync(adminObjectId, designationId, cancellationToken);
        if (designation is null)
        {
            throw new ValidationAppException(
                "The selected Designation was not found for this Admin.",
                new Dictionary<string, string[]> { ["DesignationId"] = ["The selected Designation was not found for this Admin."] });
        }

        var newEmail = request.Email.Trim();
        if (!string.Equals(newEmail, employee.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _employeeRepository.GetByEmailAsync(adminObjectId, newEmail, cancellationToken);
            if (existing is not null && existing.Id != employee.Id)
            {
                throw new ValidationAppException(
                    "An Employee with this email already exists.",
                    new Dictionary<string, string[]> { ["Email"] = ["An Employee with this email already exists."] });
            }
        }

        employee.FullName = request.FullName.Trim();
        employee.Email = newEmail;
        employee.Phone = request.Phone.Trim();
        employee.DesignationId = designationId;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _employeeRepository.UpdateAsync(employee, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "EmployeeUpdated", "Employee", employee.Id.ToString(), adminId);

        return MapToDto(employee, designation.Name);
    }

    public async Task<EmployeeResponseDto> DeactivateAsync(string adminId, string employeeId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var employee = await LoadEmployeeOrThrowAsync(adminObjectId, employeeId, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        var deleted = await _employeeRepository.SoftDeleteAsync(adminObjectId, employee.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Employee '{employeeId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "EmployeeDeactivated", "Employee", employee.Id.ToString(), adminId);

        var designation = await _designationRepository.GetByIdAsync(adminObjectId, employee.DesignationId, cancellationToken);

        var now = DateTime.UtcNow;
        employee.IsDeleted = true;
        employee.DeletedAt = now;
        employee.DeletedBy = performedBy;
        employee.UpdatedAt = now;
        employee.UpdatedBy = performedBy;

        return MapToDto(employee, designation?.Name ?? "(deleted Designation)");
    }

    private async Task<Employee> LoadEmployeeOrThrowAsync(ObjectId adminObjectId, string employeeId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(employeeId, "employeeId");
        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundAppException($"Employee '{employeeId}' was not found.");
        }

        return employee;
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

    private static EmployeeResponseDto MapToDto(Employee employee, string designationName) => new(
        employee.Id.ToString(),
        employee.AdminId.ToString(),
        employee.FullName,
        employee.Email,
        employee.Phone,
        employee.DesignationId.ToString(),
        designationName,
        !employee.IsDeleted,
        employee.CreatedAt,
        employee.UpdatedAt);
}
