using System.Text.RegularExpressions;

namespace OnlineCommunities.Infrastructure.Governance;

/// <summary>
/// Defines naming conventions for Azure resources.
/// Ensures consistent and compliant resource naming across the platform.
/// </summary>
public static class NamingConventions
{
    /// <summary>
    /// The maximum length for most Azure resource names.
    /// </summary>
    public const int MaxResourceNameLength = 60;

    /// <summary>
    /// The maximum length for storage account names.
    /// </summary>
    public const int MaxStorageAccountNameLength = 24;

    /// <summary>
    /// The minimum length for storage account names.
    /// </summary>
    public const int MinStorageAccountNameLength = 3;

    /// <summary>
    /// Valid environment codes.
    /// </summary>
    public static readonly string[] ValidEnvironments = { "dev", "test", "staging", "prod" };

    /// <summary>
    /// Valid component codes.
    /// </summary>
    public static readonly Dictionary<string, string> ComponentCodes = new()
    {
        { "api", "Web API services" },
        { "db", "Database services" },
        { "storage", "Storage services" },
        { "kv", "Key management" },
        { "sb", "Messaging services" },
        { "func", "Serverless functions" },
        { "aci", "Container instances" },
        { "redis", "Caching services" },
        { "vnet", "Virtual network" },
        { "subnet", "Subnet" },
        { "nsg", "Network security group" },
        { "lb", "Load balancer" },
        { "mi", "Managed identity" },
        { "app", "Application registration" },
        { "eh", "Event hub" },
        { "eg", "Event grid" },
        { "ai", "Application Insights" },
        { "law", "Log Analytics workspace" },
        { "ag", "Action group" }
    };

    /// <summary>
    /// Valid location codes.
    /// </summary>
    public static readonly Dictionary<string, string> LocationCodes = new()
    {
        { "eastus", "East US" },
        { "westus2", "West US 2" },
        { "westeurope", "West Europe" },
        { "southeastasia", "Southeast Asia" },
        { "northeurope", "North Europe" },
        { "centralus", "Central US" },
        { "southcentralus", "South Central US" },
        { "northcentralus", "North Central US" }
    };

    /// <summary>
    /// Reserved words that should not be used in resource names.
    /// </summary>
    public static readonly string[] ReservedWords = 
    {
        "azure", "microsoft", "windows", "system", "admin", "root", "test", "demo"
    };

    /// <summary>
    /// Validates an environment code.
    /// </summary>
    /// <param name="environment">The environment code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidEnvironment(string environment)
    {
        return !string.IsNullOrWhiteSpace(environment) && 
               ValidEnvironments.Contains(environment.ToLowerInvariant());
    }

    /// <summary>
    /// Validates a component code.
    /// </summary>
    /// <param name="component">The component code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidComponent(string component)
    {
        return !string.IsNullOrWhiteSpace(component) && 
               ComponentCodes.ContainsKey(component.ToLowerInvariant());
    }

    /// <summary>
    /// Validates a location code.
    /// </summary>
    /// <param name="location">The location code to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public static bool IsValidLocation(string location)
    {
        return !string.IsNullOrWhiteSpace(location) && 
               LocationCodes.ContainsKey(location.ToLowerInvariant());
    }

    /// <summary>
    /// Validates a resource name against naming conventions.
    /// </summary>
    /// <param name="name">The resource name to validate.</param>
    /// <param name="resourceType">The type of resource being validated.</param>
    /// <returns>A validation result with any errors.</returns>
    public static NamingValidationResult ValidateResourceName(string name, ResourceType resourceType)
    {
        var result = new NamingValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(name))
        {
            result.IsValid = false;
            result.Errors.Add("Resource name cannot be null or empty");
            return result;
        }

        // Check length limits
        var maxLength = resourceType == ResourceType.StorageAccount ? MaxStorageAccountNameLength : MaxResourceNameLength;
        if (name.Length > maxLength)
        {
            result.IsValid = false;
            result.Errors.Add($"Resource name exceeds maximum length of {maxLength} characters");
        }

        // Check for reserved words
        if (ReservedWords.Any(word => name.ToLowerInvariant().Contains(word)))
        {
            result.IsValid = false;
            result.Errors.Add("Resource name contains reserved words");
        }

        // Check character restrictions based on resource type
        switch (resourceType)
        {
            case ResourceType.StorageAccount:
                if (!Regex.IsMatch(name, @"^[a-z0-9]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Storage account names can only contain lowercase letters and numbers");
                }
                if (name.Length < MinStorageAccountNameLength)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Storage account name must be at least {MinStorageAccountNameLength} characters long");
                }
                break;

            case ResourceType.KeyVault:
                if (!Regex.IsMatch(name, @"^[a-zA-Z0-9-]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Key Vault names can only contain letters, numbers, and hyphens");
                }
                break;

            case ResourceType.AppService:
                if (!Regex.IsMatch(name, @"^[a-zA-Z0-9-]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add("App Service names can only contain letters, numbers, and hyphens");
                }
                break;

            default:
                if (!Regex.IsMatch(name, @"^[a-zA-Z0-9-_]+$"))
                {
                    result.IsValid = false;
                    result.Errors.Add("Resource names can only contain letters, numbers, hyphens, and underscores");
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Sanitizes a name to comply with naming conventions.
    /// </summary>
    /// <param name="name">The name to sanitize.</param>
    /// <param name="resourceType">The type of resource.</param>
    /// <returns>The sanitized name.</returns>
    public static string SanitizeName(string name, ResourceType resourceType)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var sanitized = name.ToLowerInvariant();

        // Remove reserved words
        foreach (var word in ReservedWords)
        {
            sanitized = sanitized.Replace(word, string.Empty);
        }

        // Apply character restrictions based on resource type
        switch (resourceType)
        {
            case ResourceType.StorageAccount:
                sanitized = Regex.Replace(sanitized, @"[^a-z0-9]", string.Empty);
                break;

            case ResourceType.KeyVault:
            case ResourceType.AppService:
                sanitized = Regex.Replace(sanitized, @"[^a-z0-9-]", "-");
                break;

            default:
                sanitized = Regex.Replace(sanitized, @"[^a-z0-9-_]", "-");
                break;
        }

        // Remove multiple consecutive hyphens
        sanitized = Regex.Replace(sanitized, @"-+", "-");

        // Remove leading/trailing hyphens
        sanitized = sanitized.Trim('-');

        // Ensure minimum length for storage accounts
        if (resourceType == ResourceType.StorageAccount && sanitized.Length < MinStorageAccountNameLength)
        {
            sanitized = sanitized.PadRight(MinStorageAccountNameLength, '0');
        }

        // Truncate if too long
        var maxLength = resourceType == ResourceType.StorageAccount ? MaxStorageAccountNameLength : MaxResourceNameLength;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);
        }

        return sanitized;
    }
}

/// <summary>
/// Types of Azure resources for naming validation.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// App Service resource.
    /// </summary>
    AppService,

    /// <summary>
    /// Storage Account resource.
    /// </summary>
    StorageAccount,

    /// <summary>
    /// Key Vault resource.
    /// </summary>
    KeyVault,

    /// <summary>
    /// SQL Server resource.
    /// </summary>
    SqlServer,

    /// <summary>
    /// Service Bus resource.
    /// </summary>
    ServiceBus,

    /// <summary>
    /// Application Insights resource.
    /// </summary>
    ApplicationInsights,

    /// <summary>
    /// Generic resource type.
    /// </summary>
    Generic
}

/// <summary>
/// Result of naming validation.
/// </summary>
public class NamingValidationResult
{
    /// <summary>
    /// Whether the name is valid.
    /// </summary>
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// List of validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}
