# Tenant Management & Billing

## Tenant Lifecycle

### Tenant Provisioning

**Onboarding Flow**:
1. Sales team creates tenant record in CRM
2. Admin triggers provisioning workflow
3. System creates tenant infrastructure
4. Initial configuration applied
5. Welcome email sent to tenant admin
6. Admin completes setup wizard

**Automated Provisioning Service**:
```csharp
public class TenantProvisioningService
{
    public async Task<Tenant> ProvisionTenant(ProvisionTenantRequest request)
    {
        // 1. Create tenant record
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName,
            Domain = GenerateSubdomain(request.OrganizationName),
            Status = TenantStatus.Provisioning,
            SubscriptionTier = request.Tier,
            CreatedAt = DateTime.UtcNow
        };
        
        await _tenantRepository.Create(tenant);
        
        // 2. Create database schema or tables with tenant discriminator
        await CreateTenantSchema(tenant.Id);
        
        // 3. Create default community
        var defaultCommunity = new Community
        {
            TenantId = tenant.Id,
            Name = "General",
            Description = "Main community space",
            Status = CommunityStatus.Active
        };
        await _communityRepository.Create(defaultCommunity);
        
        // 4. Create admin user
        var adminUser = new User
        {
            Email = request.AdminEmail,
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            EmailVerified = false
        };
        await _userRepository.Create(adminUser);
        
        // 5. Assign admin role
        await _roleService.AssignRole(adminUser.Id, tenant.Id, "TenantAdmin");
        
        // 6. Apply default configuration
        await ApplyDefaultConfiguration(tenant.Id);
        
        // 7. Set up billing account
        await _billingService.CreateBillingAccount(tenant.Id, request.BillingInfo);
        
        // 8. Send welcome email
        await _emailService.SendWelcomeEmail(adminUser.Email, tenant.Id);
        
        // 9. Update status
        tenant.Status = TenantStatus.Active;
        await _tenantRepository.Update(tenant);
        
        // 10. Log provisioning event
        await _auditService.Log(new AuditLog
        {
            TenantId = tenant.Id,
            Action = "tenant.provisioned",
            EntityType = "Tenant",
            EntityId = tenant.Id,
            Timestamp = DateTime.UtcNow
        });
        
        return tenant;
    }
    
    private async Task CreateTenantSchema(Guid tenantId)
    {
        // Option 1: Shared database with tenant discriminator (recommended)
        // No additional setup needed - global query filters handle isolation
        
        // Option 2: Separate schema per tenant
        await _dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE SCHEMA [{tenantId}] AUTHORIZATION dbo"
        );
        
        // Option 3: Separate database per tenant (high-isolation scenarios)
        await _dbContext.Database.ExecuteSqlRawAsync(
            $"CREATE DATABASE [Tenant_{tenantId}]"
        );
    }
    
    private string GenerateSubdomain(string organizationName)
    {
        // Convert "Acme Corporation" to "acme"
        var subdomain = Regex.Replace(organizationName.ToLower(), @"[^a-z0-9]", "");
        
        // Ensure uniqueness
        var attempt = 0;
        var candidateSubdomain = subdomain;
        while (await _tenantRepository.ExistsBySubdomain(candidateSubdomain))
        {
            attempt++;
            candidateSubdomain = $"{subdomain}{attempt}";
        }
        
        return candidateSubdomain;
    }
}
```

**Provisioning Configuration**:
```json
{
  "defaultCommunities": [
    {
      "name": "General",
      "description": "Main community space"
    }
  ],
  "defaultRoles": [
    "TenantAdmin",
    "Moderator",
    "Member"
  ],
  "defaultFeatureFlags": {
    "surveys.enabled": true,
    "polls.enabled": true,
    "analytics.enabled": true,
    "customBranding.enabled": true
  },
  "resourceQuotas": {
    "maxCommunities": 10,
    "maxMembers": 1000,
    "storageGB": 50
  }
}
```

### Tenant Configuration

**Setup Wizard** (completed by tenant admin):

**Step 1: Branding**
- Upload logo
- Choose primary and secondary colors
- Set font preferences
- Upload favicon

**Step 2: Initial Community**
- Community name and description
- Visibility settings (public/private)
- Invite initial members

**Step 3: User Management**
- Invite additional admins and moderators
- Configure SSO (optional)
- Set member approval requirements

**Step 4: Features**
- Enable/disable surveys, polls, analytics
- Configure email notifications
- Set moderation policies

**Step 5: Integrations**
- Connect CRM (Salesforce, HubSpot, etc.)
- Connect marketing automation
- Set up webhooks

**Configuration Service**:
```csharp
public class TenantConfigurationService
{
    public async Task UpdateBranding(Guid tenantId, BrandingConfig branding)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        
        // Upload logo to blob storage
        if (branding.LogoFile != null)
        {
            var logoUrl = await _storageService.UploadFile(
                branding.LogoFile,
                $"tenants/{tenantId}/logo.png"
            );
            branding.LogoUrl = logoUrl;
        }
        
        tenant.BrandingConfig = JsonSerializer.Serialize(branding);
        await _tenantRepository.Update(tenant);
        
        // Invalidate CDN cache for tenant assets
        await _cdnService.PurgeCache($"tenants/{tenantId}/*");
    }
    
    public async Task UpdateSettings(Guid tenantId, TenantSettings settings)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        tenant.Settings = JsonSerializer.Serialize(settings);
        await _tenantRepository.Update(tenant);
        
        // Broadcast settings update to all connected clients
        await _hubContext.Clients.Group(tenantId.ToString())
            .SendAsync("SettingsUpdated", settings);
    }
}
```

### Tenant Deactivation

**Deactivation Process**:
1. Admin initiates deactivation (voluntary or involuntary)
2. Tenant status set to `Deactivating`
3. All users notified of pending deactivation
4. Grace period (30 days for voluntary, immediate for non-payment)
5. Tenant status set to `Inactive`
6. Users cannot log in, content becomes read-only
7. Data export provided to tenant admin
8. After 90 days, data marked for deletion

**Deactivation Service**:
```csharp
public class TenantDeactivationService
{
    public async Task DeactivateTenant(Guid tenantId, DeactivationReason reason)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        
        tenant.Status = TenantStatus.Deactivating;
        tenant.DeactivationReason = reason;
        tenant.DeactivationInitiatedAt = DateTime.UtcNow;
        
        if (reason == DeactivationReason.NonPayment)
        {
            tenant.DeactivationEffectiveAt = DateTime.UtcNow.AddDays(7); // 7-day grace
        }
        else
        {
            tenant.DeactivationEffectiveAt = DateTime.UtcNow.AddDays(30); // 30-day grace
        }
        
        await _tenantRepository.Update(tenant);
        
        // Notify all users
        var users = await _userRepository.GetByTenant(tenantId);
        foreach (var user in users)
        {
            await _emailService.SendDeactivationNotice(user.Email, tenant);
        }
        
        // Schedule final deactivation job
        _backgroundJobClient.Schedule(
            () => FinalizeDeactivation(tenantId),
            tenant.DeactivationEffectiveAt.Value
        );
    }
    
    public async Task FinalizeDeactivation(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        
        tenant.Status = TenantStatus.Inactive;
        tenant.DeactivatedAt = DateTime.UtcNow;
        await _tenantRepository.Update(tenant);
        
        // Revoke all user sessions
        await _sessionService.RevokeAllTenantSessions(tenantId);
        
        // Generate data export
        var exportUrl = await _dataExportService.GenerateFullExport(tenantId);
        
        // Send final email with export link
        var adminUsers = await _userRepository.GetTenantAdmins(tenantId);
        foreach (var admin in adminUsers)
        {
            await _emailService.SendDataExport(admin.Email, exportUrl);
        }
        
        // Schedule data deletion (90 days)
        _backgroundJobClient.Schedule(
            () => DeleteTenantData(tenantId),
            DateTimeOffset.UtcNow.AddDays(90)
        );
    }
}
```

## Billing and Subscription Management

### Billing Models for Research Communities

**Model 1: Campaign-Based Billing**

For project-based research communities:

```csharp
public class ResearchCampaign
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CampaignStatus Status { get; set; }
    
    // Research task quotas
    public int ParticipantQuota { get; set; }
    public int SurveyQuota { get; set; }
    public int VideoDiaryQuota { get; set; }
    public int IdInterviewQuota { get; set; }
    public int FocusGroupQuota { get; set; }
    
    // Budget and costs
    public decimal BudgetAmount { get; set; }
    public decimal ActualSpend { get; set; }
    public decimal IncentiveBudget { get; set; }
    public decimal ModeratorBudget { get; set; }
}

public class CampaignBillingService
{
    public async Task<decimal> CalculateCampaignCost(Guid campaignId)
    {
        var campaign = await _campaignRepository.GetById(campaignId);
        
        decimal totalCost = 0;
        
        // Base community setup fee
        totalCost += 1000; // $1,000 setup
        
        // Participant fees
        var participantCount = await _participantRepository.CountByCampaign(campaignId);
        totalCost += participantCount * 25; // $25 per participant
        
        // Task fees
        var surveyCount = await _surveyRepository.CountByCampaign(campaignId);
        totalCost += surveyCount * 50; // $50 per survey
        
        var diaryTaskCount = await _researchTaskRepository.CountByCampaignAndType(
            campaignId,
            ResearchTaskType.VideoDiary
        );
        totalCost += diaryTaskCount * 150; // $150 per video diary task
        
        var idiCount = await _interviewRepository.CountByCampaign(campaignId);
        totalCost += idiCount * 300; // $300 per IDI
        
        var focusGroupCount = await _focusGroupRepository.CountByCampaign(campaignId);
        totalCost += focusGroupCount * 800; // $800 per focus group
        
        // Moderation hours
        var moderationHours = await _moderationLogRepository.GetTotalHours(campaignId);
        totalCost += moderationHours * 75; // $75 per moderation hour
        
        // Storage fees (video + media)
        var storageGB = await _storageService.GetCampaignUsageGB(campaignId);
        totalCost += storageGB * 0.50m; // $0.50 per GB per month
        
        // AI services (transcription, sentiment, etc.)
        var transcriptionMinutes = await _transcriptionService.GetTotalMinutes(campaignId);
        totalCost += transcriptionMinutes * 0.10m; // $0.10 per minute
        
        return totalCost;
    }
}
```

**Model 2: Always-On Community (Subscription)**

For long-term insight communities:

| Tier | Monthly Price | Participants | Research Tasks | Moderators | Storage | Support |
|------|--------------|--------------|----------------|------------|---------|---------|
| **Starter** | $1,999 | 200 | 10 surveys, 5 qual tasks | 1 | 100 GB | Email |
| **Professional** | $4,999 | 500 | 30 surveys, 20 qual tasks, 5 IDIs | 3 | 500 GB | Priority |
| **Enterprise** | $12,999 | 2,000 | Unlimited tasks, 20 IDIs, 10 FGs | 10 | 2 TB | Dedicated |
| **Custom** | Custom | Unlimited | Custom quotas | Unlimited | Custom | White-glove |

**Research-Specific Add-Ons**:
- Additional participants: $10 per participant/month
- Additional IDIs: $250 per interview
- Additional focus groups: $600 per session
- Advanced AI analysis (theme clustering, NLP): $500/month
- Incentive management platform: $300/month
- White-label reports: $200/month
- Research consultant hours: $150/hour

**Billing Service**:
```csharp
public class BillingService
{
    public async Task<BillingAccount> CreateBillingAccount(
        Guid tenantId,
        BillingInfo billingInfo)
    {
        // Create Stripe customer
        var customerOptions = new CustomerCreateOptions
        {
            Email = billingInfo.Email,
            Name = billingInfo.CompanyName,
            Metadata = new Dictionary<string, string>
            {
                { "tenantId", tenantId.ToString() }
            }
        };
        
        var customer = await _stripeCustomerService.CreateAsync(customerOptions);
        
        // Store payment method
        if (billingInfo.PaymentMethodId != null)
        {
            await _stripePaymentMethodService.AttachAsync(
                billingInfo.PaymentMethodId,
                new PaymentMethodAttachOptions { Customer = customer.Id }
            );
            
            await _stripeCustomerService.UpdateAsync(customer.Id, new CustomerUpdateOptions
            {
                InvoiceSettings = new CustomerInvoiceSettingsOptions
                {
                    DefaultPaymentMethod = billingInfo.PaymentMethodId
                }
            });
        }
        
        // Create billing account record
        var billingAccount = new BillingAccount
        {
            TenantId = tenantId,
            StripeCustomerId = customer.Id,
            Email = billingInfo.Email,
            Status = BillingStatus.Active
        };
        
        await _billingAccountRepository.Create(billingAccount);
        
        return billingAccount;
    }
    
    public async Task CreateSubscription(
        Guid tenantId,
        SubscriptionTier tier,
        bool annualBilling = false)
    {
        var billingAccount = await _billingAccountRepository.GetByTenant(tenantId);
        
        var priceId = GetStripePriceId(tier, annualBilling);
        
        var subscriptionOptions = new SubscriptionCreateOptions
        {
            Customer = billingAccount.StripeCustomerId,
            Items = new List<SubscriptionItemOptions>
            {
                new SubscriptionItemOptions { Price = priceId }
            },
            Metadata = new Dictionary<string, string>
            {
                { "tenantId", tenantId.ToString() },
                { "tier", tier.ToString() }
            }
        };
        
        var subscription = await _stripeSubscriptionService.CreateAsync(subscriptionOptions);
        
        // Update tenant subscription
        var tenant = await _tenantRepository.GetById(tenantId);
        tenant.SubscriptionTier = tier;
        tenant.StripeSubscriptionId = subscription.Id;
        await _tenantRepository.Update(tenant);
    }
    
    public async Task CalculateMonthlyUsage(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var billingAccount = await _billingAccountRepository.GetByTenant(tenantId);
        
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        // Count members
        var memberCount = await _membershipRepository.CountByTenant(tenantId);
        var excessMembers = Math.Max(0, memberCount - limits.MaxMembers);
        
        // Calculate storage usage
        var storageGB = await _storageService.GetTenantUsageGB(tenantId);
        var excessStorage = Math.Max(0, storageGB - limits.StorageGB);
        
        // Create usage record
        var usage = new UsageRecord
        {
            TenantId = tenantId,
            BillingPeriodStart = GetBillingPeriodStart(),
            BillingPeriodEnd = GetBillingPeriodEnd(),
            MemberCount = memberCount,
            ExcessMembers = excessMembers,
            StorageGB = storageGB,
            ExcessStorageGB = excessStorage,
            TotalAmount = CalculateAmount(excessMembers, excessStorage)
        };
        
        await _usageRecordRepository.Create(usage);
        
        // Create invoice if there are overages
        if (usage.TotalAmount > 0)
        {
            await CreateUsageInvoice(tenant, usage);
        }
    }
    
    private async Task CreateUsageInvoice(Tenant tenant, UsageRecord usage)
    {
        var invoiceItems = new List<InvoiceItemCreateOptions>();
        
        if (usage.ExcessMembers > 0)
        {
            invoiceItems.Add(new InvoiceItemCreateOptions
            {
                Customer = tenant.StripeCustomerId,
                Description = $"Additional {usage.ExcessMembers} members",
                Amount = (long)(usage.ExcessMembers * 20), // $0.20 per member in cents
                Currency = "usd"
            });
        }
        
        if (usage.ExcessStorageGB > 0)
        {
            invoiceItems.Add(new InvoiceItemCreateOptions
            {
                Customer = tenant.StripeCustomerId,
                Description = $"Additional {usage.ExcessStorageGB} GB storage",
                Amount = (long)(usage.ExcessStorageGB * 10), // $0.10 per GB in cents
                Currency = "usd"
            });
        }
        
        foreach (var item in invoiceItems)
        {
            await _stripeInvoiceItemService.CreateAsync(item);
        }
        
        // Create and finalize invoice
        var invoice = await _stripeInvoiceService.CreateAsync(new InvoiceCreateOptions
        {
            Customer = tenant.StripeCustomerId,
            AutoAdvance = true
        });
        
        await _stripeInvoiceService.FinalizeInvoiceAsync(invoice.Id);
    }
}
```

### Payment Processing

**Stripe Webhook Handler**:
```csharp
public class StripeWebhookController : ControllerBase
{
    [HttpPost("webhooks/stripe")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"];
        
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _webhookSecret
            );
            
            switch (stripeEvent.Type)
            {
                case Events.CustomerSubscriptionCreated:
                case Events.CustomerSubscriptionUpdated:
                    await HandleSubscriptionChange(stripeEvent);
                    break;
                
                case Events.CustomerSubscriptionDeleted:
                    await HandleSubscriptionCancelled(stripeEvent);
                    break;
                
                case Events.InvoicePaymentSucceeded:
                    await HandlePaymentSucceeded(stripeEvent);
                    break;
                
                case Events.InvoicePaymentFailed:
                    await HandlePaymentFailed(stripeEvent);
                    break;
            }
            
            return Ok();
        }
        catch (StripeException)
        {
            return BadRequest();
        }
    }
    
    private async Task HandlePaymentFailed(Event stripeEvent)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        var tenantId = Guid.Parse(invoice.Metadata["tenantId"]);
        
        var tenant = await _tenantRepository.GetById(tenantId);
        tenant.PaymentStatus = PaymentStatus.Failed;
        await _tenantRepository.Update(tenant);
        
        // Notify tenant admin
        var admins = await _userRepository.GetTenantAdmins(tenantId);
        foreach (var admin in admins)
        {
            await _emailService.SendPaymentFailedNotice(admin.Email, tenant);
        }
        
        // Schedule suspension if not resolved in 7 days
        _backgroundJobClient.Schedule(
            () => _billingService.SuspendTenantForNonPayment(tenantId),
            TimeSpan.FromDays(7)
        );
    }
}
```

### Invoice Management

```csharp
public class InvoiceService
{
    public async Task<List<Invoice>> GetTenantInvoices(Guid tenantId)
    {
        var billingAccount = await _billingAccountRepository.GetByTenant(tenantId);
        
        var invoices = await _stripeInvoiceService.ListAsync(new InvoiceListOptions
        {
            Customer = billingAccount.StripeCustomerId,
            Limit = 100
        });
        
        return invoices.Select(i => new Invoice
        {
            Id = i.Id,
            Amount = i.AmountDue / 100m,
            Currency = i.Currency,
            Status = i.Status,
            DueDate = i.DueDate,
            PaidAt = i.StatusTransitions?.PaidAt,
            InvoiceUrl = i.InvoicePdf
        }).ToList();
    }
    
    public async Task<Stream> DownloadInvoice(Guid tenantId, string invoiceId)
    {
        var invoice = await _stripeInvoiceService.GetAsync(invoiceId);
        
        // Verify tenant owns this invoice
        var tenantIdFromInvoice = invoice.Metadata["tenantId"];
        if (tenantIdFromInvoice != tenantId.ToString())
        {
            throw new UnauthorizedException("Invoice does not belong to tenant");
        }
        
        // Download PDF from Stripe
        using var httpClient = new HttpClient();
        return await httpClient.GetStreamAsync(invoice.InvoicePdf);
    }
}
```

## Resource Isolation Per Tenant

### Database Isolation

**Shared Database with Discriminator** (recommended approach):
```csharp
public abstract class TenantEntity
{
    public Guid TenantId { get; set; }
}

public class Post : TenantEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
}

// Global query filter
modelBuilder.Entity<Post>().HasQueryFilter(p => p.TenantId == _currentTenantId);
```

**Benefits**:
- Cost-effective for large number of tenants
- Simple to manage and backup
- Easy to run cross-tenant analytics

**Drawbacks**:
- Noisy neighbor risk
- All tenants share same resource pool

### Storage Isolation

**Blob Storage Containers**:
```
/tenants/
  /{tenant-id}/
    /logos/
    /media/
      /posts/
      /avatars/
    /exports/
```

**Access Control**:
```csharp
public class TenantStorageService
{
    public async Task<string> UploadFile(Guid tenantId, IFormFile file, string path)
    {
        var containerName = $"tenant-{tenantId}";
        var container = _blobServiceClient.GetBlobContainerClient(containerName);
        
        await container.CreateIfNotExistsAsync(PublicAccessType.None);
        
        var blobClient = container.GetBlobClient(path);
        await blobClient.UploadAsync(file.OpenReadStream(), overwrite: true);
        
        // Generate SAS token for temporary access
        var sasToken = blobClient.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.AddHours(1)
        );
        
        return sasToken.ToString();
    }
}
```

### Cache Isolation

**Redis Key Prefixing**:
```csharp
public class TenantCacheService
{
    private readonly IDistributedCache _cache;
    private readonly Guid _tenantId;
    
    public async Task<T> GetAsync<T>(string key)
    {
        var prefixedKey = $"tenant:{_tenantId}:{key}";
        var value = await _cache.GetStringAsync(prefixedKey);
        
        return value == null ? default : JsonSerializer.Deserialize<T>(value);
    }
    
    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var prefixedKey = $"tenant:{_tenantId}:{key}";
        var serialized = JsonSerializer.Serialize(value);
        
        await _cache.SetStringAsync(prefixedKey, serialized, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }
}
```

## Multi-Tenancy Flow: Browser to Database

This section documents the complete end-to-end flow of multi-tenant request handling, from the browser through authentication, middleware, application services, data access, and finally to the database with composite keys.

### Architecture Overview

```
┌─────────────┐
│   Browser   │  MSAL.js authenticates user, stores JWT with TenantId claim
└──────┬──────┘
       │ HTTP Request with Authorization: Bearer <JWT>
       │ Optional: X-Tenant-Id header
       ▼
┌─────────────────────┐
│  Azure AD B2C       │  Issues JWT with custom claims (TenantId, roles)
│  (MSAL Auth)        │
└─────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core API                              │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 1. Authentication Middleware (Microsoft.Identity.Web)      │ │
│  │    - Validates JWT token                                   │ │
│  │    - Populates ClaimsPrincipal                             │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 2. Tenant Resolution Middleware                            │ │
│  │    - Extracts TenantId from claims or header               │ │
│  │    - Validates user's access to tenant                     │ │
│  │    - Populates ITenantContext                              │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 3. Controller Action                                       │ │
│  │    - Receives request                                      │ │
│  │    - Accesses ITenantContext.TenantId                      │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 4. Application Service                                     │ │
│  │    - Business logic                                        │ │
│  │    - Calls repositories                                    │ │
│  └────────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │ 5. Repository (TenantScopedRepository<T>)                  │ │
│  │    - Auto-injects TenantId                                 │ │
│  │    - All queries scoped to current tenant                  │ │
│  └────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────────┐
│  Entity Framework Core                                           │
│  - Global Query Filters (TenantId scoping)                       │
│  - Composite Key Configuration: (TenantId, Id)                   │
│  - Automatic TenantId injection on SaveChanges                   │
└─────────────────────────────────────────────────────────────────┘
       │
       ▼
┌─────────────────────────────────────────────────────────────────┐
│  SQL Server Database                                             │
│  - Composite Primary Keys: (TenantId, Id)                        │
│  - Composite Foreign Keys: (TenantId, ReferencedId)              │
│  - Row-Level Security (RLS) - Defense in Depth                   │
│  - Indexes with TenantId as leading column                       │
└─────────────────────────────────────────────────────────────────┘
```

### Layer 1: Browser and Frontend (React/TypeScript)

**Authentication with MSAL.js**

```typescript
// msal-config.ts
import { Configuration, PublicClientApplication } from "@azure/msal-browser";

const msalConfig: Configuration = {
  auth: {
    clientId: process.env.REACT_APP_AZURE_CLIENT_ID,
    authority: `https://${process.env.REACT_APP_AZURE_B2C_DOMAIN}.b2clogin.com/${process.env.REACT_APP_AZURE_B2C_DOMAIN}.onmicrosoft.com/${process.env.REACT_APP_B2C_POLICY}`,
    knownAuthorities: [`${process.env.REACT_APP_AZURE_B2C_DOMAIN}.b2clogin.com`],
    redirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: "localStorage",
    storeAuthStateInCookie: false,
  }
};

export const msalInstance = new PublicClientApplication(msalConfig);
```

**Tenant Context Management**

```typescript
// tenant-context.tsx
import React, { createContext, useContext, useEffect, useState } from 'react';
import { useMsal } from '@azure/msal-react';

interface TenantContextData {
  tenantId: string | null;
  tenantName: string | null;
  tenantSlug: string | null;
}

const TenantContext = createContext<TenantContextData>(null);

export const TenantProvider: React.FC = ({ children }) => {
  const { accounts } = useMsal();
  const [tenantContext, setTenantContext] = useState<TenantContextData>(null);

  useEffect(() => {
    if (accounts.length > 0) {
      const account = accounts[0];
      // Extract custom claims from ID token
      const tenantId = account.idTokenClaims?.['extension_TenantId'] as string;
      const tenantName = account.idTokenClaims?.['extension_TenantName'] as string;
      
      setTenantContext({
        tenantId,
        tenantName,
        tenantSlug: window.location.hostname.split('.')[0] // e.g., acme.insightcommunity.com
      });
    }
  }, [accounts]);

  return (
    <TenantContext.Provider value={tenantContext}>
      {children}
    </TenantContext.Provider>
  );
};

export const useTenant = () => useContext(TenantContext);
```

**HTTP Client with Tenant Context**

```typescript
// api-client.ts
import axios from 'axios';
import { msalInstance } from './msal-config';

const apiClient = axios.create({
  baseURL: process.env.REACT_APP_API_BASE_URL,
});

// Request interceptor: Add JWT token to all requests
apiClient.interceptors.request.use(async (config) => {
  const accounts = msalInstance.getAllAccounts();
  
  if (accounts.length > 0) {
    const account = accounts[0];
    
    // Acquire token silently
    const tokenResponse = await msalInstance.acquireTokenSilent({
      scopes: [`https://${process.env.REACT_APP_AZURE_B2C_DOMAIN}.onmicrosoft.com/${process.env.REACT_APP_AZURE_CLIENT_ID}/access_as_user`],
      account,
    });
    
    // Add Authorization header with JWT
    config.headers.Authorization = `Bearer ${tokenResponse.accessToken}`;
    
    // Optional: Add explicit tenant header (for validation/routing)
    const tenantId = account.idTokenClaims?.['extension_TenantId'];
    if (tenantId) {
      config.headers['X-Tenant-Id'] = tenantId;
    }
  }
  
  return config;
});

export default apiClient;
```

**Example API Call**

```typescript
// services/community-service.ts
import apiClient from '../api-client';

export interface Community {
  tenantId: string;
  id: string;
  name: string;
  slug: string;
}

export const communityService = {
  async getAll(): Promise<Community[]> {
    // TenantId is automatically included in JWT and injected by backend
    const response = await apiClient.get('/api/v1/communities');
    return response.data;
  },
  
  async getById(id: string): Promise<Community> {
    // Backend will validate this community belongs to current tenant
    const response = await apiClient.get(`/api/v1/communities/${id}`);
    return response.data;
  },
  
  async create(data: { name: string; description: string }): Promise<Community> {
    // TenantId automatically injected by backend
    const response = await apiClient.post('/api/v1/communities', data);
    return response.data;
  }
};
```

### Layer 2: Authentication (OAuth 2.0 Social Login or Microsoft Entra External ID)

**Authentication Options**

**Option A (Current): OAuth 2.0 Social Login**
- JWT tokens generated by YOUR API (not external IdP)
- Custom claims added by your application:
  - `tenant_id` - The tenant GUID the user belongs to
  - `tenant_name` - Friendly tenant name
  - `role` - User's roles within the tenant

**Option B (Future - Phase 1): Microsoft Entra External ID** (formerly Azure AD B2C)
- Managed authentication service for consumer apps
- Custom attributes in Entra External ID:
  - `extension_TenantId` - The tenant GUID the user belongs to
  - `extension_TenantName` - Friendly tenant name  
  - `extension_TenantRoles` - User's roles within the tenant

**JWT Token Structure (OAuth 2.0 - Current)**

```json
{
  "iss": "OnlineCommunitiesAPI",
  "sub": "user-id-guid",
  "aud": "OnlineCommunitiesUsers",
  "exp": 1234567890,
  "iat": 1234567890,
  "email": "user@example.com",
  "jti": "unique-token-id",
  "auth_method": "Google",
  "user_id": "550e8400-e29b-41d4-a716-446655440000",
  "tenant_id": "tenant-id-guid",
  "role": ["Member", "Moderator"]
}
```

**Future (Microsoft Entra External ID):**
```json
{
  "iss": "https://yourinstance.ciamlogin.com/...",
  "sub": "external-id-subject",
  "extension_TenantId": "550e8400-e29b-41d4-a716-446655440000",
  "extension_TenantName": "Acme Corporation"
}
```

**Backend Token Validation Setup**

```csharp
// Program.cs or Startup.cs
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Microsoft Identity Web authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        
        // Custom token validation
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        // Event handlers for token validation
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                
                var tenantIdClaim = context.Principal?.FindFirst("extension_TenantId");
                if (tenantIdClaim == null)
                {
                    logger.LogWarning("Token missing TenantId claim");
                    context.Fail("Token missing required TenantId claim");
                    return;
                }
                
                // Optional: Validate tenant exists and is active
                var tenantService = context.HttpContext.RequestServices
                    .GetRequiredService<ITenantValidationService>();
                    
                var tenantId = Guid.Parse(tenantIdClaim.Value);
                if (!await tenantService.IsTenantActive(tenantId))
                {
                    logger.LogWarning("Tenant {TenantId} is not active", tenantId);
                    context.Fail("Tenant is not active");
                    return;
                }
                
                logger.LogInformation("Token validated for tenant {TenantId}", tenantId);
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            }
        };
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });
```

**appsettings.json Configuration**

```json
{
  "AzureAdB2C": {
    "Instance": "https://insightcommunity.b2clogin.com",
    "Domain": "insightcommunity.onmicrosoft.com",
    "ClientId": "client-id-here",
    "SignUpSignInPolicyId": "B2C_1_SignUpSignIn",
    "TenantId": "tenant-id-here"
  }
}
```

### Layer 3: API Middleware (ASP.NET Core)

**Tenant Context Interface**

```csharp
// Core/Interfaces/ITenantContext.cs
public interface ITenantContext
{
    Guid TenantId { get; }
    string TenantName { get; }
    bool IsResolved { get; }
}

// Infrastructure/MultiTenancy/TenantContext.cs
public class TenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; }
    public bool IsResolved => TenantId != Guid.Empty;
}
```

**Tenant Resolution Middleware**

```csharp
// Infrastructure/Middleware/TenantResolutionMiddleware.cs
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddleware(
        RequestDelegate next,
        ILogger<TenantResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantContext tenantContext,
        IUserTenantValidationService validationService)
    {
        // Skip tenant resolution for authentication endpoints
        if (context.Request.Path.StartsWithSegments("/api/auth") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        // Resolve tenant from JWT claims (primary method)
        var tenantIdFromClaim = context.User?.FindFirst("extension_TenantId")?.Value;
        
        if (!string.IsNullOrEmpty(tenantIdFromClaim) && Guid.TryParse(tenantIdFromClaim, out var tenantId))
        {
            // Optional: Validate against X-Tenant-Id header if present (defense in depth)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
            {
                if (Guid.TryParse(headerTenantId, out var headerTenant) && headerTenant != tenantId)
                {
                    _logger.LogWarning(
                        "Tenant ID mismatch: Claim={ClaimTenantId}, Header={HeaderTenantId}",
                        tenantId,
                        headerTenant);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Tenant ID mismatch" });
                    return;
                }
            }

            // Validate user has access to this tenant
            var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                var hasAccess = await validationService.ValidateUserTenantAccess(
                    Guid.Parse(userId),
                    tenantId);

                if (!hasAccess)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to access tenant {TenantId} without permission",
                        userId,
                        tenantId);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Access denied" });
                    return;
                }
            }

            // Set tenant context
            if (tenantContext is TenantContext mutableContext)
            {
                mutableContext.TenantId = tenantId;
                mutableContext.TenantName = context.User?.FindFirst("extension_TenantName")?.Value;
            }

            _logger.LogInformation(
                "Resolved tenant {TenantId} for request {Path}",
                tenantId,
                context.Request.Path);
        }
        else
        {
            _logger.LogWarning("Unable to resolve tenant for request {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not resolved" });
            return;
        }

        await _next(context);
    }
}
```

**Middleware Registration**

```csharp
// Program.cs
var app = builder.Build();

// Order matters!
app.UseHttpsRedirection();
app.UseRouting();

// 1. Authentication must come first
app.UseAuthentication();

// 2. Then authorization
app.UseAuthorization();

// 3. Then tenant resolution (after authentication)
app.UseMiddleware<TenantResolutionMiddleware>();

// 4. Then endpoint routing
app.MapControllers();

app.Run();
```

**Service Registration**

```csharp
// Program.cs or extension method
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IUserTenantValidationService, UserTenantValidationService>();
```

**Tenant-Scoped Authorization Attribute**

```csharp
// API/Attributes/RequireTenantAttribute.cs
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireTenantAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tenantContext = context.HttpContext.RequestServices
            .GetRequiredService<ITenantContext>();

        if (!tenantContext.IsResolved)
        {
            context.Result = new ObjectResult(new { error = "Tenant context not resolved" })
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}
```

### Layer 4: Controllers (API Endpoints)

**Base Controller with Tenant Context**

```csharp
// API/Controllers/TenantScopedController.cs
[ApiController]
[Authorize]
[RequireTenant]
public abstract class TenantScopedController : ControllerBase
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;

    protected TenantScopedController(
        ITenantContext tenantContext,
        ILogger logger)
    {
        TenantContext = tenantContext;
        Logger = logger;
    }

    protected Guid CurrentTenantId => TenantContext.TenantId;
    
    protected Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    
    protected string CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value;
}
```

**Example Controller**

```csharp
// API/Controllers/CommunitiesController.cs
[ApiController]
[Route("api/v1/communities")]
public class CommunitiesController : TenantScopedController
{
    private readonly ICommunityService _communityService;

    public CommunitiesController(
        ICommunityService communityService,
        ITenantContext tenantContext,
        ILogger<CommunitiesController> logger)
        : base(tenantContext, logger)
    {
        _communityService = communityService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommunityDto>>> GetAll()
    {
        // TenantId is automatically scoped by the service/repository layer
        var communities = await _communityService.GetAllAsync();
        return Ok(communities);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CommunityDto>> GetById(Guid id)
    {
        // Service will verify this community belongs to current tenant
        var community = await _communityService.GetByIdAsync(id);
        
        if (community == null)
        {
            return NotFound();
        }

        return Ok(community);
    }

    [HttpPost]
    public async Task<ActionResult<CommunityDto>> Create(CreateCommunityDto dto)
    {
        // TenantId is automatically injected by repository
        var community = await _communityService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = community.Id }, community);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CommunityDto>> Update(Guid id, UpdateCommunityDto dto)
    {
        // Service will verify this community belongs to current tenant before updating
        var community = await _communityService.UpdateAsync(id, dto);
        
        if (community == null)
        {
            return NotFound();
        }

        return Ok(community);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        // Service will verify this community belongs to current tenant before deleting
        var result = await _communityService.DeleteAsync(id);
        
        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
```

### Layer 5: Application Services

**Base Service with Tenant Context**

```csharp
// Application/Services/TenantScopedService.cs
public abstract class TenantScopedService
{
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;

    protected TenantScopedService(
        ITenantContext tenantContext,
        ILogger logger)
    {
        TenantContext = tenantContext;
        Logger = logger;
    }

    protected Guid CurrentTenantId => TenantContext.TenantId;

    protected void ValidateTenantOwnership<T>(T entity) where T : ITenantOwned
    {
        if (entity.TenantId != CurrentTenantId)
        {
            Logger.LogWarning(
                "Attempted to access entity {EntityType} belonging to tenant {EntityTenantId} from tenant {CurrentTenantId}",
                typeof(T).Name,
                entity.TenantId,
                CurrentTenantId);
            throw new UnauthorizedAccessException("Access denied to this resource");
        }
    }
}
```

**Example Service Implementation**

```csharp
// Application/Services/CommunityService.cs
public interface ICommunityService
{
    Task<List<CommunityDto>> GetAllAsync();
    Task<CommunityDto> GetByIdAsync(Guid id);
    Task<CommunityDto> CreateAsync(CreateCommunityDto dto);
    Task<CommunityDto> UpdateAsync(Guid id, UpdateCommunityDto dto);
    Task<bool> DeleteAsync(Guid id);
}

public class CommunityService : TenantScopedService, ICommunityService
{
    private readonly ICommunityRepository _communityRepository;
    private readonly IMapper _mapper;

    public CommunityService(
        ICommunityRepository communityRepository,
        IMapper mapper,
        ITenantContext tenantContext,
        ILogger<CommunityService> logger)
        : base(tenantContext, logger)
    {
        _communityRepository = communityRepository;
        _mapper = mapper;
    }

    public async Task<List<CommunityDto>> GetAllAsync()
    {
        // Repository automatically filters by CurrentTenantId
        var communities = await _communityRepository.GetAllAsync();
        return _mapper.Map<List<CommunityDto>>(communities);
    }

    public async Task<CommunityDto> GetByIdAsync(Guid id)
    {
        // Repository automatically filters by CurrentTenantId
        // If community doesn't exist or belongs to different tenant, returns null
        var community = await _communityRepository.GetByIdAsync(id);
        
        if (community == null)
        {
            return null;
        }

        // Additional validation (defense in depth)
        ValidateTenantOwnership(community);

        return _mapper.Map<CommunityDto>(community);
    }

    public async Task<CommunityDto> CreateAsync(CreateCommunityDto dto)
    {
        var community = _mapper.Map<Community>(dto);
        
        // TenantId is automatically set by repository
        await _communityRepository.AddAsync(community);

        Logger.LogInformation(
            "Created community {CommunityId} in tenant {TenantId}",
            community.Id,
            CurrentTenantId);

        return _mapper.Map<CommunityDto>(community);
    }

    public async Task<CommunityDto> UpdateAsync(Guid id, UpdateCommunityDto dto)
    {
        // Repository automatically filters by CurrentTenantId
        var community = await _communityRepository.GetByIdAsync(id);
        
        if (community == null)
        {
            return null;
        }

        // Additional validation
        ValidateTenantOwnership(community);

        _mapper.Map(dto, community);
        await _communityRepository.UpdateAsync(community);

        Logger.LogInformation(
            "Updated community {CommunityId} in tenant {TenantId}",
            community.Id,
            CurrentTenantId);

        return _mapper.Map<CommunityDto>(community);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var community = await _communityRepository.GetByIdAsync(id);
        
        if (community == null)
        {
            return false;
        }

        ValidateTenantOwnership(community);

        await _communityRepository.DeleteAsync(community);

        Logger.LogInformation(
            "Deleted community {CommunityId} from tenant {TenantId}",
            id,
            CurrentTenantId);

        return true;
    }
}
```

### Layer 6: Repository Pattern

**ITenantOwned Interface**

```csharp
// Core/Interfaces/ITenantOwned.cs
public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
```

**Base Repository with Tenant Scoping**

```csharp
// Infrastructure/Repositories/TenantScopedRepository.cs
public abstract class TenantScopedRepository<T> where T : class, ITenantOwned
{
    protected readonly ApplicationDbContext Context;
    protected readonly ITenantContext TenantContext;
    protected readonly ILogger Logger;
    protected readonly DbSet<T> DbSet;

    protected TenantScopedRepository(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger logger)
    {
        Context = context;
        TenantContext = tenantContext;
        Logger = logger;
        DbSet = context.Set<T>();
    }

    protected Guid CurrentTenantId => TenantContext.TenantId;

    // Get all entities for current tenant
    public virtual async Task<List<T>> GetAllAsync()
    {
        // Global query filter automatically applies TenantId filter
        return await DbSet.ToListAsync();
    }

    // Get by composite key (TenantId, Id)
    public virtual async Task<T> GetByIdAsync(Guid id)
    {
        // Global query filter automatically applies TenantId filter
        // Or explicit composite key lookup:
        return await DbSet.FindAsync(CurrentTenantId, id);
    }

    // Add with automatic TenantId injection
    public virtual async Task<T> AddAsync(T entity)
    {
        // Automatically set TenantId
        entity.TenantId = CurrentTenantId;

        await DbSet.AddAsync(entity);
        await Context.SaveChangesAsync();

        return entity;
    }

    // Update with tenant validation
    public virtual async Task<T> UpdateAsync(T entity)
    {
        // Validate tenant ownership
        if (entity.TenantId != CurrentTenantId)
        {
            Logger.LogWarning(
                "Attempted to update entity {EntityType} belonging to tenant {EntityTenantId} from tenant {CurrentTenantId}",
                typeof(T).Name,
                entity.TenantId,
                CurrentTenantId);
            throw new UnauthorizedAccessException("Cannot update entity from different tenant");
        }

        Context.Entry(entity).State = EntityState.Modified;
        await Context.SaveChangesAsync();

        return entity;
    }

    // Delete with tenant validation
    public virtual async Task DeleteAsync(T entity)
    {
        // Validate tenant ownership
        if (entity.TenantId != CurrentTenantId)
        {
            Logger.LogWarning(
                "Attempted to delete entity {EntityType} belonging to tenant {EntityTenantId} from tenant {CurrentTenantId}",
                typeof(T).Name,
                entity.TenantId,
                CurrentTenantId);
            throw new UnauthorizedAccessException("Cannot delete entity from different tenant");
        }

        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    // Query helper with automatic tenant scoping
    protected IQueryable<T> Query()
    {
        // Global query filter automatically applied
        return DbSet.AsQueryable();
    }
}
```

**Example Repository Implementation**

```csharp
// Infrastructure/Repositories/CommunityRepository.cs
public interface ICommunityRepository
{
    Task<List<Community>> GetAllAsync();
    Task<Community> GetByIdAsync(Guid id);
    Task<Community> GetBySlugAsync(string slug);
    Task<List<Community>> GetActiveAsync();
    Task<Community> AddAsync(Community community);
    Task<Community> UpdateAsync(Community community);
    Task DeleteAsync(Community community);
}

public class CommunityRepository : TenantScopedRepository<Community>, ICommunityRepository
{
    public CommunityRepository(
        ApplicationDbContext context,
        ITenantContext tenantContext,
        ILogger<CommunityRepository> logger)
        : base(context, tenantContext, logger)
    {
    }

    public async Task<Community> GetBySlugAsync(string slug)
    {
        // Global query filter automatically scopes to current tenant
        return await Query()
            .FirstOrDefaultAsync(c => c.Slug == slug);
    }

    public async Task<List<Community>> GetActiveAsync()
    {
        // Global query filter automatically scopes to current tenant
        return await Query()
            .Where(c => c.Status == CommunityStatus.Active)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // Inherited: GetAllAsync, GetByIdAsync, AddAsync, UpdateAsync, DeleteAsync
}
```

### Layer 7: Entity Framework Core Configuration

**Domain Entity with ITenantOwned**

```csharp
// Core/Entities/Community.cs
public class Community : ITenantOwned
{
    // Composite key part 1
    public Guid TenantId { get; set; }
    
    // Composite key part 2
    public Guid Id { get; set; }
    
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Description { get; set; }
    public CommunityStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<Post> Posts { get; set; }
    public virtual ICollection<Membership> Memberships { get; set; }
}

// Core/Entities/Post.cs
public class Post : ITenantOwned
{
    // Composite key part 1
    public Guid TenantId { get; set; }
    
    // Composite key part 2
    public Guid Id { get; set; }
    
    // Foreign key to Community (includes TenantId)
    public Guid CommunityId { get; set; }
    
    public string Title { get; set; }
    public string Content { get; set; }
    public Guid AuthorId { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public virtual Community Community { get; set; }
    public virtual User Author { get; set; }
}
```

**DbContext with Global Query Filters**

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ApplicationDbContext> _logger;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantContext tenantContext,
        ILogger<ApplicationDbContext> logger)
        : base(options)
    {
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public DbSet<Community> Communities { get; set; }
    public DbSet<Post> Posts { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Membership> Memberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Communities
        modelBuilder.Entity<Community>(entity =>
        {
            // Composite primary key
            entity.HasKey(c => new { c.TenantId, c.Id });

            // Unique constraint on Slug within tenant
            entity.HasIndex(c => new { c.TenantId, c.Slug })
                .IsUnique()
                .HasDatabaseName("IX_Communities_Tenant_Slug");

            // Index for queries
            entity.HasIndex(c => new { c.TenantId, c.Status })
                .HasDatabaseName("IX_Communities_Tenant_Status");

            // Global query filter - automatic tenant scoping
            entity.HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);

            // Property configurations
            entity.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(c => c.Slug)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(c => c.CreatedAt)
                .HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Configure Posts
        modelBuilder.Entity<Post>(entity =>
        {
            // Composite primary key
            entity.HasKey(p => new { p.TenantId, p.Id });

            // Composite foreign key to Community (includes TenantId)
            entity.HasOne(p => p.Community)
                .WithMany(c => c.Posts)
                .HasForeignKey(p => new { p.TenantId, p.CommunityId })
                .OnDelete(DeleteBehavior.Cascade);

            // Global query filter
            entity.HasQueryFilter(p => p.TenantId == _tenantContext.TenantId);

            // Indexes
            entity.HasIndex(p => new { p.TenantId, p.CommunityId })
                .HasDatabaseName("IX_Posts_Tenant_Community");

            entity.HasIndex(p => new { p.TenantId, p.CreatedAt })
                .HasDatabaseName("IX_Posts_Tenant_CreatedAt");
        });

        // Apply global query filters to all ITenantOwned entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantOwned.TenantId));
                var tenantId = Expression.Property(
                    Expression.Constant(_tenantContext),
                    nameof(ITenantContext.TenantId));
                var filter = Expression.Lambda(
                    Expression.Equal(property, tenantId),
                    parameter);

                entityType.SetQueryFilter(filter);
            }
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set TenantId on new entities
        var entries = ChangeTracker.Entries<ITenantOwned>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _tenantContext.TenantId;
                _logger.LogDebug(
                    "Auto-set TenantId {TenantId} on {EntityType}",
                    _tenantContext.TenantId,
                    entry.Entity.GetType().Name);
            }
        }

        // Validate all entities belong to current tenant
        var modifiedEntries = ChangeTracker.Entries<ITenantOwned>()
            .Where(e => e.State == EntityState.Modified || e.State == EntityState.Deleted);

        foreach (var entry in modifiedEntries)
        {
            if (entry.Entity.TenantId != _tenantContext.TenantId)
            {
                _logger.LogError(
                    "Attempted to modify entity {EntityType} belonging to tenant {EntityTenantId} from tenant {CurrentTenantId}",
                    entry.Entity.GetType().Name,
                    entry.Entity.TenantId,
                    _tenantContext.TenantId);
                throw new UnauthorizedAccessException(
                    $"Cannot modify entity belonging to different tenant");
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
```

**Migration Example**

```csharp
// Infrastructure/Data/Migrations/20250101000000_AddCommunityTable.cs
public partial class AddCommunityTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Communities",
            columns: table => new
            {
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWSEQUENTIALID()"),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                // Composite primary key
                table.PrimaryKey("PK_Communities", x => new { x.TenantId, x.Id });
            });

        migrationBuilder.CreateIndex(
            name: "IX_Communities_Tenant_Slug",
            table: "Communities",
            columns: new[] { "TenantId", "Slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Communities_Tenant_Status",
            table: "Communities",
            columns: new[] { "TenantId", "Status" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Communities");
    }
}
```

### Layer 8: Database Layer (SQL Server)

**Table Definitions with Composite Keys**

```sql
-- Communities table
CREATE TABLE Communities (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Status INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    
    -- Composite primary key
    CONSTRAINT PK_Communities PRIMARY KEY (TenantId, Id),
    
    -- Unique constraint on Slug within tenant
    CONSTRAINT UQ_Communities_Tenant_Slug UNIQUE (TenantId, Slug)
);

-- Indexes with TenantId as leading column
CREATE INDEX IX_Communities_Tenant_Status 
    ON Communities (TenantId, Status);

CREATE INDEX IX_Communities_Tenant_Name 
    ON Communities (TenantId, Name);

-- Posts table
CREATE TABLE Posts (
    TenantId UNIQUEIDENTIFIER NOT NULL,
    Id UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    CommunityId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX),
    AuthorId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2,
    
    -- Composite primary key
    CONSTRAINT PK_Posts PRIMARY KEY (TenantId, Id),
    
    -- Composite foreign key to Communities
    CONSTRAINT FK_Posts_Communities 
        FOREIGN KEY (TenantId, CommunityId)
        REFERENCES Communities (TenantId, Id)
        ON DELETE CASCADE
);

-- Indexes with TenantId as leading column
CREATE INDEX IX_Posts_Tenant_Community 
    ON Posts (TenantId, CommunityId);

CREATE INDEX IX_Posts_Tenant_CreatedAt 
    ON Posts (TenantId, CreatedAt DESC);

CREATE INDEX IX_Posts_Tenant_Author 
    ON Posts (TenantId, AuthorId);
```

**Row-Level Security (RLS) - Defense in Depth**

```sql
-- Create security policy function
CREATE FUNCTION dbo.fn_TenantFilter(@TenantId UNIQUEIDENTIFIER)
    RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS IsAccessible
    WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);
GO

-- Apply security policy to Communities
CREATE SECURITY POLICY dbo.TenantSecurityPolicy
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Communities,
    ADD BLOCK PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Communities AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Communities AFTER UPDATE
WITH (STATE = ON);
GO

-- Apply to Posts
ALTER SECURITY POLICY dbo.TenantSecurityPolicy
    ADD FILTER PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Posts,
    ADD BLOCK PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Posts AFTER INSERT,
    ADD BLOCK PREDICATE dbo.fn_TenantFilter(TenantId) ON dbo.Posts AFTER UPDATE;
GO
```

**Set Session Context in DbContext**

```csharp
// Infrastructure/Data/ApplicationDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Set session context for RLS
    if (_tenantContext.IsResolved)
    {
        await Database.ExecuteSqlRawAsync(
            "EXEC sp_set_session_context @key = N'TenantId', @value = @tenantId",
            new SqlParameter("@tenantId", _tenantContext.TenantId));
    }

    // ... rest of SaveChanges logic
    return await base.SaveChangesAsync(cancellationToken);
}
```

**Query Performance Considerations**

```sql
-- Good: Index seek on composite key
SELECT * FROM Posts 
WHERE TenantId = '550e8400-e29b-41d4-a716-446655440000' 
  AND CommunityId = 'a1b2c3d4-e5f6-7890-1234-567890abcdef';

-- Good: Index seek with TenantId first
SELECT * FROM Posts 
WHERE TenantId = '550e8400-e29b-41d4-a716-446655440000'
  AND CreatedAt > '2025-01-01'
ORDER BY CreatedAt DESC;

-- Bad: Query without TenantId (will not use indexes efficiently)
SELECT * FROM Posts WHERE Id = 'a1b2c3d4-e5f6-7890-1234-567890abcdef';
-- This should never happen with proper implementation!
```

### Libraries and Packages

**Frontend (React/TypeScript)**

```json
{
  "dependencies": {
    "@azure/msal-browser": "^3.0.0",
    "@azure/msal-react": "^2.0.0",
    "axios": "^1.6.0",
    "react": "^18.2.0",
    "react-dom": "^18.2.0",
    "react-router-dom": "^6.20.0"
  }
}
```

**Backend (ASP.NET Core 8.0)**

```xml
<ItemGroup>
  <!-- Authentication -->
  <PackageReference Include="Microsoft.Identity.Web" Version="2.15.0" />
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
  
  <!-- Entity Framework Core -->
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
  
  <!-- Logging and monitoring -->
  <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  <PackageReference Include="Serilog.Sinks.ApplicationInsights" Version="4.0.0" />
  
  <!-- Mapping -->
  <PackageReference Include="AutoMapper" Version="12.0.0" />
  <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.0" />
</ItemGroup>
```

### Security Considerations: Defense in Depth

The multi-tenancy implementation uses multiple layers of protection:

**Layer 1: Authentication**
- JWT token validation with MSAL
- Claims verification (TenantId presence and format)
- Token expiration and refresh handling

**Layer 2: Middleware**
- Explicit tenant resolution from JWT claims
- User-tenant relationship validation
- Header validation (X-Tenant-Id vs. claim)
- Request rejection if tenant not resolved

**Layer 3: Authorization**
- `[RequireTenant]` attribute on controllers
- User role validation within tenant context
- Resource ownership checks

**Layer 4: Application Logic**
- Service-level tenant validation
- `ValidateTenantOwnership()` checks
- Business rule enforcement

**Layer 5: Repository Pattern**
- Automatic TenantId injection on create
- Tenant validation on update/delete operations
- `ITenantOwned` interface enforcement

**Layer 6: EF Core**
- Global query filters (automatic WHERE TenantId = X)
- SaveChanges validation
- Composite key enforcement
- Navigation property restrictions

**Layer 7: Database**
- Composite primary keys prevent ID reuse across tenants
- Composite foreign keys enforce same-tenant relationships
- Row-Level Security (RLS) as final safety net
- Unique constraints scoped to tenant

**Layer 8: Monitoring and Auditing**
```csharp
// Infrastructure/Auditing/TenantAuditService.cs
public class TenantAuditService
{
    public async Task LogCrossTenantAttempt(
        Guid userId,
        Guid requestedTenantId,
        Guid actualTenantId,
        string resource)
    {
        _logger.LogWarning(
            "SECURITY: User {UserId} from tenant {ActualTenantId} attempted to access resource {Resource} in tenant {RequestedTenantId}",
            userId,
            actualTenantId,
            resource,
            requestedTenantId);

        // Store in audit log
        await _auditRepository.CreateAsync(new AuditLog
        {
            UserId = userId,
            TenantId = actualTenantId,
            Action = "cross_tenant_access_attempt",
            Resource = resource,
            Metadata = new Dictionary<string, object>
            {
                { "requestedTenantId", requestedTenantId },
                { "actualTenantId", actualTenantId }
            },
            Severity = AuditSeverity.High,
            Timestamp = DateTime.UtcNow
        });

        // Alert security team for repeated attempts
        // Implement rate limiting, account suspension, etc.
    }
}
```

### Testing Multi-Tenant Isolation

**Unit Test Example**

```csharp
// Tests/Infrastructure/Repositories/CommunityRepositoryTests.cs
public class CommunityRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_OnlyReturnsTenantCommunities()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        
        var dbContext = CreateInMemoryContext();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenant1);
        
        // Add communities for both tenants
        dbContext.Communities.AddRange(
            new Community { TenantId = tenant1, Id = Guid.NewGuid(), Name = "Tenant1 Community1" },
            new Community { TenantId = tenant1, Id = Guid.NewGuid(), Name = "Tenant1 Community2" },
            new Community { TenantId = tenant2, Id = Guid.NewGuid(), Name = "Tenant2 Community1" }
        );
        await dbContext.SaveChangesAsync();
        
        var repository = new CommunityRepository(dbContext, tenantContext.Object, Mock.Of<ILogger<CommunityRepository>>());
        
        // Act
        var communities = await repository.GetAllAsync();
        
        // Assert
        Assert.Equal(2, communities.Count);
        Assert.All(communities, c => Assert.Equal(tenant1, c.TenantId));
    }
    
    [Fact]
    public async Task UpdateAsync_ThrowsWhenDifferentTenant()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        
        var dbContext = CreateInMemoryContext();
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.Setup(x => x.TenantId).Returns(tenant1);
        
        var community = new Community 
        { 
            TenantId = tenant2, // Different tenant!
            Id = Guid.NewGuid(), 
            Name = "Test" 
        };
        
        var repository = new CommunityRepository(dbContext, tenantContext.Object, Mock.Of<ILogger<CommunityRepository>>());
        
        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => 
            repository.UpdateAsync(community));
    }
}
```

**Integration Test Example**

```csharp
// Tests/API/Controllers/CommunitiesControllerTests.cs
public class CommunitiesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetCommunities_OnlyReturnsCurrentTenantData()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        
        var token1 = GenerateJwtToken(tenant1Id);
        var token2 = GenerateJwtToken(tenant2Id);
        
        var client = _factory.CreateClient();
        
        // Create communities for tenant 1
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        await client.PostAsJsonAsync("/api/v1/communities", new { name = "Tenant1 Community" });
        
        // Create communities for tenant 2
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        await client.PostAsJsonAsync("/api/v1/communities", new { name = "Tenant2 Community" });
        
        // Act - Query as tenant 1
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var response = await client.GetAsync("/api/v1/communities");
        var communities = await response.Content.ReadFromJsonAsync<List<CommunityDto>>();
        
        // Assert
        Assert.Single(communities);
        Assert.Equal("Tenant1 Community", communities[0].Name);
        Assert.All(communities, c => Assert.Equal(tenant1Id, c.TenantId));
    }
}
```

### References

- [ADR-005: Shared Database with Composite Keys](../architecture/decisions/005-shared-db-composite-keys.md)
- [Backend Architecture: Multi-Tenant Strategy](./backend-architecture.md#multi-tenant-strategy)
- [Security Model: Tenant Isolation](./security-model.md#tenant-isolation)
- [Microsoft Identity Web Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)
- [MSAL.js Documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/msal-overview)
- [EF Core Global Query Filters](https://learn.microsoft.com/ef/core/querying/filters)
- [SQL Server Row-Level Security](https://learn.microsoft.com/sql/relational-databases/security/row-level-security)

## Tenant-Level Feature Toggles

### Feature Flag System

```csharp
public class FeatureFlag
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; } // Null for global flags
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public Dictionary<string, object> Config { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class FeatureFlagService
{
    public async Task<bool> IsEnabled(Guid tenantId, string featureName)
    {
        // Check tenant-specific flag first
        var tenantFlag = await _flagRepository.GetByTenantAndName(tenantId, featureName);
        if (tenantFlag != null)
        {
            if (tenantFlag.ExpiresAt.HasValue && tenantFlag.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }
            return tenantFlag.IsEnabled;
        }
        
        // Fall back to global flag
        var globalFlag = await _flagRepository.GetGlobalFlag(featureName);
        return globalFlag?.IsEnabled ?? false;
    }
    
    public async Task<T> GetConfig<T>(Guid tenantId, string featureName, string configKey)
    {
        var flag = await _flagRepository.GetByTenantAndName(tenantId, featureName);
        
        if (flag?.Config?.ContainsKey(configKey) == true)
        {
            return (T)Convert.ChangeType(flag.Config[configKey], typeof(T));
        }
        
        return default;
    }
}

// Usage in controllers
[HttpPost("api/v1/surveys")]
public async Task<IActionResult> CreateSurvey(CreateSurveyDto dto)
{
    if (!await _featureFlagService.IsEnabled(User.TenantId(), "surveys.enabled"))
    {
        return BadRequest("Surveys are not enabled for your account");
    }
    
    // Create survey...
}
```

### Usage Example:
```csharp
// Check if feature is enabled
if (await _featureFlags.IsEnabled(tenantId, "advanced-analytics"))
{
    // Show advanced analytics dashboard
}

// Get configuration value
var maxSurveyQuestions = await _featureFlags.GetConfig<int>(
    tenantId,
    "surveys.enabled",
    "maxQuestions"
) ?? 50;
```

## Admin Panel for Tenant Management

**Platform Admin Operations**:
- View all tenants
- Search and filter tenants
- View tenant details (subscription, usage, health)
- Suspend/reactivate tenant
- Change subscription tier
- View audit logs
- Impersonate tenant admin (with audit trail)

**Tenant Health Metrics**:
- Active users (DAU/MAU)
- Engagement rate
- Storage usage
- API usage
- Error rate
- Payment status
- Last activity date

**Admin Dashboard Query**:
```csharp
public class TenantAdminService
{
    public async Task<List<TenantHealthDto>> GetTenantHealth()
    {
        var tenants = await _tenantRepository.GetAll();
        
        var healthMetrics = new List<TenantHealthDto>();
        
        foreach (var tenant in tenants)
        {
            var dau = await _analyticsService.GetDAU(tenant.Id, DateTime.UtcNow);
            var mau = await _analyticsService.GetMAU(tenant.Id, DateTime.UtcNow);
            var storageGB = await _storageService.GetUsageGB(tenant.Id);
            var errorRate = await _monitoringService.GetErrorRate(tenant.Id);
            
            healthMetrics.Add(new TenantHealthDto
            {
                TenantId = tenant.Id,
                TenantName = tenant.Name,
                Status = tenant.Status,
                SubscriptionTier = tenant.SubscriptionTier,
                DAU = dau,
                MAU = mau,
                EngagementRate = dau / (double)mau,
                StorageGB = storageGB,
                ErrorRate = errorRate,
                LastActivityAt = await _analyticsService.GetLastActivity(tenant.Id),
                HealthScore = CalculateHealthScore(dau, mau, errorRate)
            });
        }
        
        return healthMetrics.OrderByDescending(t => t.HealthScore).ToList();
    }
}
```

## SLA Tracking

### Service Level Agreements

**Uptime SLAs by Tier**:
- Free: Best effort (no SLA)
- Standard: 99.5% uptime
- Premium: 99.9% uptime
- Enterprise: 99.95% uptime with custom terms

**SLA Monitoring**:
```csharp
public class SlaMonitoringService
{
    public async Task<SlaReport> GenerateMonthlyReport(Guid tenantId, DateTime month)
    {
        var startDate = new DateTime(month.Year, month.Month, 1);
        var endDate = startDate.AddMonths(1);
        
        // Query uptime data from Application Insights
        var uptimeMinutes = await _monitoringService.GetUptimeMinutes(
            tenantId,
            startDate,
            endDate
        );
        
        var totalMinutes = (endDate - startDate).TotalMinutes;
        var uptimePercentage = (uptimeMinutes / totalMinutes) * 100;
        
        var tenant = await _tenantRepository.GetById(tenantId);
        var slaTarget = GetSlaTarget(tenant.SubscriptionTier);
        
        var report = new SlaReport
        {
            TenantId = tenantId,
            Month = month,
            UptimePercentage = uptimePercentage,
            SlaTarget = slaTarget,
            SlaMet = uptimePercentage >= slaTarget,
            TotalDowntimeMinutes = totalMinutes - uptimeMinutes,
            Incidents = await _incidentRepository.GetByTenantAndPeriod(
                tenantId,
                startDate,
                endDate
            )
        };
        
        // If SLA not met, calculate credit
        if (!report.SlaMet && tenant.SubscriptionTier != SubscriptionTier.Free)
        {
            report.CreditPercentage = CalculateSlaCredit(
                slaTarget,
                uptimePercentage
            );
        }
        
        return report;
    }
    
    private double GetSlaTarget(SubscriptionTier tier)
    {
        return tier switch
        {
            SubscriptionTier.Standard => 99.5,
            SubscriptionTier.Premium => 99.9,
            SubscriptionTier.Enterprise => 99.95,
            _ => 0
        };
    }
    
    private double CalculateSlaCredit(double target, double actual)
    {
        // Example: For each 0.1% below SLA, credit 5% of monthly fee
        var shortfall = target - actual;
        return Math.Min(100, (shortfall / 0.1) * 5);
    }
}
```

## Resource Quotas and Limits

### Research-Specific Quotas

**Enforce Research Task Quotas**:
```csharp
public class ResearchQuotaEnforcementService
{
    public async Task<bool> CanCreateSurvey(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var currentMonthSurveys = await _surveyRepository.CountThisMonth(tenantId);
        
        return currentMonthSurveys < limits.MonthlySurveys;
    }
    
    public async Task<bool> CanCreateQualitativeTask(Guid tenantId, ResearchTaskType taskType)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var currentMonthTasks = await _researchTaskRepository.CountThisMonthByType(
            tenantId,
            taskType
        );
        
        return taskType switch
        {
            ResearchTaskType.VideoDiary => currentMonthTasks < limits.MonthlyVideoDiaries,
            ResearchTaskType.PhotoDiary => currentMonthTasks < limits.MonthlyPhotoDiaries,
            ResearchTaskType.Collage => currentMonthTasks < limits.MonthlyCollages,
            _ => false
        };
    }
    
    public async Task<bool> CanScheduleInterview(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var currentMonthInterviews = await _interviewRepository.CountThisMonth(tenantId);
        
        return currentMonthInterviews < limits.MonthlyIdInterviews;
    }
    
    public async Task<bool> CanScheduleFocusGroup(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var currentMonthFocusGroups = await _focusGroupRepository.CountThisMonth(tenantId);
        
        return currentMonthFocusGroups < limits.MonthlyFocusGroups;
    }
    
    public async Task<bool> CanRecruitParticipant(Guid tenantId)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var activeParticipants = await _participantRepository.CountActive(tenantId);
        
        return activeParticipants < limits.MaxParticipants;
    }
    
    public async Task<bool> CanUploadMedia(Guid tenantId, long fileSizeBytes)
    {
        var tenant = await _tenantRepository.GetById(tenantId);
        var limits = GetTierLimits(tenant.SubscriptionTier);
        
        var currentUsageGB = await _storageService.GetUsageGB(tenantId);
        var newUsageGB = currentUsageGB + (fileSizeBytes / 1_000_000_000.0);
        
        return newUsageGB <= limits.StorageGB;
    }
}
```

**Research Quota Monitoring**:
```csharp
public class ResearchQuotaMonitoringService
{
    public async Task CheckResearchQuotas()
    {
        var tenants = await _tenantRepository.GetActive();
        
        foreach (var tenant in tenants)
        {
            var limits = GetTierLimits(tenant.SubscriptionTier);
            var usage = await GetCurrentResearchUsage(tenant.Id);
            
            // Warn at 80% usage for research tasks
            if (usage.SurveyCount / (double)limits.MonthlySurveys >= 0.8)
            {
                await NotifyAdmins(
                    tenant.Id,
                    $"Survey quota at {usage.SurveyCount} of {limits.MonthlySurveys} this month"
                );
            }
            
            if (usage.IdInterviewCount / (double)limits.MonthlyIdInterviews >= 0.8)
            {
                await NotifyAdmins(
                    tenant.Id,
                    $"IDI quota at {usage.IdInterviewCount} of {limits.MonthlyIdInterviews} this month"
                );
            }
            
            if (usage.ParticipantCount / (double)limits.MaxParticipants >= 0.9)
            {
                await NotifyAdmins(
                    tenant.Id,
                    $"Participant count at {usage.ParticipantCount} of {limits.MaxParticipants} limit"
                );
            }
            
            if (usage.StorageGB / limits.StorageGB >= 0.8)
            {
                await NotifyAdmins(
                    tenant.Id,
                    $"Storage usage at {usage.StorageGB:F1} GB of {limits.StorageGB} GB limit"
                );
            }
        }
    }
}
```

## Research Contracts & Data Retention

### Research Contract Management

**Research Contracts** define the scope, timeline, and data handling for each insight community:

```csharp
public class ResearchContract
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string ContractNumber { get; set; }
    public string ClientName { get; set; }
    public string ProjectTitle { get; set; }
    
    // Dates
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? ExtendedEndDate { get; set; }
    
    // Participant consent and privacy
    public ConsentType RequiredConsentType { get; set; } // Explicit, Implied, Tiered
    public bool RequiresEthicsApproval { get; set; }
    public string EthicsApprovalNumber { get; set; }
    
    // Data retention
    public DataRetentionPolicy RetentionPolicy { get; set; }
    public int RetentionMonths { get; set; }
    public DateTime DataDeletionDate { get; set; }
    public bool AllowSecondaryResearch { get; set; } // Can data be reused?
    
    // Budget and quotas
    public decimal TotalBudget { get; set; }
    public int ParticipantQuota { get; set; }
    public int TaskQuota { get; set; }
    
    // Deliverables
    public List<ContractDeliverable> Deliverables { get; set; }
}

public class ContractDeliverable
{
    public Guid Id { get; set; }
    public string Name { get; set; } // e.g., "Final Insight Report", "Video Reel"
    public DateTime DueDate { get; set; }
    public DeliverableStatus Status { get; set; }
    public string FilePath { get; set; }
}

public enum DataRetentionPolicy
{
    DeleteAfterContract, // Delete data when contract ends
    RetainForPeriod, // Retain for specified months
    RetainIndefinitely, // Keep for future research (with consent)
    AnonymizeAndRetain // Remove PII but keep data
}

public class ResearchContractService
{
    public async Task<ResearchContract> CreateContract(CreateContractRequest request)
    {
        var contract = new ResearchContract
        {
            Id = Guid.NewGuid(),
            TenantId = request.TenantId,
            ContractNumber = GenerateContractNumber(),
            ClientName = request.ClientName,
            ProjectTitle = request.ProjectTitle,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RetentionPolicy = request.RetentionPolicy,
            RetentionMonths = request.RetentionMonths,
            DataDeletionDate = CalculateDataDeletionDate(request.EndDate, request.RetentionPolicy, request.RetentionMonths)
        };
        
        await _contractRepository.Create(contract);
        
        // Schedule data deletion job
        if (contract.RetentionPolicy != DataRetentionPolicy.RetainIndefinitely)
        {
            _backgroundJobClient.Schedule(
                () => ProcessDataRetention(contract.Id),
                contract.DataDeletionDate
            );
        }
        
        return contract;
    }
    
    public async Task ProcessDataRetention(Guid contractId)
    {
        var contract = await _contractRepository.GetById(contractId);
        var tenant = await _tenantRepository.GetById(contract.TenantId);
        
        switch (contract.RetentionPolicy)
        {
            case DataRetentionPolicy.DeleteAfterContract:
            case DataRetentionPolicy.RetainForPeriod:
                // Delete all research data for this contract
                await _dataRetentionService.DeleteContractData(contractId);
                break;
            
            case DataRetentionPolicy.AnonymizeAndRetain:
                // Remove PII but keep anonymized data
                await _dataRetentionService.AnonymizeContractData(contractId);
                break;
        }
        
        // Audit log
        await _auditService.Log(new AuditLog
        {
            TenantId = contract.TenantId,
            Action = "contract.data_retention_executed",
            EntityType = "ResearchContract",
            EntityId = contractId,
            Metadata = new Dictionary<string, object>
            {
                { "policy", contract.RetentionPolicy.ToString() },
                { "deletionDate", contract.DataDeletionDate }
            }
        });
    }
}
```

## Moderator Assignment & Management

### Moderator Assignment Per Tenant

**Moderator Profiles**:
```csharp
public class ModeratorProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; }
    
    // Qualifications
    public string Bio { get; set; }
    public List<string> Specializations { get; set; } // e.g., "Healthcare", "Technology", "Youth"
    public List<string> Languages { get; set; }
    public int YearsExperience { get; set; }
    
    // Availability
    public bool IsAvailable { get; set; }
    public int MaxActiveCommunities { get; set; }
    public int CurrentActiveCommunities { get; set; }
    
    // Performance metrics
    public double AverageResponseTime { get; set; } // Hours
    public double QualityScore { get; set; } // 0-100
    public int TotalCommunitiesModerated { get; set; }
}

public class ModeratorAssignment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ModeratorId { get; set; }
    public ModeratorProfile Moderator { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RemovedAt { get; set; }
    public ModeratorRole Role { get; set; } // Lead, Support, Observer
    public bool IsPrimary { get; set; }
}

public enum ModeratorRole
{
    Lead, // Primary moderator, owns community strategy
    Support, // Assists with moderation tasks
    Observer // Training role, read-only
}

public class ModeratorAssignmentService
{
    public async Task<ModeratorAssignment> AssignModerator(
        Guid tenantId,
        Guid moderatorId,
        ModeratorRole role)
    {
        var moderator = await _moderatorRepository.GetById(moderatorId);
        
        // Check availability
        if (moderator.CurrentActiveCommunities >= moderator.MaxActiveCommunities)
        {
            throw new InvalidOperationException("Moderator has reached maximum capacity");
        }
        
        // Check for existing assignment
        var existing = await _assignmentRepository.GetByTenantAndModerator(tenantId, moderatorId);
        if (existing != null && existing.RemovedAt == null)
        {
            throw new InvalidOperationException("Moderator already assigned to this community");
        }
        
        var assignment = new ModeratorAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ModeratorId = moderatorId,
            AssignedAt = DateTime.UtcNow,
            Role = role,
            IsPrimary = role == ModeratorRole.Lead
        };
        
        await _assignmentRepository.Create(assignment);
        
        // Update moderator workload
        moderator.CurrentActiveCommunities++;
        await _moderatorRepository.Update(moderator);
        
        // Notify moderator
        await _notificationService.NotifyModeratorAssigned(moderatorId, tenantId);
        
        // Grant tenant access
        await _roleService.AssignRole(moderator.UserId, tenantId, "Moderator");
        
        return assignment;
    }
    
    public async Task<List<ModeratorProfile>> FindAvailableModerators(
        List<string> requiredSpecializations = null,
        List<string> requiredLanguages = null)
    {
        var query = _moderatorRepository.Query()
            .Where(m => m.IsAvailable)
            .Where(m => m.CurrentActiveCommunities < m.MaxActiveCommunities);
        
        if (requiredSpecializations?.Any() == true)
        {
            query = query.Where(m => m.Specializations.Any(s => requiredSpecializations.Contains(s)));
        }
        
        if (requiredLanguages?.Any() == true)
        {
            query = query.Where(m => m.Languages.Any(l => requiredLanguages.Contains(l)));
        }
        
        return await query
            .OrderByDescending(m => m.QualityScore)
            .ThenBy(m => m.CurrentActiveCommunities)
            .ToListAsync();
    }
    
    public async Task RemoveModerator(Guid tenantId, Guid moderatorId, string reason)
    {
        var assignment = await _assignmentRepository.GetByTenantAndModerator(tenantId, moderatorId);
        
        if (assignment == null)
        {
            throw new InvalidOperationException("Moderator not assigned to this community");
        }
        
        assignment.RemovedAt = DateTime.UtcNow;
        await _assignmentRepository.Update(assignment);
        
        // Update moderator workload
        var moderator = await _moderatorRepository.GetById(moderatorId);
        moderator.CurrentActiveCommunities--;
        await _moderatorRepository.Update(moderator);
        
        // Revoke tenant access
        await _roleService.RemoveRole(moderator.UserId, tenantId, "Moderator");
        
        // Audit log
        await _auditService.Log(new AuditLog
        {
            TenantId = tenantId,
            Action = "moderator.removed",
            EntityType = "ModeratorAssignment",
            EntityId = assignment.Id,
            Metadata = new Dictionary<string, object>
            {
                { "moderatorId", moderatorId },
                { "reason", reason }
            }
        });
    }
}
```

### Moderator Workload Management

```csharp
public class ModeratorWorkloadService
{
    public async Task<ModeratorWorkloadReport> GetWorkloadReport(Guid moderatorId)
    {
        var assignments = await _assignmentRepository.GetActiveByModerator(moderatorId);
        
        var workload = new ModeratorWorkloadReport
        {
            ModeratorId = moderatorId,
            ActiveCommunities = assignments.Count
        };
        
        foreach (var assignment in assignments)
        {
            var pendingModeration = await _moderationQueueRepository.CountPending(assignment.TenantId);
            var avgDailyTasks = await _researchTaskRepository.GetAvgDailyTasks(assignment.TenantId);
            var participantCount = await _participantRepository.CountActive(assignment.TenantId);
            
            workload.Communities.Add(new CommunityWorkload
            {
                TenantId = assignment.TenantId,
                TenantName = (await _tenantRepository.GetById(assignment.TenantId)).Name,
                Role = assignment.Role,
                PendingModerationItems = pendingModeration,
                AvgDailyTasks = avgDailyTasks,
                ParticipantCount = participantCount,
                EstimatedHoursPerWeek = CalculateEstimatedHours(pendingModeration, avgDailyTasks, participantCount)
            });
        }
        
        workload.TotalEstimatedHoursPerWeek = workload.Communities.Sum(c => c.EstimatedHoursPerWeek);
        
        return workload;
    }
    
    private double CalculateEstimatedHours(int pendingItems, double avgDailyTasks, int participants)
    {
        // Rough estimates:
        // - 5 minutes per moderation item
        // - 10 minutes per research task to review
        // - 1 hour per 50 participants for engagement monitoring
        
        var moderationHours = (pendingItems * 5) / 60.0;
        var taskReviewHours = (avgDailyTasks * 7 * 10) / 60.0;
        var engagementHours = participants / 50.0;
        
        return moderationHours + taskReviewHours + engagementHours;
    }
}
```

