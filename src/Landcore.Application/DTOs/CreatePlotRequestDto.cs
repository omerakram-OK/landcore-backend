namespace Landcore.Application.DTOs;

public sealed record CreatePlotRequestDto(
    string PlotNumber,
    string BlockId,
    decimal Size,
    string SizeUnit,
    string Category,
    decimal BasePrice,
    decimal AnnualMaintenanceCharge,
    List<PlotChargeDto>? Charges,
    List<string>? OwnerClientIds);
