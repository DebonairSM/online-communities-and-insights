# Microsoft Entra External ID Roles Setup - Option 1: Comma-Separated String

## Overview

Since Microsoft Entra External ID doesn't support "String Collection" as a data type for custom attributes, we use a single String field to store comma-separated role values.

## Custom Attribute Configuration

### Create Roles Attribute

In your Entra External ID tenant:

1. Go to **"User attributes"** → **"Custom user attributes"**
2. Click **"+ Add an attribute"**
3. Configure:
   - **Name**: `Roles`
   - **Data type**: `String` (not String Collection)
   - **Description**: `User roles in their primary tenant (comma-separated)`
4. Click **"Create"**

### Example Values

The Roles attribute will contain values like:
- `"Member"` (single role)
- `"Admin,Moderator"` (multiple roles)
- `"Admin,Moderator,Member"` (multiple roles)

## Backend Code Changes

### Updated ClaimsPrincipalExtensions.cs

The `GetRoles()` method now handles comma-separated strings:

```csharp
/// <summary>
/// Gets the user roles from the claims principal.
/// Reads from Entra External ID custom 'extension_Roles' claim as comma-separated string.
/// </summary>
public static string[] GetRoles(this ClaimsPrincipal principal)
{
    var rolesClaim = principal.FindFirst("extension_Roles");
    if (rolesClaim != null && !string.IsNullOrEmpty(rolesClaim.Value))
    {
        return rolesClaim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                              .Select(r => r.Trim())
                              .ToArray();
    }
    return Array.Empty<string>();
}
```

### Updated EntraConnectorController.cs

The token enrichment endpoint now returns a single string instead of an array:

```csharp
// Prepare response for Entra
var response = new
{
    // Custom attributes will be prefixed with 'extension_' by Entra
    TenantId = membership?.TenantId.ToString(),
    Roles = membership?.RoleName ?? "Member" // Return as single string
};
```

## How It Works

### 1. User Authentication Flow

1. **User logs in** → Entra External ID authenticates
2. **API Connector called** → Entra calls `/api/entra-connector/token-enrichment`
3. **Backend queries database** → Gets user's tenant and role
4. **Returns to Entra** → `Roles: "Admin"` (single string)
5. **Token issued** → Contains `extension_Roles: "Admin"` claim
6. **API request** → Backend splits comma-separated string into array

### 2. Token Structure

```json
{
  "sub": "entra-user-guid",
  "email": "user@example.com",
  "extension_TenantId": "tenant-guid",
  "extension_Roles": "Admin,Moderator"
}
```

### 3. Backend Processing

```csharp
// In your controllers or services
var userRoles = User.GetRoles(); // Returns ["Admin", "Moderator"]
var hasAdminRole = userRoles.Contains("Admin");
```

## Multiple Roles Support

### For Users with Multiple Roles

If a user has multiple roles in your system, update your API Connector logic:

```csharp
// In EntraConnectorController.cs - Enhanced version
var userRoles = new List<string>();

// Get all memberships for the user
var memberships = await _tenantMembershipRepository.GetByUserIdAsync(user.Id);

// Collect all unique roles
foreach (var membership in memberships)
{
    if (!userRoles.Contains(membership.RoleName))
    {
        userRoles.Add(membership.RoleName);
    }
}

// Return comma-separated string
var response = new
{
    TenantId = membership?.TenantId.ToString(),
    Roles = string.Join(",", userRoles) // "Admin,Moderator,Member"
};
```

### Role Hierarchy Example

```csharp
// Example: User with Admin role automatically gets Member role
var roles = new List<string> { membership.RoleName };

if (membership.RoleName == "Admin")
{
    roles.Add("Moderator");
    roles.Add("Member");
}
else if (membership.RoleName == "Moderator")
{
    roles.Add("Member");
}

var response = new
{
    TenantId = membership?.TenantId.ToString(),
    Roles = string.Join(",", roles.Distinct()) // "Admin,Moderator,Member"
};
```

## Testing

### Test Token Enrichment

```bash
# Test your API Connector endpoint
curl -X POST https://your-api.azurewebsites.net/api/entra-connector/token-enrichment \
  -H "Content-Type: application/json" \
  -H "Authorization: Basic base64(username:password)" \
  -d '{
    "email": "test@example.com",
    "objectId": "entra-oid",
    "identityProvider": "email"
  }'
```

Expected response:
```json
{
  "TenantId": "tenant-guid",
  "Roles": "Admin,Moderator"
}
```

### Test Role Extraction

```csharp
// In your controller
[HttpGet("test-roles")]
[Authorize]
public IActionResult TestRoles()
{
    var roles = User.GetRoles();
    return Ok(new { roles });
}
```

## Benefits of This Approach

1. **Compatible**: Works with Entra External ID's String data type
2. **Flexible**: Supports single or multiple roles
3. **Simple**: Easy to parse and work with
4. **Efficient**: Single claim instead of multiple claims
5. **Future-proof**: Easy to extend for additional roles

## Migration from Array Format

If you previously had code expecting an array format, the `GetRoles()` method handles the conversion automatically. No other changes needed in your authorization handlers or business logic.

## Common Patterns

### Role Checking

```csharp
// Check if user has specific role
var hasAdminRole = User.GetRoles().Contains("Admin");

// Check if user has any of multiple roles
var hasModeratorAccess = User.GetRoles().Any(r => 
    new[] { "Admin", "Moderator" }.Contains(r));

// Get highest role (assuming hierarchy)
var roles = User.GetRoles();
var highestRole = roles.Contains("Admin") ? "Admin" :
                 roles.Contains("Moderator") ? "Moderator" : "Member";
```

### Authorization Policies

Your existing authorization policies continue to work unchanged:

```csharp
[Authorize(Policy = "RequireAdmin")]
public IActionResult AdminOnly() { ... }

[Authorize(Policy = "RequireModerator")]
public IActionResult ModeratorOrAbove() { ... }
```

This approach provides a robust, scalable solution for role management with Microsoft Entra External ID.
