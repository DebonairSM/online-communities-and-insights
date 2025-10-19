using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCommunities.Application.Commands.Identity.CreateUser;
using OnlineCommunities.Application.Common.CQRS;
using OnlineCommunities.Application.Queries.Identity.GetUserById;

namespace OnlineCommunities.Api.Controllers;

/// <summary>
/// Example controller demonstrating CQRS pattern with Mediator
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get a user by their ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var user = await _mediator.QueryAsync(query, cancellationToken);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        return Ok(user);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new CreateUserCommand(
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber);

            var userId = await _mediator.SendAsync(command, cancellationToken);

            return CreatedAtAction(
                nameof(GetUser), 
                new { id = userId }, 
                new { id = userId });
        }
        catch (Application.Common.Exceptions.ValidationException ex)
        {
            return BadRequest(new 
            { 
                message = "Validation failed", 
                errors = ex.Errors 
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// Request model for creating a user
/// </summary>
public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber = null);

