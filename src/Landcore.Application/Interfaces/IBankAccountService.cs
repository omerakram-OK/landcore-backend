using Landcore.Application.DTOs;

namespace Landcore.Application.Interfaces;

public interface IBankAccountService
{
    Task<BankAccountResponseDto> CreateAsync(string adminId, CreateBankAccountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankAccountResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default);

    Task<BankAccountResponseDto> GetByIdAsync(string adminId, string bankAccountId, CancellationToken cancellationToken = default);

    Task<BankAccountResponseDto> UpdateAsync(string adminId, string bankAccountId, UpdateBankAccountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default);

    Task DeleteAsync(string adminId, string bankAccountId, string performedByUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BankAccountReconciliationReportDto>> GetReconciliationReportAsync(string adminId, string? bankAccountId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
