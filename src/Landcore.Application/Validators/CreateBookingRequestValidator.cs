using FluentValidation;
using Landcore.Application.DTOs;
using MongoDB.Bson;

namespace Landcore.Application.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequestDto>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.PlotId)
            .NotEmpty().WithMessage("PlotId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("PlotId must be a valid identifier.");

        RuleFor(x => x.ClientId)
            .NotEmpty().WithMessage("ClientId is required.")
            .Must(id => ObjectId.TryParse(id, out _)).WithMessage("ClientId must be a valid identifier.");

        RuleFor(x => x.LeadId)
            .Must(id => ObjectId.TryParse(id, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.LeadId))
            .WithMessage("LeadId must be a valid identifier.");

        RuleFor(x => x.AgentId)
            .Must(id => ObjectId.TryParse(id, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.AgentId))
            .WithMessage("AgentId must be a valid identifier.");

        RuleFor(x => x.TokenAmount)
            .GreaterThan(0).WithMessage("TokenAmount must be greater than zero.");

        RuleFor(x => x.ExpiryDate)
            .GreaterThan(DateTime.UtcNow)
            .When(x => x.ExpiryDate.HasValue)
            .WithMessage("ExpiryDate must be in the future.");
    }
}
