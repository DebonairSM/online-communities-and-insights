# Microsoft Entra External ID Setup Guide

This guide provides step-by-step instructions for configuring Microsoft Entra External ID for your Online Communities SaaS application.

## Prerequisites

- Azure subscription with administrative access
- Azure Portal access (https://portal.azure.com)
- Your backend API deployed or running locally

## Step 1: Create Entra External ID Tenant

1. **Navigate to Microsoft Entra External ID**:
   - Go to Azure Portal → Search for "Microsoft Entra External ID"
   - Click "Create" to start a new tenant

2. **Configure Tenant Settings**:
   - **Tenant name**: `OnlineCommunities` (or your preferred name)
   - **Domain name**: `yourcompany.onmicrosoft.com`
   - **Data location**: Choose your region
   - Click "Review + create"

3. **Note Your Tenant Information**:
   - **Tenant ID**: Found in tenant overview (GUID format)
   - **Domain**: `yourcompany.onmicrosoft.com`
   - **Instance**: `https://yourcompany.ciamlogin.com/`

## Step 2: Register Your Application

1. **Create App Registration**:
   - In your Entra External ID tenant, go to "App registrations"
   - Click "New registration"
   - **Name**: `OnlineCommunities API`
   - **Supported account types**: "Accounts in this organizational directory only"
   - **Redirect URI**: Leave blank for now (will configure later)
   - Click "Register"

2. **Note Application Details**:
   - **Application (client) ID**: Found on overview page (GUID format)
   - This will be used in your `appsettings.json`

3. **Configure API Permissions**:
   - Go to "API permissions"
   - Add "Microsoft Graph" → "User.Read" (delegated)
   - Grant admin consent

## Step 3: Configure Custom Attributes

1. **Navigate to Custom Attributes**:
   - In your Entra External ID tenant
   - Go to "User attributes" → "Custom attributes"

2. **Create TenantId Attribute**:
   - Click "Add"
   - **Name**: `TenantId`
   - **Data type**: `String`
   - **Description**: `The user's primary tenant identifier`
   - Click "Create"

3. **Create Roles Attribute**:
   - Click "Add"
   - **Name**: `Roles`
   - **Data type**: `String Collection`
   - **Description**: `The user's roles in their primary tenant`
   - Click "Create"

## Step 4: Create User Flow

1. **Navigate to User Flows**:
   - In your Entra External ID tenant
   - Go to "User flows" → "New user flow"

2. **Configure Sign Up and Sign In Flow**:
   - **Name**: `B2C_1_signupsignin`
   - **Identity providers**: Select "Email signup" and any social providers you want (Google, Microsoft, GitHub)

3. **Configure User Attributes**:
   - Select attributes to collect during sign up:
     - Email address (required)
     - Given name
     - Surname
   - Click "Create"

## Step 5: Set Up Custom Authentication Extension

**CRITICAL**: Token enrichment is NOT configured in User Flows. It's configured at the Enterprise Applications level.

### Navigation Path:
1. **Azure Portal** → Search **"Microsoft Entra ID"**
2. **Left menu** → **"Enterprise applications"**
3. **"Custom authentication extensions"**

### Create Custom Extension:
1. **Click "Create a custom extension"**
2. **Event type**: Select **"TokenIssuanceStart"** ✅
3. **Click Next**

### Endpoint Configuration:
- **Name**: `TokenEnrichmentProvider`
- **Target URL**: `https://your-api.azurewebsites.net/api/entra-connector/token-enrichment`
- **Description**: `Adds tenant ID and roles to authentication tokens`
- **Click Next**

### API Authentication:
- **Select "Create new app registration"**
- **App name**: `Azure Functions authentication events API`
- **Click Next**

### Claims Configuration:
- **Add claim**: `TenantId`
- **Add claim**: `Roles`
- **Click Next**, then **Create**

### Assign Extension to Your App:
1. **Select your TokenEnrichmentProvider** from dropdown
2. **Click Save**
3. **Add new claims mapping**:
   - **Claim name**: `TenantId`
   - **Source**: `Attribute`
   - **Source attribute**: `customClaimsProvider.TenantId`
4. **Repeat for Roles claim**:
   - **Claim name**: `Roles`
   - **Source**: `Attribute`
   - **Source attribute**: `customClaimsProvider.Roles`

### Secure Your API Connector Endpoint:
Your `EntraConnectorController.cs` should handle the token enrichment requests:
```csharp
[HttpPost("token-enrichment")]
[AllowAnonymous]
public async Task<IActionResult> EnrichToken([FromBody] EntraTokenRequest request)
{
    // Your existing token enrichment logic...
    return Ok(new {
        TenantId = membership?.TenantId.ToString(),
        Roles = membership?.RoleName ?? "Member"
    });
}
```

## Step 6: Configure Your Backend

Update your `appsettings.json` with the values from above:

```json
{
  "AzureAd": {
    "Instance": "https://yourcompany.ciamlogin.com/",
    "TenantId": "your-tenant-id-guid-here",
    "ClientId": "your-client-id-guid-here",
    "Audience": "your-client-id-guid-here"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-azure-sql-server.database.windows.net;Database=OnlineCommunities;User Id=your-user;Password=your-password;Encrypt=true;"
  }
}
```

## Step 7: Configure Azure SQL Database

1. **Create Azure SQL Database**:
   ```bash
   az sql server create \
     --name onlinecommunities-sql \
     --resource-group rg-communities-prod \
     --location eastus \
     --admin-user sqladmin \
     --admin-password YourSecurePassword123!
   
   az sql db create \
     --resource-group rg-communities-prod \
     --server onlinecommunities-sql \
     --name OnlineCommunities \
     --service-objective S2
   ```

2. **Configure Firewall Rules**:
   - Allow Azure services to access the server
   - Add your development machine IP if testing locally

3. **Run Database Migrations**:
   ```bash
   cd src/Infrastructure
   dotnet ef database update --startup-project ../Api --connection "YourAzureSQLConnectionString"
   ```

## Step 8: Test the Authentication Flow

1. **Start Your Backend API**:
   ```bash
   cd src/Api
   dotnet run
   ```

2. **Test Entra External ID Login**:
   - Navigate to: `https://yourcompany.ciamlogin.com/yourcompany.onmicrosoft.com/oauth2/v2.0/authorize?client_id=YOUR_CLIENT_ID&response_type=id_token&redirect_uri=http://localhost:5189/signin-oidc&scope=openid&nonce=test`
   - Sign in with a test account
   - After authentication, the token will include your custom claims

3. **Verify Custom Claims in Token**:
   The JWT token should now include:
   ```json
   {
     "sub": "entra-user-guid",
     "email": "user@example.com",
     "oid": "object-id",
     "extension_TenantId": "tenant-guid-from-your-database",
     "extension_Roles": ["Member"]
   }
   ```

4. **Test API Endpoints**:
   ```bash
   # Get a token (use browser dev tools to extract the JWT)
   # Then test your API:
   
   curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
        http://localhost:5189/api/auth/validate-token
   ```

## Step 9: Create Test Data

1. **Create a Test Tenant in Your Database**:
   ```sql
   INSERT INTO Tenants (Id, Name, Subdomain, IsActive, SubscriptionTier, SubscriptionExpiresAt, CreatedAt)
   VALUES (
       NEWID(),
       'Test Company',
       'testco',
       1,
       'Free',
       DATEADD(year, 1, GETUTCDATE()),
       GETUTCDATE()
   );
   ```

2. **Create a Test User** (will be created automatically via JIT provisioning, but you can pre-create):
   ```sql
   INSERT INTO Users (Id, Email, FirstName, LastName, AuthMethod, EntraIdSubject, EmailVerified, IsActive, CreatedAt)
   VALUES (
       NEWID(),
       'test@example.com',
       'Test',
       'User',
       6, -- EntraExternalId
       'entra-oid-from-token',
       1,
       1,
       GETUTCDATE()
   );
   ```

3. **Create Tenant Membership**:
   ```sql
   INSERT INTO TenantMemberships (Id, UserId, TenantId, RoleName, JoinedAt, IsActive, CreatedAt)
   VALUES (
       NEWID(),
       (SELECT Id FROM Users WHERE Email = 'test@example.com'),
       (SELECT Id FROM Tenants WHERE Subdomain = 'testco'),
       'Admin',
       GETUTCDATE(),
       1,
       GETUTCDATE()
   );
   ```

## Step 10: Verify End-to-End Flow

1. **User Authentication**:
   - User logs in via Entra External ID
   - Entra authenticates the user
   - Entra calls your API Connector (`/api/entra-connector/token-enrichment`)
   - Your API queries database for user's tenant and role
   - Your API returns `TenantId` and `Roles` to Entra
   - Entra includes these as `extension_TenantId` and `extension_Roles` in the JWT token

2. **API Request**:
   - Frontend sends request with token: `Authorization: Bearer <token>`
   - Your API validates token signature (Microsoft's public keys)
   - Your API extracts claims including `extension_TenantId` and `extension_Roles`
   - Authorization handlers check permissions based on these claims

## Configuration Values Summary

Update these in your `appsettings.json`:

| Setting | Example Value | Where to Find |
|---------|---------------|---------------|
| `AzureAd:Instance` | `https://yourcompany.ciamlogin.com/` | Entra tenant overview |
| `AzureAd:TenantId` | `12345678-1234-1234-1234-123456789012` | Entra tenant overview → Tenant ID |
| `AzureAd:ClientId` | `87654321-4321-4321-4321-210987654321` | App registration → Application (client) ID |
| `AzureAd:Audience` | Same as ClientId | Same as above |

## Security Best Practices

1. **Secure API Connector Endpoint**:
   - Use basic authentication or certificate-based authentication
   - Validate the request is coming from Microsoft Entra
   - Rate limit the endpoint to prevent abuse

2. **Token Validation**:
   - Your backend automatically validates:
     - Token signature (Microsoft's signing keys)
     - Token expiration
     - Issuer and audience claims
   - No additional work needed - ASP.NET Core JWT middleware handles this

3. **HTTPS Only**:
   - Ensure your API uses HTTPS in production
   - Entra External ID requires HTTPS for redirect URIs

4. **Key Rotation**:
   - Microsoft automatically rotates signing keys
   - Your backend automatically discovers new keys via the Authority endpoint

## Troubleshooting

### Issue: "Unable to get metadata from authority"
**Solution**: Check that `AzureAd:Instance` and `AzureAd:TenantId` are correct. The authority URL should be: `https://yourcompany.ciamlogin.com/your-tenant-id/v2.0`

### Issue: "Token validation failed"
**Solution**: Verify that `AzureAd:Audience` matches your Application (client) ID exactly.

### Issue: "Custom claims not appearing in token"
**Solution**: Ensure API Connector is configured in the user flow under "Before sending the token" section.

### Issue: "API Connector failing"
**Solution**: 
- Verify your backend endpoint is publicly accessible
- Check that the URL in API Connector matches exactly
- Review logs in your backend for errors
- Test the endpoint directly with Postman

## Next Steps

1. Deploy your backend to Azure App Service
2. Configure your frontend application to use MSAL for Entra External ID authentication
3. Test the complete authentication flow
4. Monitor authentication logs in Application Insights

---

For frontend integration, see: `docs/frontend/microsoft-entra-external-id-integration.md`

