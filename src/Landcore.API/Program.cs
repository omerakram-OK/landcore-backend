using System.Text;
using FluentValidation;
using Landcore.API.Middleware;
using Landcore.Application.Configuration;
using Landcore.Application.DTOs;
using Landcore.Application.Interfaces;
using Landcore.Application.Services;
using Landcore.Application.Validators;
using Landcore.Infrastructure.Auth;
using Landcore.Infrastructure.Configuration;
using Landcore.Domain.Entities;
using Landcore.Infrastructure.Persistence;
using Landcore.Infrastructure.Persistence.Repositories;
using MongoDB.Driver;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var mongoSettings = builder.Configuration.GetSection("MongoDb").Get<MongoDbSettings>() ?? new MongoDbSettings();
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();

var repossessionSettings = builder.Configuration.GetSection("Repossession").Get<RepossessionSettings>() ?? new RepossessionSettings();

var smtpSettings = builder.Configuration.GetSection("Smtp").Get<SmtpSettings>() ?? new SmtpSettings();
var notificationSettings = builder.Configuration.GetSection("Notifications").Get<NotificationSettings>() ?? new NotificationSettings();

if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is not configured. Set it via user-secrets (dev) or the Jwt__SigningKey " +
        "environment variable (staging/production) — see Landcore.Infrastructure/Configuration/JwtSettings.cs.");
}

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(repossessionSettings);
builder.Services.AddSingleton(smtpSettings);
builder.Services.AddSingleton(notificationSettings);

builder.Services.AddSingleton(new MongoDbContext(mongoSettings));
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IDesignationRepository, DesignationRepository>();

builder.Services.AddScoped<ISocietyRepository, SocietyRepository>();

builder.Services.AddScoped<IBlockRepository, BlockRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<ILeadRepository, LeadRepository>();
builder.Services.AddScoped<IClientRepository, ClientRepository>();

builder.Services.AddScoped<IPlotRepository, PlotRepository>();

builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IInstallmentPlanRepository, InstallmentPlanRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IChequeRepository, ChequeRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();

builder.Services.AddScoped<IBankAccountRepository, BankAccountRepository>();

builder.Services.AddScoped<IApprovalRequestRepository, ApprovalRequestRepository>();
builder.Services.AddScoped<IRefundRecordRepository, RefundRecordRepository>();

builder.Services.AddScoped<IGeneratedDocumentRepository, GeneratedDocumentRepository>();

builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddSingleton<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();

builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IDesignationService, DesignationService>();

builder.Services.AddScoped<ISocietyService, SocietyService>();

builder.Services.AddScoped<IBlockService, BlockService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IClientService, ClientService>();

builder.Services.AddScoped<IPlotService, PlotService>();

builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IInstallmentPlanService, InstallmentPlanService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IChequeService, ChequeService>();
builder.Services.AddScoped<IReceiptService, ReceiptService>();

builder.Services.AddScoped<IBankAccountService, BankAccountService>();

builder.Services.AddScoped<IRepossessionService, RepossessionService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();

builder.Services.AddScoped<IDocumentGenerationService, Landcore.Infrastructure.Documents.DocumentGenerationService>();

builder.Services.AddScoped<IReportService, ReportService>();

builder.Services.AddScoped<IEmailService, Landcore.Infrastructure.Email.EmailService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<IValidator<CreateAdminRequestDto>, CreateAdminRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateAdminRequestDto>, UpdateAdminRequestValidator>();
builder.Services.AddScoped<IValidator<CreateSubscriptionRequestDto>, CreateSubscriptionRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateSubscriptionRequestDto>, UpdateSubscriptionRequestValidator>();
builder.Services.AddScoped<IValidator<CreateEmployeeRequestDto>, CreateEmployeeRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateEmployeeRequestDto>, UpdateEmployeeRequestValidator>();
builder.Services.AddScoped<IValidator<CreateDesignationRequestDto>, CreateDesignationRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateDesignationRequestDto>, UpdateDesignationRequestValidator>();

builder.Services.AddScoped<IValidator<CreateSocietyRequestDto>, CreateSocietyRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateSocietyRequestDto>, UpdateSocietyRequestValidator>();
builder.Services.AddScoped<IValidator<CreateBlockRequestDto>, CreateBlockRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateBlockRequestDto>, UpdateBlockRequestValidator>();
builder.Services.AddScoped<IValidator<CreateAgentRequestDto>, CreateAgentRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateAgentRequestDto>, UpdateAgentRequestValidator>();
builder.Services.AddScoped<IValidator<CreateLeadRequestDto>, CreateLeadRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateLeadRequestDto>, UpdateLeadRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateLeadStatusRequestDto>, UpdateLeadStatusRequestValidator>();
builder.Services.AddScoped<IValidator<AppendFollowUpNoteRequestDto>, AppendFollowUpNoteRequestValidator>();
builder.Services.AddScoped<IValidator<CreateClientRequestDto>, CreateClientRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateClientRequestDto>, UpdateClientRequestValidator>();

builder.Services.AddScoped<IValidator<CreatePlotRequestDto>, CreatePlotRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePlotRequestDto>, UpdatePlotRequestValidator>();
builder.Services.AddScoped<IValidator<AddOrUpdatePlotChargeRequestDto>, AddOrUpdatePlotChargeRequestValidator>();
builder.Services.AddScoped<IValidator<SetAnnualMaintenanceChargeRequestDto>, SetAnnualMaintenanceChargeRequestValidator>();
builder.Services.AddScoped<IValidator<ChangePlotStatusRequestDto>, ChangePlotStatusRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePlotPossessionStatusRequestDto>, UpdatePlotPossessionStatusRequestValidator>();
builder.Services.AddScoped<IValidator<SplitPlotRequestDto>, SplitPlotRequestValidator>();
builder.Services.AddScoped<IValidator<MergePlotsRequestDto>, MergePlotsRequestValidator>();

builder.Services.AddScoped<IValidator<CreateBookingRequestDto>, CreateBookingRequestValidator>();
builder.Services.AddScoped<IValidator<BookingActionRequestDto>, BookingActionRequestValidator>();
builder.Services.AddScoped<IValidator<CreateInstallmentPlanRequestDto>, CreateInstallmentPlanRequestValidator>();
builder.Services.AddScoped<IValidator<RecordPaymentRequestDto>, RecordPaymentRequestValidator>();
builder.Services.AddScoped<IValidator<BounceChequeRequestDto>, BounceChequeRequestValidator>();

builder.Services.AddScoped<IValidator<CreateBankAccountRequestDto>, CreateBankAccountRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateBankAccountRequestDto>, UpdateBankAccountRequestValidator>();

builder.Services.AddScoped<IValidator<ApplyDiscountRequestDto>, ApplyDiscountRequestValidator>();
builder.Services.AddScoped<IValidator<RecordLatePaymentRequestDto>, RecordLatePaymentRequestValidator>();
builder.Services.AddScoped<IValidator<ProposeApprovalRequestDto>, ProposeApprovalRequestValidator>();
builder.Services.AddScoped<IValidator<RejectApprovalRequestDto>, RejectApprovalRequestValidator>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, StandardAuthorizationResultHandler>();

builder.Services.AddControllers();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var details = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var payload = new
        {
            success = false,
            error = new
            {
                code = "VALIDATION_ERROR",
                message = "One or more fields are invalid.",
                details,
            },
        };

        return new BadRequestObjectResult(payload);
    };
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT bearer token from POST /api/auth/login. Paste just the token — Swagger UI adds the 'Bearer ' prefix.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<PermissionAuthorizationMiddleware>();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await mongoContext.EnsureIndexesAsync();
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to ensure MongoDB indexes at startup.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var existingCount = await mongoContext.SuperMen.CountDocumentsAsync(FilterDefinition<SuperMan>.Empty);
        if (existingCount == 0)
        {
            var seedEmail = builder.Configuration["SuperManSeed:Email"] ?? "owner@landcore.local";
            var seedPassword = builder.Configuration["SuperManSeed:Password"] ?? "ChangeMe123!";

            var seededSuperMan = new SuperMan
            {
                FullName = "Platform Owner",
                Email = seedEmail,
                PasswordHash = passwordHasher.Hash(seedPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false,
            };
            await mongoContext.SuperMen.InsertOneAsync(seededSuperMan);

            startupLogger.LogWarning(
                "No SuperMan account existed — seeded the first one with email '{Email}'. " +
                "This is a default/seeded credential: log in once and treat it as sensitive, and " +
                "set SuperManSeed:Email / SuperManSeed:Password in configuration before running " +
                "against a real environment if you don't want the built-in default.",
                seedEmail);
        }
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to check/seed the initial SuperMan account.");
    }
}

app.Run();
