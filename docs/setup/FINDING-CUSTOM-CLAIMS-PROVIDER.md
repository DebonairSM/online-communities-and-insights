# Where to Configure Custom Claims Provider (Token Enrichment)

## ⚠️ Important: You're Looking in the Wrong Place!

The **"Custom authentication extensions"** page under **User Flows** is for:
- ✅ Attribute collection (sign-up form validation)
- ✅ Attribute submission events
- ✅ One-time passcode customization

For **Token Enrichment** (adding custom claims to JWT tokens), you need a **Custom Claims Provider**, which is configured in a different location.

---

## 🎯 Where to Go: 3 Possible Paths

### Path 1: Custom Authentication Extensions (Recommended)

**Navigation:**
```
Azure Portal
  → Microsoft Entra External ID
    → Identity
      → Custom authentication extensions
        → + Add custom extension
```

**Steps:**
1. Click **"+ Add custom extension"**
2. Select **"TokenIssuanceStart"** as the event type
3. Give it a name: `TokenEnrichmentProvider`
4. Configure your REST API endpoint
5. Set up authentication (HTTP Basic)
6. Save the extension

---

### Path 2: Via App Registration

**Navigation:**
```
Azure Portal
  → Microsoft Entra ID
    → App registrations
      → [Your App: 7daf9f98-b404-4b7d-b404-7606caebdb7c]
        → Token configuration
          → Add optional claim or Custom claims provider
```

**Alternative in App Registration:**
```
Azure Portal
  → Microsoft Entra ID
    → App registrations
      → [Your App]
        → API permissions
          → Custom authentication extensions
```

---

### Path 3: Via Enterprise Applications

**Navigation:**
```
Azure Portal
  → Microsoft Entra External ID
    → Applications
      → Enterprise applications
        → [Your Application]
          → Single sign-on
            → Custom claims provider
```

---

## 📝 Configuration Details

Once you find the right place, here's what you'll configure:

### Custom Claims Provider Settings

| Setting | Value |
|---------|-------|
| **Name** | `TokenEnrichmentProvider` |
| **Event Type** | `OnTokenIssuanceStart` |
| **Endpoint URL** | `https://[your-api]/api/entra-connector/token-enrichment` |
| **Authentication** | HTTP Basic Authentication |
| **Username** | `entra-connector-user` |
| **Password** | [from appsettings.json] |

### Claims to Return

Your API will return these claims:
- `TenantId` - User's primary tenant GUID
- `Roles` - User's role(s) in the tenant

These will appear in the token as:
- `extension_TenantId`
- `extension_Roles`

---

## 🔍 What You're Looking For

You should see a screen that allows you to:

1. **Create a new custom claims provider**
   - Event type: **TokenIssuanceStart** / **OnTokenIssuanceStart**
   - Not attribute collection events!

2. **Configure REST API endpoint**
   - Target URL
   - Authentication method
   - Timeout settings

3. **Assign to applications**
   - Which apps should use this custom claims provider
   - Claim mapping configuration

---

## ❌ What You're NOT Looking For

You are currently on the **User Flow Custom Authentication Extensions** page, which is for:
- Attribute collection during sign-up
- Form validation
- Pre-populating sign-up fields

This is **NOT** where you configure token enrichment!

---

## 🎬 Step-by-Step: Once You Find the Right Page

### Step 1: Create Custom Claims Provider

1. Click **"+ Add custom extension"** or **"+ Create custom claims provider"**
2. Fill in:
   - **Display name:** `TokenEnrichmentProvider`
   - **Description:** `Adds tenant ID and roles to authentication tokens`
   - **Event type:** `TokenIssuanceStart`

### Step 2: Configure API Endpoint

1. **Target URL:** `https://[your-ngrok-url]/api/entra-connector/token-enrichment`
2. **Authentication type:** `HTTP Basic Authentication`
3. **Username:** `entra-connector-user`
4. **Password:** [your strong password from appsettings.json]
5. **Timeout:** `10000` ms

### Step 3: Map Claims

Configure which claims from your API response should be included:

| API Response Claim | Token Claim Name |
|-------------------|------------------|
| `TenantId` | `extension_TenantId` |
| `Roles` | `extension_Roles` |

### Step 4: Assign to Application

1. Select your application: `7daf9f98-b404-4b7d-b404-7606caebdb7c`
2. Enable the custom claims provider
3. Save configuration

---

## 🧪 Testing

After configuration, test by:

1. **Sign in** to your application
2. **Capture the JWT token** from browser DevTools
3. **Decode the token** at [jwt.ms](https://jwt.ms)
4. **Verify claims** are present:

```json
{
  "aud": "7daf9f98-b404-4b7d-b404-7606caebdb7c",
  "iss": "https://OnlineCommunities.ciamlogin.com/f44bdf23-d931-4f67-b046-7798f80b618f/v2.0",
  "extension_TenantId": "00000000-0000-0000-0000-000000000000",
  "extension_Roles": "Owner",
  ...
}
```

---

## 🆘 Still Can't Find It?

If you still can't locate the Custom Claims Provider configuration:

### Alternative: Use Azure CLI

```powershell
# List all custom authentication extensions
az rest --method get --url "https://graph.microsoft.com/beta/identity/customAuthenticationExtensions"

# Create a custom claims provider
az rest --method post --url "https://graph.microsoft.com/beta/identity/customAuthenticationExtensions" `
  --headers "Content-Type=application/json" `
  --body @custom-claims-provider.json
```

### Or Use Microsoft Graph API

You can also configure this via the Microsoft Graph API endpoint:
```
POST https://graph.microsoft.com/beta/identity/customAuthenticationExtensions
```

---

## 📚 Additional Resources

- [Microsoft Docs: Custom Claims Provider](https://learn.microsoft.com/entra/identity-platform/custom-extension-tokenissuancestart-configuration)
- [Token Issuance Start Event](https://learn.microsoft.com/entra/identity-platform/custom-extension-tokenissuancestart-setup)
- [Configure Custom Claims](https://learn.microsoft.com/entra/external-id/customers/how-to-add-attributes-to-token)

---

## ✅ Key Takeaways

1. **User Flow Custom Extensions** ≠ **Custom Claims Provider**
2. You need **TokenIssuanceStart** event type
3. Look for **"Custom authentication extensions"** under **Identity** menu
4. Or configure via **App Registration** → **Token configuration**
5. The API response format has been updated in your code to match Microsoft's schema
