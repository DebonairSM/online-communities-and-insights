# Microsoft Entra External ID Token Enrichment - Quick Reference

## 🚨 CRITICAL: Token Enrichment Location

**Token enrichment is NOT in User Flows!** It's in **Enterprise Applications**.

### Wrong Location ❌
- ~~User Flows → API connectors~~
- ~~User Flows → Custom authentication extensions~~

### Correct Location ✅
- **Microsoft Entra ID** → **Enterprise applications** → **Custom authentication extensions**

## Exact Navigation Path

1. **Azure Portal** → Search **"Microsoft Entra ID"**
2. **Left menu** → **"Enterprise applications"**
3. **"Custom authentication extensions"**
4. **"Create a custom extension"**

## Configuration Steps

### 1. Event Type
- **Event type**: `TokenIssuanceStart` ✅

### 2. Endpoint Configuration
- **Name**: `TokenEnrichmentProvider`
- **Target URL**: `https://your-api.azurewebsites.net/api/entra-connector/token-enrichment`
- **Description**: `Adds tenant ID and roles to authentication tokens`

### 3. API Authentication
- **Select**: "Create new app registration"
- **App name**: `Azure Functions authentication events API`

### 4. Claims Configuration
- **Add claim**: `TenantId`
- **Add claim**: `Roles`

### 5. Assign to Application
1. **Select your TokenEnrichmentProvider** from dropdown
2. **Click Save**
3. **Add claims mapping**:
   - **Claim name**: `TenantId`
   - **Source**: `Attribute`
   - **Source attribute**: `customClaimsProvider.TenantId`
   - **Repeat for Roles**

## Backend Endpoint Format

Your backend should return:
```json
{
  "TenantId": "tenant-guid-string",
  "Roles": "Admin,Moderator"  // Comma-separated string
}
```

## Common Confusion Points

### User Flow vs Enterprise Applications

| Location | Purpose | What It Does |
|----------|---------|--------------|
| **User Flows** | Attribute collection | Collects data during sign-up |
| **Enterprise Applications** | Token enrichment | Adds claims to JWT tokens |

### Event Types

| Event Type | When It Runs | Use Case |
|------------|--------------|----------|
| `TokenIssuanceStart` | Before token is issued | Add custom claims |
| `TokenIssuanceEnd` | After token is issued | Logging, auditing |

## Troubleshooting

### "Can't find API connectors"
- **Problem**: Looking in User Flows
- **Solution**: Go to Enterprise Applications → Custom authentication extensions

### "Custom claims not in token"
- **Problem**: Extension not assigned to application
- **Solution**: Assign extension and configure claims mapping

### "Extension not working"
- **Problem**: Wrong event type
- **Solution**: Use `TokenIssuanceStart` for token enrichment

## Key Differences from Azure AD B2C

| Feature | Azure AD B2C | Entra External ID |
|---------|--------------|-------------------|
| Token enrichment | User Flow API connectors | Enterprise Applications extensions |
| Event type | Pre-token | TokenIssuanceStart |
| Configuration | User Flow level | Application level |

## Quick Test

After setup, test with:
```bash
# Get token from user flow
# Check JWT contains:
# - extension_TenantId
# - extension_Roles
```

## Documentation References

- [Microsoft Docs: Custom authentication extensions](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/custom-authentication-extensions)
- [Token enrichment patterns](https://docs.microsoft.com/en-us/azure/active-directory/external-identities/custom-authentication-extensions-token-enrichment)

---

**Remember**: Token enrichment = Enterprise Applications, NOT User Flows!
