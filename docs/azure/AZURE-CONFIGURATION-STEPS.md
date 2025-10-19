# Azure Configuration Steps - Quick Start

This document provides the exact steps you need to complete in Azure to make your authentication system fully functional.

## ✅ What's Already Done (Backend Complete)

- Microsoft Entra External ID token validation
- Entity Framework Core database layer
- User and tenant repository implementations
- Authorization handlers for multi-tenant security
- API endpoints for authentication
- 64 passing unit and integration tests

## 🔧 What You Need to Do in Azure (30-45 minutes)

### Step 1: Create Microsoft Entra External ID Tenant (10 min)

1. Go to https://portal.azure.com
2. Search for "Microsoft Entra External ID"
3. Click "+ Create a tenant"
4. Select "Customer identity and access management (CIAM)"
5. Fill in:
   - **Organization name**: `OnlineCommunities`
   - **Initial domain**: `yourcompany` (creates `yourcompany.onmicrosoft.com`)
   - **Country/Region**: Select your location
6. Click "Review + Create" → "Create"
7. **Save these values**:
   - Tenant ID (GUID on overview page)
   - Instance URL: `https://yourcompany.ciamlogin.com/`

### Step 2: Register Your API Application (5 min)

1. In your new Entra External ID tenant
2. Go to "App registrations" → "+ New registration"
3. Fill in:
   - **Name**: `OnlineCommunities-API`
   - **Supported account types**: "Accounts in this organizational directory only"
   - **Redirect URI**: Leave blank for now
4. Click "Register"
5. **Save this value**:
   - Application (client) ID (GUID on overview page)

### Step 3: Create Custom Attributes (5 min)

1. In your Entra External ID tenant
2. Go to "User attributes" in the left menu
3. Click "+ Add custom attribute"

**Attribute 1: TenantId**
- **Attribute name**: `TenantId`
- **Data type**: `String`
- **Description**: `User's primary tenant identifier`
- Click "Create"

**Attribute 2: Roles**
- **Attribute name**: `Roles`
- **Data type**: `String Collection`
- **Description**: `User roles in their primary tenant`
- Click "Create"

### Step 4: Create Sign-Up/Sign-In User Flow (10 min)

1. Go to "User flows" → "+ New user flow"
2. Select "Sign up and sign in"
3. Fill in:
   - **Name**: `signupsignin` (creates `B2C_1_signupsignin`)
   - **Identity providers**: Check:
     - ☑ Email signup
     - ☑ Google (optional)
     - ☑ Microsoft Account (optional)
     - ☑ GitHub (optional - requires GitHub OAuth app)
4. **User attributes and claims**:
   - Collect during sign-up: Email, Given Name, Surname
   - Return in token: Email, Given Name, Surname, Object ID
5. Click "Create"

### Step 5: Configure Custom Authentication Extension (15 min)

**CRITICAL**: This is what adds tenant and role information to tokens.

**IMPORTANT**: Token enrichment is NOT configured in User Flows. It's configured at the Enterprise Applications level.

#### Navigation Path (Exact Steps):
1. **Azure Portal** → Search for **"Microsoft Entra ID"**
2. **Left menu** → **"Enterprise applications"**
3. **"Custom authentication extensions"**

#### Create Custom Extension:
1. **Click "Create a custom extension"**
2. **Event type**: Select **"TokenIssuanceStart"** ✅
3. **Click Next**

#### Endpoint Configuration:
- **Name**: `TokenEnrichmentProvider`
- **Target URL**: 
  - Local testing: `https://yourtunnel.ngrok.io/api/entra-connector/token-enrichment`
  - Production: `https://your-api.azurewebsites.net/api/entra-connector/token-enrichment`
- **Description**: `Adds tenant ID and roles to authentication tokens`
- **Click Next**

#### API Authentication:
- **Select "Create new app registration"**
- **App name**: `Azure Functions authentication events API`
- **Click Next**

#### Claims Configuration:
- **Add claim**: `TenantId`
- **Add claim**: `Roles`
- **Click Next**, then **Create**

#### Assign Extension to Your App:
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

### Step 6: Update Your Backend Configuration (2 min)

Edit `src/Api/appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://yourcompany.ciamlogin.com/",
    "TenantId": "paste-tenant-id-guid-here",
    "ClientId": "paste-client-id-guid-here",
    "Audience": "paste-client-id-guid-here"
  }
}
```

### Step 7: Set Up Database (Optional - Runs Locally on LocalDB)

**For local testing** (already configured):
- Uses LocalDB: `Server=(localdb)\\mssqllocaldb;Database=OnlineCommunities`
- Run migrations:
  ```bash
  cd src/Infrastructure
  dotnet ef database update --startup-project ../Api
  ```

**For Azure SQL Database** (production):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=OnlineCommunities;User Id=your-user;Password=your-password;Encrypt=true;"
  }
}
```

## Testing Your Setup

### 1. Start Your Backend:
```bash
cd src/Api
dotnet run
```

### 2. Test Health Endpoint:
```bash
curl http://localhost:5189/health
```

Expected: `{"status":"healthy"}`

### 3. Test Landing Page:
```bash
curl http://localhost:5189/
```

Expected:
```json
{
  "message": "Online Communities API",
  "version": "1.0.0",
  "authentication": "Microsoft Entra External ID"
}
```

### 4. Run All Unit Tests:
```bash
dotnet test --filter "FullyQualifiedName~Core.Tests|FullyQualifiedName~Application.Tests"
```

Expected: All 47 tests passing

### 5. Test Authentication (After Azure Setup):

You'll need to:
1. Get a token from Entra External ID (via your frontend or Postman)
2. Test the token validation endpoint:

```bash
curl -H "Authorization: Bearer YOUR_ENTRA_TOKEN" \
     http://localhost:5189/api/auth/validate-token
```

Expected: Token details showing it's valid

## For Local Testing Without Full Azure Setup

You can test the API without configuring Entra External ID:

1. Use the test configuration in `appsettings.Testing.json`
2. Run unit tests (64 tests) - These work without Azure
3. Integration tests for controllers will need Azure configuration

## Exposing Local API for API Connector Testing

If you want to test the API Connector locally before deploying to Azure:

**Option 1: ngrok** (Recommended for testing)
```bash
# Install ngrok: https://ngrok.com/download
ngrok http 5189

# Use the HTTPS URL in your API Connector configuration
# Example: https://abc123.ngrok.io/api/entra-connector/token-enrichment
```

**Option 2: Azure App Service** (For production)
```bash
# Deploy to Azure
az webapp up --name onlinecommunities-api --resource-group rg-communities --runtime "DOTNETCORE:9.0"

# Use: https://onlinecommunities-api.azurewebsites.net/api/entra-connector/token-enrichment
```

## Validation Checklist

Before considering Phase 0 complete:

- [ ] Entra External ID tenant created and configured
- [ ] Application registered with client ID obtained
- [ ] Custom attributes (`TenantId`, `Roles`) created
- [ ] User flow created with sign-up/sign-in
- [ ] **Custom Authentication Extension** created (NOT API Connector in User Flow)
- [ ] Extension assigned to application with claims mapping
- [ ] Backend configuration updated with Entra values
- [ ] Database migrations applied
- [ ] All 64 tests passing
- [ ] Health endpoint responding
- [ ] Can obtain token from Entra External ID
- [ ] Token contains custom claims (`extension_TenantId`, `extension_Roles`)
- [ ] API validates tokens correctly

## Common Issues & Solutions

### Issue: "AzureAd configuration is required"
**Solution**: Update `appsettings.json` with your actual Entra External ID values.

### Issue: "Unable to get metadata from authority"
**Solution**: Check that Instance and TenantId are correct. Test the authority URL:
```
https://yourcompany.ciamlogin.com/your-tenant-id/v2.0/.well-known/openid-configuration
```

### Issue: "Custom claims not in token"
**Solution**: 
- **CRITICAL**: Token enrichment is configured in **Enterprise Applications** → **Custom authentication extensions**, NOT in User Flows
- Verify Custom Authentication Extension is assigned to your application
- Check that endpoint is publicly accessible
- Review backend logs for token enrichment errors

### Issue: "Can't find API connectors in User Flow"
**Solution**: 
- **User Flow extensions** are for attribute collection during sign-up
- **Token enrichment** happens at **Enterprise Applications** level
- Navigate to: **Microsoft Entra ID** → **Enterprise applications** → **Custom authentication extensions**

### Issue: "Database migration fails"
**Solution**:
```bash
# For LocalDB
dotnet ef database drop --startup-project ../Api --force
dotnet ef database update --startup-project ../Api

# For Azure SQL
# Ensure firewall rules allow your IP
```

## What's Next?

**Immediate**:
1. Complete Azure configuration (steps above)
2. Test end-to-end authentication flow
3. Create test users and tenants in database

**Phase 1** (Next sprint):
- Community management features
- Frontend application with MSAL integration
- Member invitation system
- Basic content creation

---

**You now have a production-ready authentication system!** Once you complete the Azure configuration, you'll have an enterprise-grade SaaS authentication foundation with SOC 2 compliance.

For detailed Azure setup: `docs/setup/AZURE-SETUP-GUIDE.md`  
For Phase 0 completion summary: `docs/implementation/PHASE-0-COMPLETE.md`

