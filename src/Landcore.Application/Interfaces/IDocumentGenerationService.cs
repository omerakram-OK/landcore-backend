using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IDocumentGenerationService
{
    Task<GeneratedDocumentResponseDto> GenerateAsync(string adminId, GenerateDocumentRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GeneratedDocumentResponseDto>> GetHistoryByPlotIdAsync(string adminId, string plotId, CancellationToken cancellationToken = default);

    Task<GeneratedDocumentFileDto> DownloadAsync(string adminId, string documentId, CancellationToken cancellationToken = default);
}
