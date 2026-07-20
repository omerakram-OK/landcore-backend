namespace Landcore.Application.DTOs;

public sealed record NewPlotDefinitionDto(
    string PlotNumber,
    string? BlockId,
    decimal Size,
    string SizeUnit,
    string Category,
    decimal BasePrice,
    decimal AnnualMaintenanceCharge,
    List<PlotChargeDto>? Charges,
    List<string>? OwnerClientIds);
