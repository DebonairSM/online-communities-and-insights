using OnlineCommunities.Application.Common.CQRS;

namespace OnlineCommunities.Application.Commands.Identity.CreateUser;

/// <summary>
/// Command to create a new user
/// </summary>
public record CreateUserCommand(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null
) : ICommand<Guid>;

