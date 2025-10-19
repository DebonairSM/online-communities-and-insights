using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Core.Entities.Identity;
using OnlineCommunities.Core.Interfaces;

namespace OnlineCommunities.Application.Commands.Identity.CreateUser;

/// <summary>
/// Handler for creating a new user
/// </summary>
public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly IUserRepository _userRepository;

    public CreateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Guid> HandleAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        // Check if user already exists
        var existingUser = await _userRepository.GetByEmailAsync(command.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email {command.Email} already exists");
        }

        // Create the user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PhoneNumber = command.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            EmailVerified = false,
            AuthMethod = Core.Enums.AuthenticationMethod.EmailPassword
        };

        await _userRepository.AddAsync(user);

        return user.Id;
    }
}

