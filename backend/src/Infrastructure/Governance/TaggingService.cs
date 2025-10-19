using Microsoft.Extensions.Logging;

namespace OnlineCommunities.Infrastructure.Governance;

/// <summary>
/// Service for managing Azure resource tags.
/// Provides standardized tagging for governance, cost management, and compliance.
/// </summary>
public class TaggingService
{
    private readonly ILogger<TaggingService> _logger;

    /// <summary>
    /// Initializes a new instance of the TaggingService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public TaggingService(ILogger<TaggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets standard tags for a resource.
    /// </summary>
    /// <param name="environment">The deployment environment.</param>
    /// <param name="project">The project identifier.</param>
    /// <param name="component">The resource component.</param>
    /// <param name="owner">The resource owner.</param>
    /// <param name="costCenter">The cost center.</param>
    /// <param name="createdBy">Who created the resource.</param>
    /// <returns>Standard resource tags.</returns>
    public ResourceTags GetStandardTags(
        string environment,
        string project,
        string component,
        string owner,
        string costCenter = "engineering",
        string createdBy = "azure-devops")
    {
        try
        {
            _logger.LogDebug("Generating standard tags for environment {Environment}, project {Project}, component {Component}",
                environment, project, component);

            var tags = new ResourceTags
            {
                Environment = environment,
                Project = project,
                Component = component,
                Owner = owner,
                CostCenter = costCenter,
                CreatedDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                CreatedBy = createdBy
            };

            // Add environment-specific tags
            AddEnvironmentSpecificTags(tags, environment);

            // Add component-specific tags
            AddComponentSpecificTags(tags, component);

            _logger.LogDebug("Generated standard tags for {Component} in {Environment}", component, environment);
            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate standard tags for environment {Environment}, project {Project}, component {Component}",
                environment, project, component);
            throw;
        }
    }

    /// <summary>
    /// Gets production API service tags.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <param name="version">The application version.</param>
    /// <returns>Production API service tags.</returns>
    public ResourceTags GetProductionApiTags(string owner, string version = "1.0.0")
    {
        var tags = GetStandardTags("prod", "oc", "api", owner, "product");
        tags.Application = "online-communities-api";
        tags.Version = version;
        tags.DataClassification = "confidential";
        tags.BackupRequired = "true";
        tags.Compliance = "gdpr";
        tags.RetentionPeriod = "7y";
        tags.MaintenanceWindow = "sunday-2am";
        tags.ContactEmail = "api-team@company.com";
        tags.Documentation = "https://wiki.company.com/online-communities-api";

        return tags;
    }

    /// <summary>
    /// Gets development database tags.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <returns>Development database tags.</returns>
    public ResourceTags GetDevelopmentDatabaseTags(string owner)
    {
        var tags = GetStandardTags("dev", "oc", "database", owner);
        tags.Application = "online-communities-db";
        tags.DataClassification = "internal";
        tags.BackupRequired = "false";
        tags.ContactEmail = "dev-team@company.com";

        return tags;
    }

    /// <summary>
    /// Gets staging storage tags.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <returns>Staging storage tags.</returns>
    public ResourceTags GetStagingStorageTags(string owner)
    {
        var tags = GetStandardTags("staging", "oc", "storage", owner);
        tags.Application = "online-communities-storage";
        tags.DataClassification = "internal";
        tags.BackupRequired = "true";
        tags.RetentionPeriod = "90d";
        tags.ContactEmail = "qa-team@company.com";

        return tags;
    }

    /// <summary>
    /// Gets production database tags.
    /// </summary>
    /// <param name="owner">The resource owner.</param>
    /// <returns>Production database tags.</returns>
    public ResourceTags GetProductionDatabaseTags(string owner)
    {
        var tags = GetStandardTags("prod", "oc", "database", owner, "product");
        tags.Application = "online-communities-db";
        tags.DataClassification = "confidential";
        tags.BackupRequired = "true";
        tags.Compliance = "gdpr";
        tags.RetentionPeriod = "7y";
        tags.MaintenanceWindow = "sunday-3am";
        tags.ContactEmail = "dba-team@company.com";
        tags.Documentation = "https://wiki.company.com/online-communities-database";

        return tags;
    }

    /// <summary>
    /// Gets messaging service tags.
    /// </summary>
    /// <param name="environment">The deployment environment.</param>
    /// <param name="owner">The resource owner.</param>
    /// <returns>Messaging service tags.</returns>
    public ResourceTags GetMessagingServiceTags(string environment, string owner)
    {
        var tags = GetStandardTags(environment, "oc", "messaging", owner);
        tags.Application = "online-communities-messaging";
        tags.DataClassification = environment == "prod" ? "confidential" : "internal";
        tags.BackupRequired = environment == "prod" ? "true" : "false";
        
        if (environment == "prod")
        {
            tags.Compliance = "gdpr";
            tags.RetentionPeriod = "1y";
            tags.MaintenanceWindow = "sunday-1am";
        }

        tags.ContactEmail = "messaging-team@company.com";

        return tags;
    }

    /// <summary>
    /// Validates resource tags.
    /// </summary>
    /// <param name="tags">The tags to validate.</param>
    /// <returns>A validation result.</returns>
    public TagValidationResult ValidateTags(ResourceTags tags)
    {
        try
        {
            _logger.LogDebug("Validating resource tags for component {Component} in environment {Environment}",
                tags.Component, tags.Environment);

            var result = tags.Validate();

            if (result.IsValid)
            {
                _logger.LogDebug("Tag validation successful for component {Component}", tags.Component);
            }
            else
            {
                _logger.LogWarning("Tag validation failed for component {Component}: {Errors}",
                    tags.Component, string.Join(", ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate tags for component {Component}", tags.Component);
            return new TagValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Validation error: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Validates tags from a dictionary.
    /// </summary>
    /// <param name="tagDictionary">The tag dictionary to validate.</param>
    /// <returns>A validation result.</returns>
    public TagValidationResult ValidateTags(Dictionary<string, string> tagDictionary)
    {
        try
        {
            var tags = ResourceTags.FromDictionary(tagDictionary);
            return ValidateTags(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate tags from dictionary");
            return new TagValidationResult
            {
                IsValid = false,
                Errors = new List<string> { $"Validation error: {ex.Message}" }
            };
        }
    }

    /// <summary>
    /// Merges custom tags with standard tags.
    /// </summary>
    /// <param name="standardTags">The standard tags.</param>
    /// <param name="customTags">The custom tags to merge.</param>
    /// <returns>Merged tags.</returns>
    public ResourceTags MergeTags(ResourceTags standardTags, Dictionary<string, string> customTags)
    {
        try
        {
            _logger.LogDebug("Merging custom tags with standard tags for component {Component}",
                standardTags.Component);

            // Add custom tags to the standard tags
            foreach (var customTag in customTags)
            {
                standardTags.CustomTags[customTag.Key] = customTag.Value;
            }

            _logger.LogDebug("Successfully merged {CustomTagCount} custom tags", customTags.Count);
            return standardTags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge custom tags with standard tags");
            throw;
        }
    }

    /// <summary>
    /// Gets tags for a specific resource type and environment.
    /// </summary>
    /// <param name="resourceType">The type of resource.</param>
    /// <param name="environment">The deployment environment.</param>
    /// <param name="owner">The resource owner.</param>
    /// <param name="customTags">Optional custom tags.</param>
    /// <returns>Resource tags for the specified type and environment.</returns>
    public ResourceTags GetTagsForResourceType(
        string resourceType,
        string environment,
        string owner,
        Dictionary<string, string>? customTags = null)
    {
        try
        {
            _logger.LogDebug("Getting tags for resource type {ResourceType} in environment {Environment}",
                resourceType, environment);

            ResourceTags tags = resourceType.ToLowerInvariant() switch
            {
                "api" => GetProductionApiTags(owner),
                "database" => environment == "prod" ? GetProductionDatabaseTags(owner) : GetDevelopmentDatabaseTags(owner),
                "storage" => environment == "staging" ? GetStagingStorageTags(owner) : GetStandardTags(environment, "oc", "storage", owner),
                "messaging" => GetMessagingServiceTags(environment, owner),
                _ => GetStandardTags(environment, "oc", resourceType, owner)
            };

            // Override environment if needed
            if (tags.Environment != environment)
            {
                tags.Environment = environment;
            }

            // Add custom tags if provided
            if (customTags != null && customTags.Any())
            {
                tags = MergeTags(tags, customTags);
            }

            _logger.LogDebug("Generated tags for resource type {ResourceType} in environment {Environment}",
                resourceType, environment);

            return tags;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get tags for resource type {ResourceType} in environment {Environment}",
                resourceType, environment);
            throw;
        }
    }

    /// <summary>
    /// Adds environment-specific tags.
    /// </summary>
    /// <param name="tags">The tags to modify.</param>
    /// <param name="environment">The environment.</param>
    private static void AddEnvironmentSpecificTags(ResourceTags tags, string environment)
    {
        switch (environment.ToLowerInvariant())
        {
            case "dev":
                tags.DataClassification = "internal";
                tags.BackupRequired = "false";
                break;

            case "test":
                tags.DataClassification = "internal";
                tags.BackupRequired = "false";
                break;

            case "staging":
                tags.DataClassification = "internal";
                tags.BackupRequired = "true";
                tags.RetentionPeriod = "90d";
                break;

            case "prod":
                tags.DataClassification = "confidential";
                tags.BackupRequired = "true";
                tags.Compliance = "gdpr";
                tags.RetentionPeriod = "7y";
                break;
        }
    }

    /// <summary>
    /// Adds component-specific tags.
    /// </summary>
    /// <param name="tags">The tags to modify.</param>
    /// <param name="component">The component.</param>
    private static void AddComponentSpecificTags(ResourceTags tags, string component)
    {
        switch (component.ToLowerInvariant())
        {
            case "api":
                tags.Application = "online-communities-api";
                tags.MaintenanceWindow = "sunday-2am";
                tags.ContactEmail = "api-team@company.com";
                break;

            case "database":
                tags.Application = "online-communities-db";
                tags.MaintenanceWindow = "sunday-3am";
                tags.ContactEmail = "dba-team@company.com";
                break;

            case "storage":
                tags.Application = "online-communities-storage";
                tags.MaintenanceWindow = "saturday-4am";
                tags.ContactEmail = "storage-team@company.com";
                break;

            case "messaging":
                tags.Application = "online-communities-messaging";
                tags.MaintenanceWindow = "sunday-1am";
                tags.ContactEmail = "messaging-team@company.com";
                break;
        }
    }
}
