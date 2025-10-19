# Getting Your API Endpoint URL with ngrok

## Current Status ✅

Your API is running on: **`http://localhost:5189`**

Your endpoint path is: **`/api/entra-connector/token-enrichment`**

**Full local URL:** `http://localhost:5189/api/entra-connector/token-enrichment`

---

## What You Need: Public URL with ngrok

Microsoft Entra needs a **publicly accessible HTTPS URL** to call your API. Since your API is running locally, we'll use **ngrok** to create a secure tunnel.

---

## Option 1: Install ngrok (Recommended for Testing)

### Step 1: Download ngrok

1. Go to [https://ngrok.com/download](https://ngrok.com/download)
2. Download the Windows version
3. Extract `ngrok.exe` to a folder (e.g., `C:\ngrok\`)

### Step 2: Sign Up (Optional but Recommended)

1. Create a free account at [https://dashboard.ngrok.com/signup](https://dashboard.ngrok.com/signup)
2. Get your authtoken from [https://dashboard.ngrok.com/get-started/your-authtoken](https://dashboard.ngrok.com/get-started/your-authtoken)
3. Configure ngrok:
   ```powershell
   C:\ngrok\ngrok.exe config add-authtoken YOUR_AUTH_TOKEN
   ```

### Step 3: Start ngrok Tunnel

Open a **new PowerShell terminal** and run:

```powershell
# Navigate to where you extracted ngrok
cd C:\ngrok

# Start the tunnel (keep this running!)
.\ngrok.exe http 5189 --host-header="localhost:5189"
```

**Alternative if ngrok is in your PATH:**
```powershell
ngrok http 5189 --host-header="localhost:5189"
```

### Step 4: Get Your Public URL

After running ngrok, you'll see output like this:

```
ngrok

Session Status                online
Account                       your-email@example.com
Version                       3.x.x
Region                        United States (us)
Latency                       -
Web Interface                 http://127.0.0.1:4040
Forwarding                    https://abc123def456.ngrok-free.app -> http://localhost:5189

Connections                   ttl     opn     rt1     rt5     p50     p90
                              0       0       0.00    0.00    0.00    0.00
```

**Your Target URL will be:**
```
https://abc123def456.ngrok-free.app/api/entra-connector/token-enrichment
```

**Copy the `https://` URL** (e.g., `https://abc123def456.ngrok-free.app`) and add `/api/entra-connector/token-enrichment` to the end.

---

## Option 2: Use Cloudflare Tunnel (Alternative)

If you prefer not to use ngrok, you can use Cloudflare Tunnel:

```powershell
# Install cloudflared
winget install --id Cloudflare.cloudflared

# Start tunnel
cloudflared tunnel --url http://localhost:5189
```

---

## Option 3: Deploy to Azure (Production Ready)

For a production setup, deploy your API to Azure App Service:

### Quick Deploy Steps:

1. **Create an Azure App Service:**
   ```powershell
   az webapp up --name onlinecommunities-api --resource-group OnlineCommunities-RG --runtime "DOTNETCORE:8.0"
   ```

2. **Your Target URL will be:**
   ```
   https://onlinecommunities-api.azurewebsites.net/api/entra-connector/token-enrichment
   ```

---

## Testing Your Endpoint

Once you have your public URL, test it:

```powershell
# Test the endpoint (replace with your ngrok URL)
curl -X POST https://your-ngrok-url.ngrok-free.app/api/entra-connector/token-enrichment `
  -H "Content-Type: application/json" `
  -d '{"email":"test@example.com"}'
```

You should see a response with the token enrichment data.

---

## What to Do Next

### ✅ Step 1: Keep Your API Running
Your API is already running on `http://localhost:5189`. Keep that terminal open!

### ✅ Step 2: Start ngrok
In a **new terminal**, run ngrok to expose your API:
```powershell
ngrok http 5189 --host-header="localhost:5189"
```

### ✅ Step 3: Copy the ngrok URL
From the ngrok output, copy the `https://` forwarding URL (e.g., `https://abc123.ngrok-free.app`)

### ✅ Step 4: Build Your Target URL
Add your API path to the ngrok URL:
```
https://abc123.ngrok-free.app/api/entra-connector/token-enrichment
```

### ✅ Step 5: Use in Azure Portal
Now you can use this URL in the Azure Portal when creating your Custom Authentication Extension!

---

## Important Notes

### ⚠️ ngrok Free Tier Limitations
- URL changes every time you restart ngrok
- 40 requests/minute limit
- Session expires after 2 hours (free tier)

**For production:** Deploy to Azure App Service for a permanent URL.

### 🔒 Security Note
The endpoint is `[AllowAnonymous]` in your controller, which is correct for Entra's authentication. Entra will authenticate using:
- Bearer token in the Authorization header
- Or HTTP Basic auth (username/password from appsettings.json)

---

## Quick Reference

| What | Value |
|------|-------|
| **Local API** | `http://localhost:5189` |
| **API Endpoint** | `/api/entra-connector/token-enrichment` |
| **Full Local URL** | `http://localhost:5189/api/entra-connector/token-enrichment` |
| **ngrok URL** | `https://[your-id].ngrok-free.app` (after running ngrok) |
| **Target URL for Azure** | `https://[your-id].ngrok-free.app/api/entra-connector/token-enrichment` |

---

## Troubleshooting

### Issue: "Failed to complete tunnel connection"
- Make sure your API is running on port 5189
- Try running ngrok with `--host-header` flag

### Issue: "502 Bad Gateway"
- Verify your API is running
- Check the port number matches (5189)

### Issue: ngrok URL not working
- Make sure you're using the `https://` URL, not `http://`
- Check that ngrok is still running
- Try accessing the ngrok web interface at `http://127.0.0.1:4040`

---

## Ready to Continue?

Once you have your ngrok URL, you can proceed with creating the Custom Authentication Extension in Azure Portal using that URL as your **Target URL**!
