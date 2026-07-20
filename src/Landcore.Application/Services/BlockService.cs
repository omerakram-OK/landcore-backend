using System.Globalization;
using FluentValidation;
using Landcore.Application.Common;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class BlockService : IBlockService
{
    private readonly IBlockRepository _blockRepository;
    private readonly ISocietyRepository _societyRepository;
    private readonly IValidator<CreateBlockRequestDto> _createValidator;
    private readonly IValidator<UpdateBlockRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public BlockService(
        IBlockRepository blockRepository,
        ISocietyRepository societyRepository,
        IValidator<CreateBlockRequestDto> createValidator,
        IValidator<UpdateBlockRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _blockRepository = blockRepository;
        _societyRepository = societyRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<BlockResponseDto> CreateAsync(string adminId, CreateBlockRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var societyId = await ResolveSocietyIdAsync(adminObjectId, request.SocietyId, "SocietyId", cancellationToken);
        var name = request.Name.Trim();

        var existing = await _blockRepository.GetByNameAsync(adminObjectId, societyId, name, cancellationToken);
        if (existing is not null)
        {
            throw new ValidationAppException(
                "A Block with this name/number already exists in this Society.",
                new Dictionary<string, string[]> { ["Name"] = ["A Block with this name/number already exists in this Society."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var block = new Block
        {
            AdminId = adminObjectId,
            SocietyId = societyId,
            Name = name,
            Description = request.Description.Trim(),
            TotalPlots = request.TotalPlots,
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _blockRepository.CreateAsync(block, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "BlockCreated", "Block", block.Id.ToString(), adminId);

        return MapToDto(block);
    }

    public async Task<IReadOnlyList<BlockResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var blocks = await _blockRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return blocks.Select(MapToDto).ToList();
    }

    public async Task<BlockResponseDto> GetByIdAsync(string adminId, string blockId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var block = await LoadBlockOrThrowAsync(adminObjectId, blockId, cancellationToken);
        return MapToDto(block);
    }

    public async Task<BlockResponseDto> UpdateAsync(string adminId, string blockId, UpdateBlockRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var block = await LoadBlockOrThrowAsync(adminObjectId, blockId, cancellationToken);
        var societyId = await ResolveSocietyIdAsync(adminObjectId, request.SocietyId, "SocietyId", cancellationToken);

        var newName = request.Name.Trim();
        if (societyId != block.SocietyId || !string.Equals(newName, block.Name, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _blockRepository.GetByNameAsync(adminObjectId, societyId, newName, cancellationToken);
            if (existing is not null && existing.Id != block.Id)
            {
                throw new ValidationAppException(
                    "A Block with this name/number already exists in this Society.",
                    new Dictionary<string, string[]> { ["Name"] = ["A Block with this name/number already exists in this Society."] });
            }
        }

        block.SocietyId = societyId;
        block.Name = newName;
        block.Description = request.Description.Trim();
        block.TotalPlots = request.TotalPlots;
        block.UpdatedAt = DateTime.UtcNow;
        block.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _blockRepository.UpdateAsync(block, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "BlockUpdated", "Block", block.Id.ToString(), adminId);

        return MapToDto(block);
    }

    public async Task DeleteAsync(string adminId, string blockId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var block = await LoadBlockOrThrowAsync(adminObjectId, blockId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _blockRepository.SoftDeleteAsync(adminObjectId, block.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Block '{blockId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "BlockDeleted", "Block", block.Id.ToString(), adminId);
    }

    public async Task<BulkImportBlocksResultDto> BulkImportAsync(string adminId, string fileContent, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        var rows = ImportFileParser.ParseRows(fileContent);
        var results = new List<BulkImportBlockRowResultDto>(rows.Count);
        var societyIdCache = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 2;
            var row = rows[i];
            row.TryGetValue("Name", out var nameRaw);
            nameRaw ??= string.Empty;

            try
            {
                row.TryGetValue("SocietyName", out var societyNameRaw);
                if (string.IsNullOrWhiteSpace(societyNameRaw))
                {
                    throw new ValidationAppException("SocietyName is required.");
                }

                var societyName = societyNameRaw.Trim();
                if (!societyIdCache.TryGetValue(societyName, out var societyId))
                {
                    var society = await _societyRepository.GetByNameAsync(adminObjectId, societyName, cancellationToken);
                    if (society is null)
                    {
                        throw new ValidationAppException($"Society '{societyName}' was not found — create it under Societies first, or check for a typo.");
                    }

                    societyId = society.Id;
                    societyIdCache[societyName] = societyId;
                }

                if (string.IsNullOrWhiteSpace(nameRaw))
                {
                    throw new ValidationAppException("Name is required.");
                }

                var name = nameRaw.Trim();

                var existing = await _blockRepository.GetByNameAsync(adminObjectId, societyId, name, cancellationToken);
                if (existing is not null)
                {
                    throw new ValidationAppException($"A Block named '{name}' already exists in Society '{societyName}'.");
                }

                row.TryGetValue("Description", out var descriptionRaw);
                var description = (descriptionRaw ?? string.Empty).Trim();

                var totalPlots = 0;
                if (row.TryGetValue("TotalPlots", out var totalPlotsRaw) && !string.IsNullOrWhiteSpace(totalPlotsRaw))
                {
                    if (!int.TryParse(totalPlotsRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out totalPlots) || totalPlots < 0)
                    {
                        throw new ValidationAppException($"TotalPlots must be a non-negative whole number (got '{totalPlotsRaw}').");
                    }
                }

                var now = DateTime.UtcNow;
                var block = new Block
                {
                    AdminId = adminObjectId,
                    SocietyId = societyId,
                    Name = name,
                    Description = description,
                    TotalPlots = totalPlots,
                    CreatedAt = now,
                    CreatedBy = performedBy,
                    UpdatedAt = now,
                    UpdatedBy = performedBy,
                    IsDeleted = false,
                };

                await _blockRepository.CreateAsync(block, cancellationToken);
                results.Add(new BulkImportBlockRowResultDto(rowNumber, name, true, null));
            }
            catch (AppException ex)
            {
                results.Add(new BulkImportBlockRowResultDto(rowNumber, nameRaw, false, ex.Message));
            }
        }

        _auditLogger.LogAction(
            performedByUserId,
            "BlocksBulkImported",
            "Block",
            "-",
            adminId,
            new { TotalRows = results.Count, SuccessCount = results.Count(row => row.Success), FailureCount = results.Count(row => !row.Success) });

        return new BulkImportBlocksResultDto(results.Count, results.Count(row => row.Success), results.Count(row => !row.Success), results);
    }

    private async Task<Block> LoadBlockOrThrowAsync(ObjectId adminObjectId, string blockId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(blockId, "blockId");
        var block = await _blockRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (block is null)
        {
            throw new NotFoundAppException($"Block '{blockId}' was not found.");
        }

        return block;
    }

    private async Task<ObjectId> ResolveSocietyIdAsync(ObjectId adminId, string societyIdValue, string fieldName, CancellationToken cancellationToken)
    {
        var societyId = ParseObjectId(societyIdValue, fieldName);
        var society = await _societyRepository.GetByIdAsync(adminId, societyId, cancellationToken);
        if (society is null)
        {
            throw new ValidationAppException(
                "The Society was not found for this Admin.",
                new Dictionary<string, string[]> { [fieldName] = ["The Society was not found for this Admin."] });
        }

        return societyId;
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

    private static BlockResponseDto MapToDto(Block block) => new(
        block.Id.ToString(),
        block.AdminId.ToString(),
        block.SocietyId.ToString(),
        block.Name,
        block.Description,
        block.TotalPlots,
        block.CreatedAt,
        block.UpdatedAt);
}
