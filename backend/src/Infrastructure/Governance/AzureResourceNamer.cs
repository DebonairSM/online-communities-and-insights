using System.Text;

namespace OnlineCommunities.Infrastructure.Governance;

/// <summary>
/// Generates consistent Azure resource names based on naming conventions.
/// Provides centralized naming logic for automated resource creation.
/// </summary>
public class AzureResourceNamer
{
    private readonly string _environment;
    private readonly string _project;
    private readonly string _location;
    private readonly string _instance;

    /// <summary>
    /// Initializes a new instance of the AzureResourceNamer.
    /// </summary>
    /// <param name="environment">The environment code (dev, test, staging, prod).</param>
    /// <param name="project">The project identifier (e.g., "oc" for Online Communities).</param>
    /// <param name="location">The Azure location code (e.g., "eastus").</param>
    /// <param name="instance">The instance identifier (e.g., "01", "primary").</param>
    public AzureResourceNamer(string environment, string project, string location, string instance = "01")
    {
        _environment = ValidateAndSanitize(environment, "environment");
        _project = ValidateAndSanitize(project, "project");
        _location = ValidateAndSanitize(location, "location");
        _instance = ValidateAndSanitize(instance, "instance");
    }

    /// <summary>
    /// Gets the name for an App Service.
    /// </summary>
    /// <param name="component">The component code (e.g., "api", "func").</param>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The App Service name.</returns>
    public string GetAppServiceName(string component, string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, component, instanceToUse);
    }

    /// <summary>
    /// Gets the name for an App Service Plan.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The App Service Plan name.</returns>
    public string GetAppServicePlanName(string component)
    {
        return FormatName(_environment, _project, component, "plan");
    }

    /// <summary>
    /// Gets the name for a Storage Account.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Storage Account name.</returns>
    public string GetStorageAccountName(string component, string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        var name = $"{_environment}{_project}{component}{instanceToUse}";
        
        // Ensure storage account name is lowercase and alphanumeric only
        name = name.ToLowerInvariant();
        name = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-z0-9]", string.Empty);
        
        // Ensure minimum length
        if (name.Length < NamingConventions.MinStorageAccountNameLength)
        {
            name = name.PadRight(NamingConventions.MinStorageAccountNameLength, '0');
        }
        
        // Ensure maximum length
        if (name.Length > NamingConventions.MaxStorageAccountNameLength)
        {
            name = name.Substring(0, NamingConventions.MaxStorageAccountNameLength);
        }
        
        return name;
    }

    /// <summary>
    /// Gets the name for a SQL Server.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The SQL Server name.</returns>
    public string GetSqlServerName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "sql", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a SQL Database.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The SQL Database name.</returns>
    public string GetSqlDatabaseName(string component, string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "db", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Key Vault.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Key Vault name.</returns>
    public string GetKeyVaultName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "kv", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Service Bus.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Service Bus name.</returns>
    public string GetServiceBusName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "sb", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Function App.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Function App name.</returns>
    public string GetFunctionAppName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "func", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Virtual Network.
    /// </summary>
    /// <returns>The Virtual Network name.</returns>
    public string GetVirtualNetworkName()
    {
        return FormatName(_environment, _project, "vnet", string.Empty);
    }

    /// <summary>
    /// Gets the name for a Subnet.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Subnet name.</returns>
    public string GetSubnetName(string component)
    {
        return FormatName(_environment, _project, component, "subnet");
    }

    /// <summary>
    /// Gets the name for a Network Security Group.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Network Security Group name.</returns>
    public string GetNetworkSecurityGroupName(string component)
    {
        return FormatName(_environment, _project, component, "nsg");
    }

    /// <summary>
    /// Gets the name for a Load Balancer.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Load Balancer name.</returns>
    public string GetLoadBalancerName(string component)
    {
        return FormatName(_environment, _project, component, "lb");
    }

    /// <summary>
    /// Gets the name for a Managed Identity.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Managed Identity name.</returns>
    public string GetManagedIdentityName(string component)
    {
        return FormatName(_environment, _project, component, "mi");
    }

    /// <summary>
    /// Gets the name for an Application Registration.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Application Registration name.</returns>
    public string GetApplicationRegistrationName(string component)
    {
        return FormatName(_environment, _project, component, "app");
    }

    /// <summary>
    /// Gets the name for an Event Hub.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Event Hub name.</returns>
    public string GetEventHubName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "eh", instanceToUse);
    }

    /// <summary>
    /// Gets the name for an Event Grid.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Event Grid name.</returns>
    public string GetEventGridName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "eg", instanceToUse);
    }

    /// <summary>
    /// Gets the name for Application Insights.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Application Insights name.</returns>
    public string GetApplicationInsightsName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "ai", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Log Analytics Workspace.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Log Analytics Workspace name.</returns>
    public string GetLogAnalyticsWorkspaceName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "law", instanceToUse);
    }

    /// <summary>
    /// Gets the name for an Action Group.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <returns>The Action Group name.</returns>
    public string GetActionGroupName(string component)
    {
        return FormatName(_environment, _project, component, "ag");
    }

    /// <summary>
    /// Gets the name for a Redis Cache.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Redis Cache name.</returns>
    public string GetRedisCacheName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "redis", instanceToUse);
    }

    /// <summary>
    /// Gets the name for a Container Instance.
    /// </summary>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The Container Instance name.</returns>
    public string GetContainerInstanceName(string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, "aci", instanceToUse);
    }

    /// <summary>
    /// Gets a custom resource name using the standard pattern.
    /// </summary>
    /// <param name="component">The component code.</param>
    /// <param name="instance">Optional instance override.</param>
    /// <returns>The custom resource name.</returns>
    public string GetCustomResourceName(string component, string? instance = null)
    {
        var instanceToUse = instance ?? _instance;
        return FormatName(_environment, _project, component, instanceToUse);
    }

    /// <summary>
    /// Gets the resource group name.
    /// </summary>
    /// <returns>The resource group name.</returns>
    public string GetResourceGroupName()
    {
        return FormatName(_environment, _project, "rg", string.Empty);
    }

    /// <summary>
    /// Gets all resource names for a complete environment setup.
    /// </summary>
    /// <returns>A dictionary of resource types and their names.</returns>
    public Dictionary<string, string> GetAllResourceNames()
    {
        return new Dictionary<string, string>
        {
            { "ResourceGroup", GetResourceGroupName() },
            { "AppService", GetAppServiceName("api") },
            { "AppServicePlan", GetAppServicePlanName("api") },
            { "StorageAccount", GetStorageAccountName("storage") },
            { "SqlServer", GetSqlServerName() },
            { "SqlDatabase", GetSqlDatabaseName("main") },
            { "KeyVault", GetKeyVaultName() },
            { "ServiceBus", GetServiceBusName() },
            { "FunctionApp", GetFunctionAppName() },
            { "VirtualNetwork", GetVirtualNetworkName() },
            { "ApplicationInsights", GetApplicationInsightsName() },
            { "LogAnalyticsWorkspace", GetLogAnalyticsWorkspaceName() },
            { "RedisCache", GetRedisCacheName() }
        };
    }

    /// <summary>
    /// Formats a resource name using the standard pattern.
    /// </summary>
    /// <param name="environment">The environment code.</param>
    /// <param name="project">The project identifier.</param>
    /// <param name="component">The component code.</param>
    /// <param name="instance">The instance identifier.</param>
    /// <returns>The formatted resource name.</returns>
    private static string FormatName(string environment, string project, string component, string instance)
    {
        var parts = new List<string> { environment, project, component };
        
        if (!string.IsNullOrWhiteSpace(instance))
        {
            parts.Add(instance);
        }
        
        return string.Join("-", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    /// <summary>
    /// Validates and sanitizes input parameters.
    /// </summary>
    /// <param name="value">The value to validate and sanitize.</param>
    /// <param name="parameterName">The name of the parameter for error messages.</param>
    /// <returns>The sanitized value.</returns>
    private static string ValidateAndSanitize(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{parameterName} cannot be null or empty", parameterName);
        }

        // Remove invalid characters and convert to lowercase
        var sanitized = System.Text.RegularExpressions.Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]", "-");
        
        // Remove multiple consecutive hyphens
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"-+", "-");
        
        // Remove leading/trailing hyphens
        sanitized = sanitized.Trim('-');
        
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            throw new ArgumentException($"{parameterName} contains only invalid characters", parameterName);
        }
        
        return sanitized;
    }
}
