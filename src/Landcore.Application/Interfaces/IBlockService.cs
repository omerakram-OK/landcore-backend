using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IBlockService
{
    Task<BlockResponseDto> CreateAsync(string adminId, CreateBlockRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BlockResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<BlockResponseDto> GetByIdAsync(string adminId, string blockId, CancellationToken cancellationToken = default);

    Task<BlockResponseDto> UpdateAsync(string adminId, string blockId, UpdateBlockRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string blockId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<BulkImportBlocksResultDto> BulkImportAsync(string adminId, string fileContent, string performedByUserId, CancellationToken cancellationToken = default);
}
