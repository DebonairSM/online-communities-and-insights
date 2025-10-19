namespace OnlineCommunities.Api.Extensions;

/// <summary>
/// Extension methods for HttpContext to access request-scoped data.
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the tenant ID from the current HTTP context.
    /// This is typically set by the TenantContextMiddleware.
    /// </summary>
    public static Guid? GetTenantId(this HttpContext context)
    {
        if (context.Items.TryGetValue("TenantId", out var tenantId) && tenantId is Guid guid)
        {
            return guid;
        }
        return null;
    }

    /// <summary>
    /// Sets the tenant ID in the current HTTP context.
    /// This should be called by middleware after resolving the tenant.
    /// </summary>
    public static void SetTenantId(this HttpContext context, Guid tenantId)
    {
        context.Items["TenantId"] = tenantId;
    }
}

