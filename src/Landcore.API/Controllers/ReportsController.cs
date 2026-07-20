using Landcore.API.Middleware;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("daily-collection")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Reports", "View")]
    public async Task<IActionResult> GetDailyCollection([FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var result = await _reportService.GetDailyCollectionReportAsync(CurrentAdminId, date, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("monthly-profit")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Reports", "View")]
    public async Task<IActionResult> GetMonthlyProfit([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
    {
        var result = await _reportService.GetMonthlyProfitReportAsync(CurrentAdminId, year, month, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("aging")]
    [Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
    [RequirePermission("Reports", "View")]
    public async Task<IActionResult> GetAging(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAgingReportAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("platform-summary")]
    [Authorize(Roles = Constants.Roles.SuperMan)]
    public async Task<IActionResult> GetPlatformSummary(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetPlatformSummaryReportAsync(cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
