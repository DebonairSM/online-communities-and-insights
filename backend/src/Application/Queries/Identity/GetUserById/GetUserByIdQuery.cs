using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Application.DTOs.Identity;

namespace OnlineCommunities.Application.Queries.Identity.GetUserById;

/// <summary>
/// Query to get a user by their ID
/// </summary>
public record GetUserByIdQuery(Guid UserId) : IQuery<UserDto?>;

