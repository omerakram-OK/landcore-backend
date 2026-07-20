namespace Landcore.Application.DTOs;

public sealed record GenerateDocumentRequestDto(
    string PlotId,
    string DocumentType);
