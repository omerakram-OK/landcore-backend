namespace Landcore.Application.DTOs;

public sealed record PlotResponseDto(
    string Id,
    string AdminId,
    string PlotNumber,
    string BlockId,
    string SocietyId,
    decimal Size,
    string SizeUnit,
    string Category,
    decimal BasePrice,
    List<PlotChargeDto> Charges,
    decimal AnnualMaintenanceCharge,
    string Status,
    string PossessionStatus,
    List<string> OwnerClientIds,
    List<PlotHistoryLogEntryDto> HistoryLog,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt);
