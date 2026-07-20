using Landcore.Application.DTOs;
using Landcore.Application.Exceptions;
using Landcore.Application.Interfaces;
using Landcore.Common;
using Landcore.Domain.Entities;
using Landcore.Domain.Enums;
using MongoDB.Bson;

namespace Landcore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IAuthRepository authRepository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _authRepository = authRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationAppException("Email and password are required.");
        }

        var email = request.Email.Trim();

        var admin = await _authRepository.FindAdminByEmailAsync(email, cancellationToken);
        if (admin is not null)
        {
            return await LoginAsAdminAsync(admin, request.Password, cancellationToken);
        }

        var employee = await _authRepository.FindEmployeeByEmailAsync(email, cancellationToken);
        if (employee is not null)
        {
            return await LoginAsEmployeeAsync(employee, request.Password, cancellationToken);
        }

        var superMan = await _authRepository.FindSuperManByEmailAsync(email, cancellationToken);
        if (superMan is not null)
        {
            return LoginAsSuperMan(superMan, request.Password);
        }

        throw new AuthenticationFailedException("Invalid email or password.");
    }

    private async Task<LoginResultDto> LoginAsAdminAsync(Admin admin, string password, CancellationToken cancellationToken)
    {
        if (!_passwordHasher.Verify(admin.PasswordHash, password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        await EnsureSubscriptionActiveAsync(admin.Id, cancellationToken);

        var issued = _jwtTokenService.GenerateToken(new JwtIssueRequest(
            UserId: admin.Id.ToString(),
            Role: Constants.Roles.Admin,
            AdminId: admin.Id.ToString(),
            Permissions: Array.Empty<string>(),
            Email: admin.ContactEmail,
            FullName: admin.SocietyName));

        return new LoginResultDto(issued.Token, Constants.Roles.Admin, issued.ExpiresAtUtc);
    }

    private async Task<LoginResultDto> LoginAsEmployeeAsync(Employee employee, string password, CancellationToken cancellationToken)
    {
        if (!_passwordHasher.Verify(employee.PasswordHash, password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        await EnsureSubscriptionActiveAsync(employee.AdminId, cancellationToken);

        var designation = await _authRepository.GetDesignationByIdAsync(employee.DesignationId, cancellationToken);
        var permissions = designation?.Permissions
            .SelectMany(permission => permission.Actions.Select(action => $"{permission.Module}:{action}"))
            .ToArray() ?? Array.Empty<string>();

        var issued = _jwtTokenService.GenerateToken(new JwtIssueRequest(
            UserId: employee.Id.ToString(),
            Role: Constants.Roles.Employee,
            AdminId: employee.AdminId.ToString(),
            Permissions: permissions,
            Email: employee.Email,
            FullName: employee.FullName));

        return new LoginResultDto(issued.Token, Constants.Roles.Employee, issued.ExpiresAtUtc);
    }

    private LoginResultDto LoginAsSuperMan(SuperMan superMan, string password)
    {
        if (!_passwordHasher.Verify(superMan.PasswordHash, password))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        var issued = _jwtTokenService.GenerateToken(new JwtIssueRequest(
            UserId: superMan.Id.ToString(),
            Role: Constants.Roles.SuperMan,
            AdminId: null,
            Permissions: Array.Empty<string>(),
            Email: superMan.Email,
            FullName: superMan.FullName));

        return new LoginResultDto(issued.Token, Constants.Roles.SuperMan, issued.ExpiresAtUtc);
    }

    private async Task EnsureSubscriptionActiveAsync(ObjectId adminId, CancellationToken cancellationToken)
    {
        var subscription = await _authRepository.GetSubscriptionByAdminIdAsync(adminId, cancellationToken);

        if (subscription is null || subscription.Status is SubscriptionStatus.Overdue or SubscriptionStatus.Suspended)
        {
            throw new SubscriptionSuspendedException(
                "This society's subscription is suspended or overdue. Contact the platform administrator to reactivate access.");
        }
    }
}
