# Quick Setup: Entra Custom Authentication Extension

## 🚀 5-Minute Setup Guide

### What You Need

- ✅ API running on `https://your-url/api/entra-connector/token-enrichment`
- ✅ Username: `entra-connector-user`
- ✅ Password: (from your `appsettings.json`)

---

## Azure Portal Steps

### 1️⃣ Create the Custom Extension

**Navigation:**
```
Azure Portal → Microsoft Entra External ID → Settings → Custom authentication extensions → + Create
```

**Configuration:**

| Setting | Value |
|---------|-------|
| Display Name | `TokenEnrichmentConnector` |
| Target URL | `https://[your-url]/api/entra-connector/token-enrichment` |
| Authentication | HTTP Basic |
| Username | `entra-connector-user` |
| Password | [from appsettings.json] |
| Timeout | `10000` ms |

**Click:** Create

---

### 2️⃣ Assign to User Flow

**Navigation:**
```
Azure Portal → Microsoft Entra External ID → Identity → External Identities → User flows → [Your Flow]
```

**Steps:**
1. Click **"Custom authentication extensions"** in left menu
2. Find **"When a user submits their information"** section
3. Click the **✏️ pencil icon**
4. Select **"TokenEnrichmentConnector"** from dropdown
5. Click **"Select"**

**Verify:** Extension appears under "When a user submits their information"

---

## Local Development Setup

### Start Your API

```powershell
# Terminal 1: Run API
cd backend/src/Api
dotnet run

# API will start on http://localhost:5000
```

### Expose with Ngrok

```powershell
# Terminal 2: Start ngrok tunnel
ngrok http 5000 --host-header="localhost:5000"

# Copy the forwarding URL (e.g., https://abc123.ngrok.io)
```

### Use the Ngrok URL

In Azure Portal, use:
```
https://abc123.ngrok.io/api/entra-connector/token-enrichment
```

---

## Test the Setup

### 1. Sign In
- Go to your app's sign-in page
- Sign in with a test user

### 2. Check the Token
- Decode JWT at [jwt.ms](https://jwt.ms)
- Look for custom claims:
  ```json
  {
    "extension_TenantId": "...",
    "extension_Roles": "Owner"
  }
  ```

### 3. Check API Logs
```
Token enrichment request for user test@example.com
JIT provisioned user 123 for email test@example.com
Token enrichment successful
```

---

## ⚠️ Troubleshooting

### Can't reach endpoint
- ✅ Verify ngrok is running
- ✅ Check API is running on correct port
- ✅ Test manually: `curl -X POST https://your-url/api/entra-connector/token-enrichment`

### Authentication failed
- ✅ Username/password must match exactly
- ✅ Check for typos in Azure Portal
- ✅ Review `appsettings.json` values

### No custom claims in token
- ✅ Verify extension is enabled
- ✅ Check claims mapping
- ✅ Review API response format

---

## Production Checklist

When deploying to production:

- [ ] Update Target URL to Azure App Service URL
- [ ] Store credentials in Azure Key Vault
- [ ] Enable HTTPS/TLS
- [ ] Configure Application Insights
- [ ] Add rate limiting
- [ ] Test with production user flow

---

## 📚 Full Documentation

See: `docs/setup/ENTRA-CUSTOM-AUTHENTICATION-EXTENSION-SETUP.md`
