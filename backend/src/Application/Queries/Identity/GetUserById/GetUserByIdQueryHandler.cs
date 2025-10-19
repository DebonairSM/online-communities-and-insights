using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Application.DTOs.Identity;
using OnlineCommunities.Core.Interfaces;

namespace OnlineCommunities.Application.Queries.Identity.GetUserById;

/// <summary>
/// Handler for getting a user by their ID
/// </summary>
public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserDto?> HandleAsync(GetUserByIdQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(query.UserId);
        
        if (user == null)
        {
            return null;
        }

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified
        };
    }
}

