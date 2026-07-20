using FluentValidation;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Application.Validators;
using Landcore.Domain.Entities;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class BankAccountService : IBankAccountService
{
    private readonly IBankAccountRepository _bankAccountRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IValidator<CreateBankAccountRequestDto> _createValidator;
    private readonly IValidator<UpdateBankAccountRequestDto> _updateValidator;
    private readonly IAuditLogger _auditLogger;

    public BankAccountService(
        IBankAccountRepository bankAccountRepository,
        IPaymentRepository paymentRepository,
        IValidator<CreateBankAccountRequestDto> createValidator,
        IValidator<UpdateBankAccountRequestDto> updateValidator,
        IAuditLogger auditLogger)
    {
        _bankAccountRepository = bankAccountRepository;
        _paymentRepository = paymentRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _auditLogger = auditLogger;
    }

    public async Task<BankAccountResponseDto> CreateAsync(string adminId, CreateBankAccountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_createValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var now = DateTime.UtcNow;

        var bankAccount = new BankAccount
        {
            AdminId = adminObjectId,
            AccountName = request.AccountName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            BankName = request.BankName.Trim(),
            CreatedAt = now,
            CreatedBy = performedBy,
            UpdatedAt = now,
            UpdatedBy = performedBy,
            IsDeleted = false,
        };

        await _bankAccountRepository.CreateAsync(bankAccount, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "BankAccountCreated", "BankAccount", bankAccount.Id.ToString(), adminId);

        return MapToDto(bankAccount);
    }

    public async Task<IReadOnlyList<BankAccountResponseDto>> GetAllAsync(string adminId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bankAccounts = await _bankAccountRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        return bankAccounts.Select(MapToDto).ToList();
    }

    public async Task<BankAccountResponseDto> GetByIdAsync(string adminId, string bankAccountId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bankAccount = await LoadBankAccountOrThrowAsync(adminObjectId, bankAccountId, cancellationToken);
        return MapToDto(bankAccount);
    }

    public async Task<BankAccountResponseDto> UpdateAsync(string adminId, string bankAccountId, UpdateBankAccountRequestDto request, string performedByUserId, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.ValidateOrThrowAsync(_updateValidator, request, cancellationToken);

        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bankAccount = await LoadBankAccountOrThrowAsync(adminObjectId, bankAccountId, cancellationToken);

        bankAccount.AccountName = request.AccountName.Trim();
        bankAccount.AccountNumber = request.AccountNumber.Trim();
        bankAccount.BankName = request.BankName.Trim();
        bankAccount.UpdatedAt = DateTime.UtcNow;
        bankAccount.UpdatedBy = ParseObjectId(performedByUserId, "performedByUserId");

        await _bankAccountRepository.UpdateAsync(bankAccount, cancellationToken);

        _auditLogger.LogAction(performedByUserId, "BankAccountUpdated", "BankAccount", bankAccount.Id.ToString(), adminId);

        return MapToDto(bankAccount);
    }

    public async Task DeleteAsync(string adminId, string bankAccountId, string performedByUserId, CancellationToken cancellationToken = default)
    {
        var adminObjectId = ParseObjectId(adminId, "adminId");
        var bankAccount = await LoadBankAccountOrThrowAsync(adminObjectId, bankAccountId, cancellationToken);

        var performedBy = ParseObjectId(performedByUserId, "performedByUserId");
        var deleted = await _bankAccountRepository.SoftDeleteAsync(adminObjectId, bankAccount.Id, performedBy, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundAppException($"BankAccount '{bankAccountId}' was not found.");
        }

        _auditLogger.LogAction(performedByUserId, "BankAccountDeleted", "BankAccount", bankAccount.Id.ToString(), adminId);
    }

    public async Task<IReadOnlyList<BankAccountReconciliationReportDto>> GetReconciliationReportAsync(string adminId, string? bankAccountId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            throw new ValidationAppException(
                "'to' must not be earlier than 'from'.",
                new Dictionary<string, string[]> { ["to"] = ["'to' must not be earlier than 'from'."] });
        }

        var adminObjectId = ParseObjectId(adminId, "adminId");

        IReadOnlyList<BankAccount> accounts;
        if (string.IsNullOrWhiteSpace(bankAccountId))
        {
            accounts = await _bankAccountRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);
        }
        else
        {
            var singleAccount = await LoadBankAccountOrThrowAsync(adminObjectId, bankAccountId, cancellationToken);
            accounts = new List<BankAccount> { singleAccount };
        }

        var allPayments = await _paymentRepository.GetAllByAdminIdAsync(adminObjectId, cancellationToken);

        var results = new List<BankAccountReconciliationReportDto>(accounts.Count);
        foreach (var account in accounts)
        {
            var matching = allPayments
                .Where(payment => payment.BankAccountId == account.Id && payment.Date >= from && payment.Date <= to)
                .ToList();

            var byMode = matching
                .GroupBy(payment => payment.Mode)
                .Select(paymentGroup => new PaymentModeBreakdownDto(paymentGroup.Key.ToString(), paymentGroup.Count(), paymentGroup.Sum(payment => (decimal)payment.Amount)))
                .OrderBy(dto => dto.Mode)
                .ToList();

            const string note = "This report shows only the Payments already recorded in Landcore against this " +
                "BankAccount for the requested date range — i.e. the 'expected/recorded from our system' side. " +
                "No bank-statement-import feature exists anywhere in this system, so a true two-sided " +
                "reconciliation (comparing these figures against actual bank deposits) is not possible yet; " +
                "these totals must be cross-checked manually against the physical bank statement.";

            results.Add(new BankAccountReconciliationReportDto(
                account.Id.ToString(),
                account.AccountName,
                account.AccountNumber,
                account.BankName,
                from,
                to,
                matching.Count,
                matching.Sum(payment => (decimal)payment.Amount),
                byMode,
                note));
        }

        return results;
    }

    private async Task<BankAccount> LoadBankAccountOrThrowAsync(ObjectId adminObjectId, string bankAccountId, CancellationToken cancellationToken)
    {
        var id = ParseObjectId(bankAccountId, "bankAccountId");
        var bankAccount = await _bankAccountRepository.GetByIdAsync(adminObjectId, id, cancellationToken);
        if (bankAccount is null)
        {
            throw new NotFoundAppException($"BankAccount '{bankAccountId}' was not found.");
        }

        return bankAccount;
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

    private static BankAccountResponseDto MapToDto(BankAccount bankAccount) => new(
        bankAccount.Id.ToString(),
        bankAccount.AdminId.ToString(),
        bankAccount.AccountName,
        bankAccount.AccountNumber,
        bankAccount.BankName,
        bankAccount.CreatedAt,
        bankAccount.UpdatedAt);
}
