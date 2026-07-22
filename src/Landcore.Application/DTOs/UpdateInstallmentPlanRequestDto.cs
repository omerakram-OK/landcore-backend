namespace Landcore.Application.DTOs;

public sealed record UpdateInstallmentPlanRequestDto(
    List<UpdateInstallmentItemDto> Installments);
