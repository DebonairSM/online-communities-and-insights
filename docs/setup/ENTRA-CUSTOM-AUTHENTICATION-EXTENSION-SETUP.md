# Entra Custom Authentication Extension Setup Guide

This guide walks you through setting up a Custom Authentication Extension in Microsoft Entra External ID to enrich tokens with custom claims (tenant ID and roles).

## Overview

The Custom Authentication Extension calls your API during the sign-in flow to add custom claims to the token. This enables multi-tenant context and role-based access control.

## Prerequisites

1. ✅ Entra External ID tenant configured
2. ✅ Application registration created
3. ✅ Backend API running with `/api/entra-connector/token-enrichment` endpoint
4. ✅ Authentication credentials configured in `appsettings.json`

## Step 1: Prepare Your API Endpoint

### 1.1 Update Authentication Credentials

In `appsettings.json`, update the `EntraConnector` section:

```json
"EntraConnector": {
  "Username": "entra-connector-user",
  "Password": "CHANGE_THIS_TO_A_STRONG_PASSWORD"
}
```

**Important:** 
- Use a strong, unique password (minimum 16 characters recommended)
- Store this securely - you'll need it for the Azure configuration
- For production, use Azure Key Vault

### 1.2 Expose Your API Endpoint

For **local development**, use ngrok or similar tunneling tool:

```powershell
# Run your API first
dotnet run --project backend/src/Api

# In another terminal, start ngrok
ngrok http 5000 --host-header="localhost:5000"
```

Copy the ngrok URL (e.g., `https://abc123.ngrok.io`)

For **production**, deploy to Azure App Service and use that URL.

## Step 2: Create Custom Authentication Extension in Azure Portal

### 2.1 Navigate to Custom Authentication Extensions

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Microsoft Entra External ID**
3. Go to **Settings** → **Custom authentication extensions**
4. Click **"+ Create a custom extension"**

### 2.2 Configure the API Connector

Fill in the following details:

| Field | Value |
|-------|-------|
| **Display Name** | `TokenEnrichmentConnector` |
| **Description** | `Adds tenant ID and role claims to authentication tokens` |
| **Target URL** | `https://[your-ngrok-url]/api/entra-connector/token-enrichment` |
| **Timeout** | `10000` (10 seconds) |

### 2.3 Configure Authentication

Select **HTTP Basic Authentication** and enter:

| Field | Value |
|-------|-------|
| **Username** | `entra-connector-user` (must match appsettings.json) |
| **Password** | Your strong password from appsettings.json |

### 2.4 Configure Claims Mapping

Add the following custom claims:

| Claim Name | Source | Description |
|------------|--------|-------------|
| `TenantId` | API Response | User's primary tenant ID |
| `Roles` | API Response | User's roles in the tenant |

Click **"Create"** to save the custom extension.

## Step 3: Assign Extension to User Flow

### 3.1 Navigate to User Flows

1. Go to **Identity** → **External Identities** → **User flows**
2. Select your sign-up/sign-in user flow (e.g., `signupsignin`)

### 3.2 Configure Custom Authentication Extensions

1. In the left menu, select **Custom authentication extensions**
2. Look for the section **"When a user submits their information"**
3. Click the **pencil icon** to edit
4. Select your `TokenEnrichmentConnector` from the dropdown
5. Click **"Select"** to save

### 3.3 Verify Configuration

After saving, you should see:
- ✅ Custom extension listed under "When a user submits their information"
- ✅ Status shows as "Enabled"

## Step 4: Test the Integration

### 4.1 Test Sign-In Flow

1. Navigate to your application's sign-in page
2. Sign in with a test user
3. Check the browser console/network tab for the token

### 4.2 Verify Token Claims

Decode the JWT token (use [jwt.ms](https://jwt.ms)) and verify:

```json
{
  "extension_TenantId": "00000000-0000-0000-0000-000000000000",
  "extension_Roles": "Owner",
  ...
}
```

### 4.3 Check API Logs

Review your API logs for token enrichment requests:

```
Token enrichment request for user test@example.com from provider AADInternal
JIT provisioned user 123 for email test@example.com
Token enrichment successful for user 123
```

## Step 5: Production Deployment

### 5.1 Update Production Settings

When deploying to production:

1. **Update Target URL** to your Azure App Service URL:
   ```
   https://your-api.azurewebsites.net/api/entra-connector/token-enrichment
   ```

2. **Store credentials in Key Vault**:
   ```json
   "EntraConnector": {
     "Username": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/EntraConnectorUsername)",
     "Password": "@Microsoft.KeyVault(SecretUri=https://your-vault.vault.azure.net/secrets/EntraConnectorPassword)"
   }
   ```

3. **Configure HTTPS/TLS**:
   - Ensure your API uses HTTPS
   - Configure SSL certificate in Azure App Service

### 5.2 Security Best Practices

- ✅ Use Azure Key Vault for credentials
- ✅ Enable Application Insights for monitoring
- ✅ Implement rate limiting on the endpoint
- ✅ Add request validation/authentication
- ✅ Log all enrichment requests for audit trail

## Troubleshooting

### Issue: "Unable to reach endpoint"

**Solution:**
- Verify ngrok is running and URL is correct
- Check firewall/network settings
- Ensure API is running and accessible
- Test endpoint manually: `curl -X POST https://your-url/api/entra-connector/token-enrichment`

### Issue: "Authentication failed"

**Solution:**
- Verify username/password match between Azure and appsettings.json
- Check for special characters in password (encode if needed)
- Review API logs for authentication errors

### Issue: "Custom claims not in token"

**Solution:**
- Verify claims mapping is configured correctly
- Check API response format matches expected structure
- Ensure extension is enabled in user flow
- Review Entra logs in Azure Portal

### Issue: "Timeout errors"

**Solution:**
- Increase timeout in custom extension settings
- Optimize API response time
- Check database connection/query performance
- Add caching for frequently accessed data

## API Response Format

Your API must return claims in this format:

```json
{
  "data": {
    "@odata.type": "microsoft.graph.onTokenIssuanceStartResponseData",
    "actions": [
      {
        "@odata.type": "microsoft.graph.tokenIssuanceStart.provideClaimsForToken",
        "claims": {
          "TenantId": "00000000-0000-0000-0000-000000000000",
          "Roles": "Owner"
        }
      }
    ]
  }
}
```

## Next Steps

1. ✅ Set up monitoring and alerting
2. ✅ Configure Application Insights
3. ✅ Implement request throttling
4. ✅ Add comprehensive error handling
5. ✅ Document API endpoint behavior
6. ✅ Create runbook for operations team

## Additional Resources

- [Microsoft Entra Custom Extensions Documentation](https://learn.microsoft.com/entra/identity-platform/custom-extension-overview)
- [Token Enrichment Guide](https://learn.microsoft.com/entra/external-id/customers/how-to-add-attributes-to-token)
- [API Connector Reference](https://learn.microsoft.com/entra/identity-platform/custom-extension-tokenissuancestart-setup)

## Support

For issues or questions:
1. Check the [troubleshooting section](#troubleshooting) above
2. Review API logs and Azure Entra logs
3. Contact your Azure administrator
