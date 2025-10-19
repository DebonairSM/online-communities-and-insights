using FluentValidation;

namespace OnlineCommunities.Application.Commands.Identity.CreateUser;

/// <summary>
/// Validator for CreateUserCommand
/// </summary>
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(256).WithMessage("Email must not exceed 256 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number must not exceed 20 characters")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

