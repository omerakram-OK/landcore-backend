using System.Globalization;
using FluentValidation;
using Landcore.Application.Common;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class PlotService : IPlotService
{
    private readonly IPlotRepository _plotRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly ISocietyRepository _societyRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IValidator<CreatePlotRequestDto> _createValidator;
    private readonly IValidator<UpdatePlotRequestDto> _updateValidator;
    private readonly IValidator<AddOrUpdatePlotChargeRequestDto> _addOrUpdateChargeValidator;
    private readonly IValidator<SetAnnualMaintenanceChargeRequestDto> _setMaintenanceChargeValidator;
    private readonly IValidator<ChangePlotStatusRequestDto> _changeStatusValidator;
    private readonly IValidator<UpdatePlotPossessionStatusRequestDto> _updatePossessionStatusValidator;
    private readonly IValidator<SplitPlotRequestDto> _splitValidator;
    private readonly IValidator<MergePlotsRequestDto> _mergeValidator;
    private readonly IAuditLogger _auditLogger;

    private static readonly Dictionary<PlotStatus, PlotStatus[]> AllowedStatusTransitions = new()
    {
        [PlotStatus.Available] = [PlotStatus.Booked],

        [PlotStatus.Booked] = [PlotStatus.Available, PlotStatus.Sold],

        [PlotStatus.Sold] = [PlotStatus.Overdue],

        [PlotStatus.Overdue] = [PlotStatus.Sold, PlotStatus.Repossessed],

        [PlotStatus.Repossessed] = [PlotStatus.Available],
    };

    private static readonly HashSet<PlotStatus> AllowedSourceStatusesForMergeSplit =
    [
        PlotStatus.Available,
        PlotStatus.Overdue,
        PlotStatus.Repossessed,
    ];

    public PlotService(
        IPlotRepository plotRepository,
        IBlockRepository blockRepository,
        ISocietyRepository societyRepository,
        IClientRepository clientRepository,
        IValidator<CreatePlotRequestDto> createValidator,
        IValidator<UpdatePlotRequestDto> updateValidator,
        IValidator<AddOrUpdatePlotChargeRequestDto> addOrUpdateChargeValidator,
        IValidator<SetAnnualMaintenanceChargeRequestDto> setMaintenanceChargeValidator,
        IValidator<ChangePlotStatusRequestDto> changeStatusValidator,
        IValidator<UpdatePlotPossessionStatusRequestDto> updatePossessionStatusValidator,
        IValidator<SplitPlotRequestDto> splitValidator,
        IValidator<MergePlotsRequestDto> mergeValidator,
        IAuditLogger auditLogger)
    {
        _plotRepository = plotRepository;
        _blockRepository = blockRepository;
        _societyRepository = societyRepository;
        _clientRepository = clientRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addOrUpdateChargeValidator = addOrUpdateChargeValidator;
        _setMaintenanceChargeValidator = setMaintenanceChargeValidator;
        _changeStatusValidator = changeStatusValidator;
        _updatePossessionStatusValidator = updatePossessionStatusValidator;
        _splitValidator = splitValidator;
        _mergeValidator = mergeValidator;
        _auditLogger = auditLogger;
    }

    public async Task<PlotResponseDto> CreateAsync(string adminId, CreatePlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var block = await ResolveBlockAsync(adminObjectId, request.BlockId, "BlockId", cancellationToken);
        var plotNumber = request.PlotNumber.Trim();

        await EnsurePlotNumberAvailableAsync(adminObjectId, block.Id, plotNumber, excludingPlotId: null, cancellationToken);

        var ownerClientIds = await ResolveOwnerClientIdsAsync(adminObjectId, request.OwnerClientIds, cancellationToken);
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var plot = new Plot
        {
            AdminId = adminObjectId,
            PlotNumber = plotNumber,
            BlockId = block.Id,
            SocietyId = block.SocietyId,
            Size = request.Size,
            SizeUnit = ParseEnum<PlotSizeUnit>(request.SizeUnit, "SizeUnit"),
            Category = ParseEnum<PlotCategory>(request.Category, "Category"),
            BasePrice = (Decimal128)request.BasePrice,
            Charges = ResolveCharges(request.Charges),
            AnnualMaintenanceCharge = (Decimal128)request.AnnualMaintenanceCharge,
            Status = PlotStatus.Available,
            PossessionStatus = PossessionStatus.NotHandedOver,
            OwnerClientIds = ownerClientIds,
            HistoryLog =
            [
                new Plot.HistoryLogEntry
                {
                    Event = "Created",
                    Details = $"Plot '{plotNumber}' created.",
                    At = now,
                    By = performedBy,
                },
            ],
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _plotRepository.CreateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "PlotCreated", "Plot", plot.Id.ToString(), adminId);

        return MapToDto(plot);
    }

    public async Task<IReadOnlyList<PlotResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plots = await _plotRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return plots.Select(MapToDto).ToList();
    }

    public async Task<PlotResponseDto> GetByIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);
        return MapToDto(plot);
    }

    public async Task<PlotResponseDto> UpdateAsync(string adminId, string plotId, UpdatePlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var block = await ResolveBlockAsync(adminObjectId, request.BlockId, "BlockId", cancellationToken);
        var plotNumber = request.PlotNumber.Trim();

        if (block.Id != plot.BlockId || !string.Equals(plotNumber, plot.PlotNumber, StringComparison.OrdinalIgnoreCase))
        {
            await EnsurePlotNumberAvailableAsync(adminObjectId, block.Id, plotNumber, excludingPlotId: plot.Id, cancellationToken);
        }

        var ownerClientIds = await ResolveOwnerClientIdsAsync(adminObjectId, request.OwnerClientIds, cancellationToken);

        plot.PlotNumber = plotNumber;
        plot.BlockId = block.Id;
        plot.SocietyId = block.SocietyId;
        plot.Size = request.Size;
        plot.SizeUnit = ParseEnum<PlotSizeUnit>(request.SizeUnit, "SizeUnit");
        plot.Category = ParseEnum<PlotCategory>(request.Category, "Category");
        plot.BasePrice = (Decimal128)request.BasePrice;
        plot.OwnerClientIds = ownerClientIds;
        plot.UpdatedAt = DateTime.UtcNow;
        plot.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "PlotUpdated", "Plot", plot.Id.ToString(), adminId);

        return MapToDto(plot);
    }

    public async Task DeleteAsync(string adminId, string plotId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _plotRepository.SoftDeleteAsync(adminObjectId, plot.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"Plot '{plotId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "PlotDeleted", "Plot", plot.Id.ToString(), adminId);
    }

    public async Task<PlotResponseDto> AddOrUpdateChargeAsync(string adminId, string plotId, AddOrUpdatePlotChargeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_addOrUpdateChargeValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var chargeType = request.ChargeType.Trim();
        var existing = plot.Charges.FirstOrDefault(charge => string.Equals(charge.ChargeType, chargeType, StringComparison.OrdinalIgnoreCase));
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        string eventName;

        if (existing is not null)
        {
            existing.Amount = (Decimal128)request.Amount;
            eventName = "PlotChargeUpdated";
        }
        else
        {
            plot.Charges.Add(new PlotCharge { ChargeType = chargeType, Amount = (Decimal128)request.Amount });
            eventName = "PlotChargeAdded";
        }

        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = eventName,
            Details = $"{chargeType}: {request.Amount:0.##}",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, eventName, "Plot", plot.Id.ToString(), adminId, new { ChargeType = chargeType, request.Amount });

        return MapToDto(plot);
    }

    public async Task<PlotResponseDto> RemoveChargeAsync(string adminId, string plotId, string chargeType, string performedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(chargeType))
        {
            throw new ValidationAppException(
                "ChargeType is required.",
                new Dictionary<string, string[]> { ["ChargeType"] = ["ChargeType is required."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var normalized = chargeType.Trim();
        var existing = plot.Charges.FirstOrDefault(charge => string.Equals(charge.ChargeType, normalized, StringComparison.OrdinalIgnoreCase));
        if (existing is null)
        {
            throw new NotFoundAppException($"Charge type '{chargeType}' was not found on Plot '{plotId}'.");
        }

        plot.Charges.Remove(existing);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "PlotChargeRemoved",
            Details = normalized,
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "PlotChargeRemoved", "Plot", plot.Id.ToString(), adminId, new { ChargeType = normalized });

        return MapToDto(plot);
    }

    public async Task<PlotResponseDto> SetAnnualMaintenanceChargeAsync(string adminId, string plotId, SetAnnualMaintenanceChargeRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_setMaintenanceChargeValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        plot.AnnualMaintenanceCharge = (Decimal128)request.Amount;
        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "AnnualMaintenanceChargeUpdated",
            Details = $"AnnualMaintenanceCharge set to {request.Amount:0.##}.",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "AnnualMaintenanceChargeUpdated", "Plot", plot.Id.ToString(), adminId, new { request.Amount });

        return MapToDto(plot);
    }

    public async Task<PlotResponseDto> ChangeStatusAsync(string adminId, string plotId, ChangePlotStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_changeStatusValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var newStatus = ParseEnum<PlotStatus>(request.Status, "Status");

        if (newStatus == plot.Status)
        {
            throw new ValidationAppException(
                $"Plot is already in status '{newStatus}'.",
                new Dictionary<string, string[]> { ["Status"] = [$"Plot is already in status '{newStatus}'."] });
        }

        if (!AllowedStatusTransitions.TryGetValue(plot.Status, out var allowedTargets) || !allowedTargets.Contains(newStatus))
        {
            throw new ValidationAppException(
                $"Cannot change Plot status from '{plot.Status}' to '{newStatus}'.",
                new Dictionary<string, string[]> { ["Status"] = [$"Cannot change Plot status from '{plot.Status}' to '{newStatus}'."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var previousStatus = plot.Status;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        plot.Status = newStatus;
        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "StatusChanged",
            Details = notes is null
                ? $"{previousStatus} -> {newStatus}"
                : $"{previousStatus} -> {newStatus}: {notes}",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(
            performedByUserId,
            "PlotStatusChanged",
            "Plot",
            plot.Id.ToString(),
            adminId,
            new { From = previousStatus.ToString(), To = newStatus.ToString() });

        return MapToDto(plot);
    }

    public async Task<PlotResponseDto> UpdatePossessionStatusAsync(string adminId, string plotId, UpdatePlotPossessionStatusRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updatePossessionStatusValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var plot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        var newStatus = ParseEnum<PossessionStatus>(request.PossessionStatus, "PossessionStatus");

        if (newStatus == plot.PossessionStatus)
        {
            throw new ValidationAppException(
                $"Plot possession status is already '{newStatus}'.",
                new Dictionary<string, string[]> { ["PossessionStatus"] = [$"Plot possession status is already '{newStatus}'."] });
        }

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var previousStatus = plot.PossessionStatus;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        plot.PossessionStatus = newStatus;
        plot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "PossessionStatusChanged",
            Details = notes is null
                ? $"{previousStatus} -> {newStatus}"
                : $"{previousStatus} -> {newStatus}: {notes}",
            At = now,
            By = performedBy,
        });
        plot.UpdatedAt = now;
        plot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(plot, cancellationToken);

        _auditLogger.LogAction(
            performedByUserId,
            "PlotPossessionStatusChanged",
            "Plot",
            plot.Id.ToString(),
            adminId,
            new { From = previousStatus.ToString(), To = newStatus.ToString() });

        return MapToDto(plot);
    }

    public async Task<IReadOnlyList<PlotResponseDto>> SplitAsync(string adminId, string plotId, SplitPlotRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_splitValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var sourcePlot = await LoadPlotOrThrowAsync(adminObjectId, plotId, cancellationToken);

        EnsureEligibleForMergeSplit(sourcePlot);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();

        var newPlots = new List<Plot>(request.NewPlots.Count);
        var requestedKeys = new HashSet<(ObjectId BlockId, string PlotNumber)>();

        foreach (var definition in request.NewPlots)
        {
            ObjectId blockId;
            ObjectId societyId;
            if (string.IsNullOrWhiteSpace(definition.BlockId))
            {
                blockId = sourcePlot.BlockId;
                societyId = sourcePlot.SocietyId;
            }
            else
            {
                var block = await ResolveBlockAsync(adminObjectId, definition.BlockId, "NewPlots.BlockId", cancellationToken);
                blockId = block.Id;
                societyId = block.SocietyId;
            }

            var plotNumber = definition.PlotNumber.Trim();
            var key = (blockId, plotNumber.ToUpperInvariant());
            if (!requestedKeys.Add(key))
            {
                throw new ValidationAppException(
                    $"Duplicate PlotNumber '{plotNumber}' among the new Plot definitions for this Block.",
                    new Dictionary<string, string[]> { ["NewPlots"] = [$"Duplicate PlotNumber '{plotNumber}' among the new Plot definitions for this Block."] });
            }

            await EnsurePlotNumberAvailableAsync(adminObjectId, blockId, plotNumber, excludingPlotId: null, cancellationToken);

            var ownerClientIds = definition.OwnerClientIds is null
                ? sourcePlot.OwnerClientIds.ToList()
                : await ResolveOwnerClientIdsAsync(adminObjectId, definition.OwnerClientIds, cancellationToken);

            newPlots.Add(new Plot
            {
                AdminId = adminObjectId,
                PlotNumber = plotNumber,
                BlockId = blockId,
                SocietyId = societyId,
                Size = definition.Size,
                SizeUnit = ParseEnum<PlotSizeUnit>(definition.SizeUnit, "NewPlots.SizeUnit"),
                Category = ParseEnum<PlotCategory>(definition.Category, "NewPlots.Category"),
                BasePrice = (Decimal128)definition.BasePrice,
                Charges = ResolveCharges(definition.Charges),
                AnnualMaintenanceCharge = (Decimal128)definition.AnnualMaintenanceCharge,
                Status = PlotStatus.Available,
                PossessionStatus = PossessionStatus.NotHandedOver,
                OwnerClientIds = ownerClientIds,
                HistoryLog =
                [
                    new Plot.HistoryLogEntry
                    {
                        Event = "SplitFrom",
                        Details = BuildDetails($"Created via split from Plot '{sourcePlot.PlotNumber}' ({sourcePlot.Id}).", notes),
                        At = now,
                        By = performedBy,
                    },
                ],
                CreatedAt = now,
                CreatedBy = performedBy,
                UpdatedAt = now,
                UpdatedBy = performedBy,
                IsDeleted = false,
            });
        }

        foreach (var newPlot in newPlots)
        {
            await _plotRepository.CreateAsync(newPlot, cancellationToken);
        }

        var newPlotsDescription = string.Join(", ", newPlots.Select(plot => $"'{plot.PlotNumber}' ({plot.Id})"));

        sourcePlot.IsDeleted = true;
        sourcePlot.DeletedAt = now;
        sourcePlot.DeletedBy = performedBy;
        sourcePlot.HistoryLog.Add(new Plot.HistoryLogEntry
        {
            Event = "SplitInto",
            Details = BuildDetails($"Split into {newPlots.Count} new Plot(s): {newPlotsDescription}.", notes),
            At = now,
            By = performedBy,
        });
        sourcePlot.UpdatedAt = now;
        sourcePlot.UpdatedBy = performedBy;

        await _plotRepository.UpdateAsync(sourcePlot, cancellationToken);

        _auditLogger.LogAction(
            performedByUserId,
            "PlotSplit",
            "Plot",
            sourcePlot.Id.ToString(),
            adminId,
            new { NewPlotIds = newPlots.Select(plot => plot.Id.ToString()).ToList() });

        return newPlots.Select(MapToDto).ToList();
    }

    public async Task<PlotResponseDto> MergeAsync(string adminId, MergePlotsRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_mergeValidator, request, cancellationToken);

        if (request.NewPlot is null)
        {
            throw new ValidationAppException(
                "NewPlot is required.",
                new Dictionary<string, string[]> { ["NewPlot"] = ["NewPlot is required."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var sourcePlots = new List<Plot>(request.SourcePlotIds.Count);

        foreach (var rawId in request.SourcePlotIds)
        {
            if (!seenIds.Add(rawId))
            {
                throw new ValidationAppException(
                    "SourcePlotIds contains a duplicate Plot id.",
                    new Dictionary<string, string[]> { ["SourcePlotIds"] = ["SourcePlotIds contains a duplicate Plot id."] });
            }

            var sourcePlot = await LoadPlotOrThrowAsync(adminObjectId, rawId, cancellationToken);
            EnsureEligibleForMergeSplit(sourcePlot);
            sourcePlots.Add(sourcePlot);
        }

        var commonBlockId = sourcePlots[0].BlockId;
        var commonSocietyId = sourcePlots[0].SocietyId;
        if (sourcePlots.Any(plot => plot.BlockId != commonBlockId))
        {
            throw new ValidationAppException(
                "All Plots being merged must belong to the same Block.",
                new Dictionary<string, string[]> { ["SourcePlotIds"] = ["All Plots being merged must belong to the same Block."] });
        }

        var blockId = string.IsNullOrWhiteSpace(request.NewPlot.BlockId)
            ? commonBlockId
            : (await ResolveBlockAsync(adminObjectId, request.NewPlot.BlockId, "NewPlot.BlockId", cancellationToken)).Id;

        if (blockId != commonBlockId)
        {
            throw new ValidationAppException(
                "The new Plot's BlockId must match the source Plots' Block.",
                new Dictionary<string, string[]> { ["NewPlot"] = ["The new Plot's BlockId must match the source Plots' Block."] });
        }

        var plotNumber = request.NewPlot.PlotNumber.Trim();
        await EnsurePlotNumberAvailableAsync(adminObjectId, blockId, plotNumber, excludingPlotId: null, cancellationToken);

        var ownerClientIds = request.NewPlot.OwnerClientIds is null
            ? sourcePlots.SelectMany(plot => plot.OwnerClientIds).Distinct().ToList()
            : await ResolveOwnerClientIdsAsync(adminObjectId, request.NewPlot.OwnerClientIds, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        var sourceDescription = string.Join(", ", sourcePlots.Select(plot => $"'{plot.PlotNumber}' ({plot.Id})"));

        var mergedPlot = new Plot
        {
            AdminId = adminObjectId,
            PlotNumber = plotNumber,
            BlockId = blockId,
            SocietyId = commonSocietyId,
            Size = request.NewPlot.Size,
            SizeUnit = ParseEnum<PlotSizeUnit>(request.NewPlot.SizeUnit, "NewPlot.SizeUnit"),
            Category = ParseEnum<PlotCategory>(request.NewPlot.Category, "NewPlot.Category"),
            BasePrice = (Decimal128)request.NewPlot.BasePrice,
            Charges = ResolveCharges(request.NewPlot.Charges),
            AnnualMaintenanceCharge = (Decimal128)request.NewPlot.AnnualMaintenanceCharge,
            Status = PlotStatus.Available,
            PossessionStatus = PossessionStatus.NotHandedOver,
            OwnerClientIds = ownerClientIds,
            HistoryLog =
            [
                new Plot.HistoryLogEntry
                {
                    Event = "MergedFrom",
                    Details = BuildDetails($"Created via merge of Plots: {sourceDescription}.", notes),
                    At = now,
                    By = performedBy,
                },
            ],
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _plotRepository.CreateAsync(mergedPlot, cancellationToken);

        foreach (var sourcePlot in sourcePlots)
        {
            sourcePlot.IsDeleted = true;
            sourcePlot.DeletedAt = now;
            sourcePlot.DeletedBy = performedBy;
            sourcePlot.HistoryLog.Add(new Plot.HistoryLogEntry
            {
                Event = "MergedInto",
                Details = BuildDetails($"Merged into new Plot '{mergedPlot.PlotNumber}' ({mergedPlot.Id}).", notes),
                At = now,
                By = performedBy,
            });
            sourcePlot.UpdatedAt = now;
            sourcePlot.UpdatedBy = performedBy;

            await _plotRepository.UpdateAsync(sourcePlot, cancellationToken);
        }

        _auditLogger.LogAction(
            performedByUserId,
            "PlotMerged",
            "Plot",
            mergedPlot.Id.ToString(),
            adminId,
            new { SourcePlotIds = sourcePlots.Select(plot => plot.Id.ToString()).ToList() });

        return MapToDto(mergedPlot);
    }

    public async Task<BulkImportPlotsResultDto> BulkImportAsync(string adminId, string fileContent, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");

        var rows = ImportFileParser.ParseRows(fileContent);
        var results = new List<BulkImportPlotRowResultDto>(rows.Count);
        var societyIdCache = new Dictionary<string, ObjectId>(StringComparer.OrdinalIgnoreCase);
        var blockCache = new Dictionary<(ObjectId SocietyId, string BlockName), Block>();

        for (var i = 0; i < rows.Count; i++)
        {
            var rowNumber = i + 2;
            var row = rows[i];
            row.TryGetValue("PlotNumber", out var plotNumberRaw);
            plotNumberRaw ??= string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(plotNumberRaw))
                {
                    throw new ValidationAppException("PlotNumber is required.");
                }

                var plotNumber = plotNumberRaw.Trim();

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

                row.TryGetValue("BlockName", out var blockNameRaw);
                if (string.IsNullOrWhiteSpace(blockNameRaw))
                {
                    throw new ValidationAppException("BlockName is required.");
                }

                var blockName = blockNameRaw.Trim();
                var blockCacheKey = (societyId, blockName.ToUpperInvariant());
                if (!blockCache.TryGetValue(blockCacheKey, out var block))
                {
                    block = await _blockRepository.GetByNameAsync(adminObjectId, societyId, blockName, cancellationToken);
                    if (block is null)
                    {
                        throw new ValidationAppException($"Block '{blockName}' was not found in Society '{societyName}' — create it under Blocks first, or check for a typo.");
                    }

                    blockCache[blockCacheKey] = block;
                }

                var blockId = block.Id;

                await EnsurePlotNumberAvailableAsync(adminObjectId, blockId, plotNumber, excludingPlotId: null, cancellationToken);

                row.TryGetValue("Size", out var sizeRaw);
                if (!decimal.TryParse(sizeRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var size) || size <= 0)
                {
                    throw new ValidationAppException($"Size must be a positive number (got '{sizeRaw}').");
                }

                row.TryGetValue("SizeUnit", out var sizeUnitRaw);
                var sizeUnit = ParseEnum<PlotSizeUnit>(sizeUnitRaw ?? string.Empty, "SizeUnit");

                row.TryGetValue("Category", out var categoryRaw);
                var category = ParseEnum<PlotCategory>(categoryRaw ?? string.Empty, "Category");

                row.TryGetValue("BasePrice", out var basePriceRaw);
                if (!decimal.TryParse(basePriceRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out var basePrice) || basePrice < 0)
                {
                    throw new ValidationAppException($"BasePrice must be a non-negative number (got '{basePriceRaw}').");
                }

                var annualMaintenanceCharge = 0m;
                if (row.TryGetValue("AnnualMaintenanceCharge", out var amcRaw) && !string.IsNullOrWhiteSpace(amcRaw))
                {
                    if (!decimal.TryParse(amcRaw, NumberStyles.Number, CultureInfo.InvariantCulture, out annualMaintenanceCharge) || annualMaintenanceCharge < 0)
                    {
                        throw new ValidationAppException($"AnnualMaintenanceCharge must be a non-negative number (got '{amcRaw}').");
                    }
                }

                var now = DateTime.UtcNow;
                var plot = new Plot
                {
                    AdminId = adminObjectId,
                    PlotNumber = plotNumber,
                    BlockId = blockId,
                    SocietyId = societyId,
                    Size = size,
                    SizeUnit = sizeUnit,
                    Category = category,
                    BasePrice = (Decimal128)basePrice,
                    Charges = [],
                    AnnualMaintenanceCharge = (Decimal128)annualMaintenanceCharge,
                    Status = PlotStatus.Available,
                    PossessionStatus = PossessionStatus.NotHandedOver,
                    OwnerClientIds = [],
                    HistoryLog =
                    [
                        new Plot.HistoryLogEntry
                        {
                            Event = "Created",
                            Details = $"Plot '{plotNumber}' created via bulk inventory import.",
                            At = now,
                            By = performedBy,
                        },
                    ],
                    CreatedAt = now,
                    CreatedBy = performedBy,
                    UpdatedAt = now,
                    UpdatedBy = performedBy,
                    IsDeleted = false,
                };

                await _plotRepository.CreateAsync(plot, cancellationToken);
                results.Add(new BulkImportPlotRowResultDto(rowNumber, plotNumber, true, null));
            }
            catch (AppException ex)
            {
                results.Add(new BulkImportPlotRowResultDto(rowNumber, plotNumberRaw, false, ex.Message));
            }
        }

        _auditLogger.LogAction(
            performedByUserId,
            "PlotsBulkImported",
            "Plot",
            "-",
            adminId,
            new { TotalRows = results.Count, SuccessCount = results.Count(row => row.Success), FailureCount = results.Count(row => !row.Success) });

        return new BulkImportPlotsResultDto(results.Count, results.Count(row => row.Success), results.Count(row => !row.Success), results);
    }

    private static void EnsureEligibleForMergeSplit(Plot plot)
    {
        if (!AllowedSourceStatusesForMergeSplit.Contains(plot.Status))
        {
            throw new ValidationAppException(
                $"Plot '{plot.PlotNumber}' cannot be split/merged while its status is '{plot.Status}' " +
                "(it has an active Booking/Sale) — only Available, Overdue, or Repossessed Plots may be split/merged.",
                new Dictionary<string, string[]>
                {
                    ["Status"] = [$"Plot '{plot.PlotNumber}' has status '{plot.Status}', which cannot be split/merged."],
                });
        }
    }

    private async Task EnsurePlotNumberAvailableAsync(ObjectId adminId, ObjectId blockId, string plotNumber, ObjectId? excludingPlotId, CancellationToken cancellationToken)
    {
        var existing = await _plotRepository.GetByPlotNumberAsync(adminId, blockId, plotNumber, cancellationToken);
        if (existing is not null && existing.Id != excludingPlotId)
        {
            throw new ValidationAppException(
                $"A Plot with number '{plotNumber}' already exists in this Block.",
                new Dictionary<string, string[]> { ["PlotNumber"] = [$"A Plot with number '{plotNumber}' already exists in this Block."] });
        }
    }

    private async Task<Block> ResolveBlockAsync(ObjectId adminId, string blockIdValue, string fieldName, CancellationToken cancellationToken)
    {
        var blockId = ParseObjectId(blockIdValue, fieldName);
        var block = await _blockRepository.GetByIdAsync(adminId, blockId, cancellationToken);
        if (block is null)
        {
            throw new ValidationAppException(
                "The Block was not found for this Admin.",
                new Dictionary<string, string[]> { [fieldName] = ["The Block was not found for this Admin."] });
        }

        return block;
    }

    private async Task<List<ObjectId>> ResolveOwnerClientIdsAsync(ObjectId adminId, List<string>? ownerClientIds, CancellationToken cancellationToken)
    {
        if (ownerClientIds is null || ownerClientIds.Count == 0)
        {
            return [];
        }

        var resolved = new List<ObjectId>(ownerClientIds.Count);
        foreach (var rawId in ownerClientIds)
        {
            var clientId = ParseObjectId(rawId, "OwnerClientIds");
            var client = await _clientRepository.GetByIdAsync(adminId, clientId, cancellationToken);
            if (client is null)
            {
                throw new ValidationAppException(
                    "One or more owner Clients were not found for this Admin.",
                    new Dictionary<string, string[]> { ["OwnerClientIds"] = ["One or more owner Clients were not found for this Admin."] });
            }

            resolved.Add(clientId);
        }

        return resolved;
    }

    private static List<PlotCharge> ResolveCharges(List<PlotChargeDto>? charges)
    {
        if (charges is null || charges.Count == 0)
        {
            return [];
        }

        var result = new List<PlotCharge>();
        foreach (var charge in charges)
        {
            var chargeType = charge.ChargeType.Trim();
            var existing = result.FirstOrDefault(existingCharge => string.Equals(existingCharge.ChargeType, chargeType, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                existing.Amount = (Decimal128)charge.Amount;
            }
            else
            {
                result.Add(new PlotCharge { ChargeType = chargeType, Amount = (Decimal128)charge.Amount });
            }
        }

        return result;
    }

    private static string BuildDetails(string baseText, string? notes) =>
        notes is null ? baseText : $"{baseText} {notes}";

    private async Task<Plot> LoadPlotOrThrowAsync(ObjectId adminObjectId, string plotId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(plotId, "plotId");
        var plot = await _plotRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (plot is null)
        {
            throw new NotFoundAppException($"Plot '{plotId}' was not found.");
        }

        return plot;
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

    private static TEnum ParseEnum<TEnum>(string value, string fieldName) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            throw new ValidationAppException(
                $"'{fieldName}' must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}.",
                new Dictionary<string, string[]> { [fieldName] = [$"'{value}' is not a valid {typeof(TEnum).Name}."] });
        }

        return parsed;
    }

    private static PlotResponseDto MapToDto(Plot plot) => new(
        plot.Id.ToString(),
        plot.AdminId.ToString(),
        plot.PlotNumber,
        plot.BlockId.ToString(),
        plot.SocietyId.ToString(),
        plot.Size,
        plot.SizeUnit.ToString(),
        plot.Category.ToString(),
        (decimal)plot.BasePrice,
        plot.Charges.Select(charge => new PlotChargeDto(charge.ChargeType, (decimal)charge.Amount)).ToList(),
        (decimal)plot.AnnualMaintenanceCharge,
        plot.Status.ToString(),
        plot.PossessionStatus.ToString(),
        plot.OwnerClientIds.Select(id => id.ToString()).ToList(),
        plot.HistoryLog.Select(entry => new PlotHistoryLogEntryDto(entry.Event, entry.Details, entry.At, entry.By.ToString())).ToList(),
        plot.IsDeleted,
        plot.CreatedAt,
        plot.UpdatedAt);
}
