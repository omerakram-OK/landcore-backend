using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class BankAccountsController : ControllerBase
{
    private readonly IBankAccountService _bankAccountService;

    public BankAccountsController(IBankAccountService bankAccountService)
    {
        _bankAccountService = bankAccountService;
    }

    [HttpPost]
    [RequirePermission("BankAccounts", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateBankAccountRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("BankAccounts", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("BankAccounts", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("BankAccounts", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateBankAccountRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("BankAccounts", "Delete")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _bankAccountService.DeleteAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(new { deleted = true }));
    }

    [HttpGet("{id}/reconciliation")]
    [RequirePermission("BankAccounts", "View")]
    public async Task<IActionResult> GetReconciliation(string id, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
    {
        var result = await _bankAccountService.GetReconciliationReportAsync(CurrentAdminId, id, from, to, cancellationToken);
        var entry = result.SingleOrDefault();
        if (entry is null)
        {
            throw new NotFoundAppException($"BankAccount '{id}' was not found.");
        }

        return Ok(Envelope(entry));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
