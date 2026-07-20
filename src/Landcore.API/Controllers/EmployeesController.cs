using Landcore.API.Middleware;
using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Landcore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = Constants.Roles.Admin + "," + Constants.Roles.Employee)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    [HttpPost]
    [RequirePermission("Employees", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.CreateAsync(CurrentAdminId, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet]
    [RequirePermission("Employees", "View")]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetAllAsync(CurrentAdminId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpGet("{id}")]
    [RequirePermission("Employees", "View")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.GetByIdAsync(CurrentAdminId, id, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpPut("{id}")]
    [RequirePermission("Employees", "Edit")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateEmployeeRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _employeeService.UpdateAsync(CurrentAdminId, id, request, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    [HttpDelete("{id}")]
    [RequirePermission("Employees", "Delete")]
    public async Task<IActionResult> Deactivate(string id, CancellationToken cancellationToken)
    {
        var result = await _employeeService.DeactivateAsync(CurrentAdminId, id, CurrentUserId, cancellationToken);
        return Ok(Envelope(result));
    }

    private string CurrentUserId => User.FindFirst(Constants.ClaimTypes.UserId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the user id claim.");

    private string CurrentAdminId => User.FindFirst(Constants.ClaimTypes.AdminId)?.Value
        ?? throw new InvalidOperationException("Authenticated request is missing the adminId claim.");

    private static object Envelope(object data) => new { success = true, data };
}
