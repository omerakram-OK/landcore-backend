using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class ReceiptService : IReceiptService
{
    private readonly IReceiptRepository _receiptRepository;

    public ReceiptService(IReceiptRepository receiptRepository)
    {
        _receiptRepository = receiptRepository;
    }

    public async Task<IReadOnlyList<ReceiptResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var receipts = await _receiptRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return receipts.Select(MapToDto).ToList();
    }

    public async Task<ReceiptResponseDto> GetByIdAsync(string adminId, string receiptId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var id = ParseObjectId(receiptId, "receiptId");
        var receipt = await _receiptRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (receipt is null)
        {
            throw new NotFoundAppException($"Receipt '{receiptId}' was not found.");
        }

        return MapToDto(receipt);
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

    private static ReceiptResponseDto MapToDto(Receipt receipt) => new(
        receipt.Id.ToString(),
        receipt.AdminId.ToString(),
        receipt.ReceiptNumber,
        receipt.PaymentId.ToString(),
        receipt.CreatedAt);
}
