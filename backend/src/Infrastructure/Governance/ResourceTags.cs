using System.ComponentModel.DataAnnotations;

namespace OnlineCommunities.Infrastructure.Governance;

/// <summary>
/// Represents Azure resource tags for governance and cost management.
/// Provides standardized tagging structure for consistent resource management.
/// </summary>
public class ResourceTags
{
    /// <summary>
    /// The deployment environment (dev, test, staging, prod).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Environment { get; set; } = string.Empty;

    /// <summary>
    /// The project identifier (e.g., "oc" for Online Communities).
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Project { get; set; } = string.Empty;

    /// <summary>
    /// The resource component/purpose (api, database, storage, messaging).
    /// </summary>
    [Required]
    [StringLength(30)]
    public string Component { get; set; } = string.Empty;

    /// <summary>
    /// The team or individual responsible for the resource.
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// The cost allocation center (engineering, product, infrastructure).
    /// </summary>
    [Required]
    [StringLength(30)]
    public string CostCenter { get; set; } = string.Empty;

    /// <summary>
    /// The resource creation date in YYYY-MM-DD format.
    /// </summary>
    [Required]
    [StringLength(10)]
    public string CreatedDate { get; set; } = string.Empty;

    /// <summary>
    /// Who or what created the resource (azure-devops, terraform, manual).
    /// </summary>
    [Required]
    [StringLength(30)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// The application name (optional).
    /// </summary>
    [StringLength(50)]
    public string? Application { get; set; }

    /// <summary>
    /// The application version (optional).
    /// </summary>
    [StringLength(20)]
    public string? Version { get; set; }

    /// <summary>
    /// The data sensitivity level (public, internal, confidential, restricted).
    /// </summary>
    [StringLength(20)]
    public string? DataClassification { get; set; }

    /// <summary>
    /// Whether backup is required (true, false).
    /// </summary>
    [StringLength(5)]
    public string? BackupRequired { get; set; }

    /// <summary>
    /// The data retention period (30d, 90d, 1y, 7y).
    /// </summary>
    [StringLength(10)]
    public string? RetentionPeriod { get; set; }

    /// <summary>
    /// Compliance requirements (gdpr, sox, hipaa, pci).
    /// </summary>
    [StringLength(20)]
    public string? Compliance { get; set; }

    /// <summary>
    /// The preferred maintenance window (sunday-2am, saturday-4am).
    /// </summary>
    [StringLength(20)]
    public string? MaintenanceWindow { get; set; }

    /// <summary>
    /// Contact email for resource issues.
    /// </summary>
    [StringLength(100)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Link to resource documentation.
    /// </summary>
    [StringLength(200)]
    [Url]
    public string? Documentation { get; set; }

    /// <summary>
    /// Additional custom tags.
    /// </summary>
    public Dictionary<string, string> CustomTags { get; set; } = new();

    /// <summary>
    /// Converts the ResourceTags to a dictionary for Azure resource tagging.
    /// </summary>
    /// <returns>A dictionary of tag names and values.</returns>
    public Dictionary<string, string> ToDictionary()
    {
        var tags = new Dictionary<string, string>
        {
            { "Environment", Environment },
            { "Project", Project },
            { "Component", Component },
            { "Owner", Owner },
            { "CostCenter", CostCenter },
            { "CreatedDate", CreatedDate },
            { "CreatedBy", CreatedBy }
        };

        // Add optional tags if they have values
        if (!string.IsNullOrWhiteSpace(Application))
            tags["Application"] = Application;

        if (!string.IsNullOrWhiteSpace(Version))
            tags["Version"] = Version;

        if (!string.IsNullOrWhiteSpace(DataClassification))
            tags["DataClassification"] = DataClassification;

        if (!string.IsNullOrWhiteSpace(BackupRequired))
            tags["BackupRequired"] = BackupRequired;

        if (!string.IsNullOrWhiteSpace(RetentionPeriod))
            tags["RetentionPeriod"] = RetentionPeriod;

        if (!string.IsNullOrWhiteSpace(Compliance))
            tags["Compliance"] = Compliance;

        if (!string.IsNullOrWhiteSpace(MaintenanceWindow))
            tags["MaintenanceWindow"] = MaintenanceWindow;

        if (!string.IsNullOrWhiteSpace(ContactEmail))
            tags["ContactEmail"] = ContactEmail;

        if (!string.IsNullOrWhiteSpace(Documentation))
            tags["Documentation"] = Documentation;

        // Add custom tags
        foreach (var customTag in CustomTags)
        {
            tags[customTag.Key] = customTag.Value;
        }

        return tags;
    }

    /// <summary>
    /// Creates ResourceTags from a dictionary.
    /// </summary>
    /// <param name="tags">The dictionary of tags.</param>
    /// <returns>A ResourceTags instance.</returns>
    public static ResourceTags FromDictionary(Dictionary<string, string> tags)
    {
        var resourceTags = new ResourceTags();

        if (tags.TryGetValue("Environment", out var environment))
            resourceTags.Environment = environment;

        if (tags.TryGetValue("Project", out var project))
            resourceTags.Project = project;

        if (tags.TryGetValue("Component", out var component))
            resourceTags.Component = component;

        if (tags.TryGetValue("Owner", out var owner))
            resourceTags.Owner = owner;

        if (tags.TryGetValue("CostCenter", out var costCenter))
            resourceTags.CostCenter = costCenter;

        if (tags.TryGetValue("CreatedDate", out var createdDate))
            resourceTags.CreatedDate = createdDate;

        if (tags.TryGetValue("CreatedBy", out var createdBy))
            resourceTags.CreatedBy = createdBy;

        if (tags.TryGetValue("Application", out var application))
            resourceTags.Application = application;

        if (tags.TryGetValue("Version", out var version))
            resourceTags.Version = version;

        if (tags.TryGetValue("DataClassification", out var dataClassification))
            resourceTags.DataClassification = dataClassification;

        if (tags.TryGetValue("BackupRequired", out var backupRequired))
            resourceTags.BackupRequired = backupRequired;

        if (tags.TryGetValue("RetentionPeriod", out var retentionPeriod))
            resourceTags.RetentionPeriod = retentionPeriod;

        if (tags.TryGetValue("Compliance", out var compliance))
            resourceTags.Compliance = compliance;

        if (tags.TryGetValue("MaintenanceWindow", out var maintenanceWindow))
            resourceTags.MaintenanceWindow = maintenanceWindow;

        if (tags.TryGetValue("ContactEmail", out var contactEmail))
            resourceTags.ContactEmail = contactEmail;

        if (tags.TryGetValue("Documentation", out var documentation))
            resourceTags.Documentation = documentation;

        // Extract custom tags (not in the standard set)
        var standardTagNames = new HashSet<string>
        {
            "Environment", "Project", "Component", "Owner", "CostCenter",
            "CreatedDate", "CreatedBy", "Application", "Version",
            "DataClassification", "BackupRequired", "RetentionPeriod",
            "Compliance", "MaintenanceWindow", "ContactEmail", "Documentation"
        };

        foreach (var tag in tags)
        {
            if (!standardTagNames.Contains(tag.Key))
            {
                resourceTags.CustomTags[tag.Key] = tag.Value;
            }
        }

        return resourceTags;
    }

    /// <summary>
    /// Validates the resource tags.
    /// </summary>
    /// <returns>A validation result with any errors.</returns>
    public TagValidationResult Validate()
    {
        var result = new TagValidationResult { IsValid = true };

        // Validate required tags
        if (string.IsNullOrWhiteSpace(Environment))
        {
            result.IsValid = false;
            result.Errors.Add("Environment tag is required");
        }
        else if (!IsValidEnvironment(Environment))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid Environment value: {Environment}. Must be one of: dev, test, staging, prod");
        }

        if (string.IsNullOrWhiteSpace(Project))
        {
            result.IsValid = false;
            result.Errors.Add("Project tag is required");
        }

        if (string.IsNullOrWhiteSpace(Component))
        {
            result.IsValid = false;
            result.Errors.Add("Component tag is required");
        }

        if (string.IsNullOrWhiteSpace(Owner))
        {
            result.IsValid = false;
            result.Errors.Add("Owner tag is required");
        }

        if (string.IsNullOrWhiteSpace(CostCenter))
        {
            result.IsValid = false;
            result.Errors.Add("CostCenter tag is required");
        }

        if (string.IsNullOrWhiteSpace(CreatedDate))
        {
            result.IsValid = false;
            result.Errors.Add("CreatedDate tag is required");
        }
        else if (!IsValidDate(CreatedDate))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid CreatedDate format: {CreatedDate}. Must be YYYY-MM-DD");
        }

        if (string.IsNullOrWhiteSpace(CreatedBy))
        {
            result.IsValid = false;
            result.Errors.Add("CreatedBy tag is required");
        }

        // Validate optional tags
        if (!string.IsNullOrWhiteSpace(DataClassification) && !IsValidDataClassification(DataClassification))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid DataClassification value: {DataClassification}. Must be one of: public, internal, confidential, restricted");
        }

        if (!string.IsNullOrWhiteSpace(BackupRequired) && !IsValidBoolean(BackupRequired))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid BackupRequired value: {BackupRequired}. Must be true or false");
        }

        if (!string.IsNullOrWhiteSpace(Compliance) && !IsValidCompliance(Compliance))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid Compliance value: {Compliance}. Must be one of: gdpr, sox, hipaa, pci");
        }

        return result;
    }

    /// <summary>
    /// Checks if the environment value is valid.
    /// </summary>
    /// <param name="environment">The environment value to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidEnvironment(string environment)
    {
        var validEnvironments = new[] { "dev", "test", "staging", "prod" };
        return validEnvironments.Contains(environment.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if the date format is valid.
    /// </summary>
    /// <param name="date">The date string to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidDate(string date)
    {
        return DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Checks if the data classification value is valid.
    /// </summary>
    /// <param name="classification">The classification value to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidDataClassification(string classification)
    {
        var validClassifications = new[] { "public", "internal", "confidential", "restricted" };
        return validClassifications.Contains(classification.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if the boolean value is valid.
    /// </summary>
    /// <param name="value">The boolean value to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidBoolean(string value)
    {
        var validBooleans = new[] { "true", "false" };
        return validBooleans.Contains(value.ToLowerInvariant());
    }

    /// <summary>
    /// Checks if the compliance value is valid.
    /// </summary>
    /// <param name="compliance">The compliance value to check.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool IsValidCompliance(string compliance)
    {
        var validCompliances = new[] { "gdpr", "sox", "hipaa", "pci" };
        return validCompliances.Contains(compliance.ToLowerInvariant());
    }
}

/// <summary>
/// Result of tag validation.
/// </summary>
public class TagValidationResult
{
    /// <summary>
    /// Whether the tags are valid.
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
