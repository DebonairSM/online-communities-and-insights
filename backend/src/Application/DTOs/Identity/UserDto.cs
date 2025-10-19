namespace OnlineCommunities.Application.DTOs.Identity;

/// <summary>
/// Data transfer object for user information
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
}

