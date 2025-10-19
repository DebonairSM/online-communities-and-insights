using System.ComponentModel.DataAnnotations;

namespace OnlineCommunities.Api.Configuration;

public sealed class EntraConnectorOptions
{
    public const string SectionName = "EntraConnector";

    [Required(ErrorMessage = "EntraConnector:Username is required")]
    [MinLength(3, ErrorMessage = "EntraConnector:Username must be at least 3 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "EntraConnector:Password is required")]
    [MinLength(16, ErrorMessage = "EntraConnector:Password must be at least 16 characters for security")]
    public string Password { get; set; } = string.Empty;
}
