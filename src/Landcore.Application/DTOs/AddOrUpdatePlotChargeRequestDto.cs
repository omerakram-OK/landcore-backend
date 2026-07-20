namespace Landcore.Application.DTOs;

public sealed record AddOrUpdatePlotChargeRequestDto(
    string ChargeType,
    decimal Amount);
