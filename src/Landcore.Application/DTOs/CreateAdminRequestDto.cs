namespace Landcore.Application.DTOs;

public sealed record CreateAdminRequestDto(string SocietyName, string ContactEmail, string InitialPassword);
