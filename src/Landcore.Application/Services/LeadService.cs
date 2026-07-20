using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class LeadService : ILeadService
{
    private readonly ILeadRepository _leadRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IValidator<CreateLeadRequestDto> _createValidator;
    private readonly IValidator<UpdateLeadRequestDto> _updateValidator;
    private readonly IValidator<UpdateLeadStatusRequestDto> _updateStatusValidator;
    private readonly IValidator<AppendFollowUpNoteRequestDto> _appendNoteValidator;
    private readonly IAuditLogger _auditLogger;

    public LeadService(
        ILeadRepository leadRepository,
        IEmployeeRepository employeeRepository,
        IValidator<CreateLeadRequestDto> createValidator,
        IValidator<UpdateLeadRequestDto> updateValidator,
        IValidator<UpdateLeadStatusRequestDto> updateStatusValidator,
        IValidator<AppendFollowUpNoteRequestDto> appendNoteValidator,
        IAuditLogger auditLogger)
    {
        _leadRepository = leadRepository;
        _employeeRepository = employeeRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _updateStatusValidator = updateStatusValidator;
        _appendNoteValidator = appendNoteValidator;
        _auditLogger = auditLogger;
    }

    public async Task<LeadResponseDto> CreateAsync(string adminId, CreateLeadRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var assignedEmployeeId = ParseObjectId(request.AssignedEmployeeId, "AssignedEmployeeId");

        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, assignedEmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new ValidationAppException(
                "The assigned Employee was not found for this Admin.",
                new Dictionary<string, string[]> { ["AssignedEmployeeId"] = ["The assigned Employee was not found for this Admin."] });
        }

        var interestedPlotId = ParseOptionalObjectId(request.InterestedPlotId, "InterestedPlotId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var lead = new Lead
        {
            AdminId = adminObjectId,
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            Email = request.Email.Trim(),
            Source = Enum.Parse<LeadSource>(request.Source, ignoreCase: true),
            InterestedPlotId = interestedPlotId,
            Status = LeadStatus.New,
            AssignedEmployeeId = assignedEmployeeId,
            FollowUpNotes = new List<Lead.FollowUpNote>(),
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _leadRepository.CreateAsync(lead, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LeadCreated", "Lead", lead.Id.ToString(), adminId);

        return MapToDto(lead, employee.FullName);
    }

    public async Task<IReadOnlyList<LeadResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var leads = await _leadRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);

        var employeeNames = new Dictionary<ObjectId, string>();
        var result = new List<LeadResponseDto>(leads.Count);

        foreach (var lead in leads)
        {
            if (!employeeNames.TryGetValue(lead.AssignedEmployeeId, out var name))
            {
                var employee = await _employeeRepository.GetByIdAsync(adminObjectId, lead.AssignedEmployeeId, cancellationToken);
                name = employee?.FullName ?? "(deleted Employee)";
                employeeNames[lead.AssignedEmployeeId] = name;
            }

            result.Add(MapToDto(lead, name));
        }

        return result;
    }

    public async Task<LeadResponseDto> GetByIdAsync(string adminId, string leadId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var lead = await LoadLeadOrThrowAsync(adminObjectId, leadId, cancellationToken);
        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, lead.AssignedEmployeeId, cancellationToken);
        return MapToDto(lead, employee?.FullName ?? "(deleted Employee)");
    }

    public async Task<LeadResponseDto> UpdateAsync(string adminId, string leadId, UpdateLeadRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var lead = await LoadLeadOrThrowAsync(adminObjectId, leadId, cancellationToken);

        var assignedEmployeeId = ParseObjectId(request.AssignedEmployeeId, "AssignedEmployeeId");
        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, assignedEmployeeId, cancellationToken);
        if (employee is null)
        {
            throw new ValidationAppException(
                "The assigned Employee was not found for this Admin.",
                new Dictionary<string, string[]> { ["AssignedEmployeeId"] = ["The assigned Employee was not found for this Admin."] });
        }

        lead.Name = request.Name.Trim();
        lead.Phone = request.Phone.Trim();
        lead.Email = request.Email.Trim();
        lead.Source = Enum.Parse<LeadSource>(request.Source, ignoreCase: true);
        lead.InterestedPlotId = ParseOptionalObjectId(request.InterestedPlotId, "InterestedPlotId");
        lead.AssignedEmployeeId = assignedEmployeeId;
        lead.UpdatedAt = DateTime.UtcNow;
        lead.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _leadRepository.UpdateAsync(lead, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LeadUpdated", "Lead", lead.Id.ToString(), adminId);

        return MapToDto(lead, employee.FullName);
    }

    public async Task<LeadResponseDto> UpdateStatusAsync(string adminId, string leadId, UpdateLeadStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateStatusValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var lead = await LoadLeadOrThrowAsync(adminObjectId, leadId, cancellationToken);

        var newStatus = Enum.Parse<LeadStatus>(request.Status, ignoreCase: true);

        lead.Status = newStatus;
        lead.UpdatedAt = DateTime.UtcNow;
        lead.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _leadRepository.UpdateAsync(lead, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LeadStatusChanged", "Lead", lead.Id.ToString(), adminId, new { NewStatus = newStatus.ToString() });

        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, lead.AssignedEmployeeId, cancellationToken);
        return MapToDto(lead, employee?.FullName ?? "(deleted Employee)");
    }

    public async Task<LeadResponseDto> AppendFollowUpNoteAsync(string adminId, string leadId, AppendFollowUpNoteRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_appendNoteValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var lead = await LoadLeadOrThrowAsync(adminObjectId, leadId, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        lead.FollowUpNotes.Add(new Lead.FollowUpNote
        {
            Note = request.Note.Trim(),
            By = performedBy,
            At = now,
        });
        lead.UpdatedAt = now;
        lead.UpdatedBy = performedBy;

        await _leadRepository.UpdateAsync(lead, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "LeadFollowUpNoteAdded", "Lead", lead.Id.ToString(), adminId);

        var employee = await _employeeRepository.GetByIdAsync(adminObjectId, lead.AssignedEmployeeId, cancellationToken);
        return MapToDto(lead, employee?.FullName ?? "(deleted Employee)");
    }

    public async Task DeleteAsync(string adminId, string leadId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var lead = await LoadLeadOrThrowAsync(adminObjectId, leadId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _leadRepository.SoftDeleteAsync(adminObjectId, lead.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Lead '{leadId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "LeadDeleted", "Lead", lead.Id.ToString(), adminId);
    }

    private async Task<Lead> LoadLeadOrThrowAsync(ObjectId adminObjectId, string leadId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(leadId, "leadId");
        var lead = await _leadRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (lead is null)
        {
            throw new NotFoundAppException($"Lead '{leadId}' was not found.");
        }

        return lead;
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

    private static ObjectId? ParseOptionalObjectId(string? value, string fieldName) =>
        string.IsNullOrWhiteSpace(value) ? null : ParseObjectId(value, fieldName);

    private static LeadResponseDto MapToDto(Lead lead, string assignedEmployeeName) => new(
        lead.Id.ToString(),
        lead.AdminId.ToString(),
        lead.Name,
        lead.Phone,
        lead.Email,
        lead.Source.ToString(),
        lead.InterestedPlotId?.ToString(),
        lead.Status.ToString(),
        lead.AssignedEmployeeId.ToString(),
        assignedEmployeeName,
        lead.FollowUpNotes.Select(note => new FollowUpNoteDto(note.Note, note.By.ToString(), note.At)).ToList(),
        lead.CreatedAt,
        lead.UpdatedAt);
}
