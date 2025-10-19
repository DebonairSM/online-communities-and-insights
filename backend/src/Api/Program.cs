using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OnlineCommunities.Api.Authorization.Handlers;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Api.Extensions;
using OnlineCommunities.Api.Hubs;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Application.Services.Identity;
using OnlineCommunities.Core.Interfaces;
using OnlineCommunities.Infrastructure.Data;
using OnlineCommunities.Infrastructure.Repositories;
using OnlineCommunities.Infrastructure.Messaging;
using OnlineCommunities.Infrastructure.Integrations.Email;
using OnlineCommunities.Infrastructure.Integrations.CDM;
using OnlineCommunities.Infrastructure.Governance;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// AUTHENTICATION - Microsoft Entra External ID Only
// ============================================================================

// Get Entra External ID configuration
var entraInstance = builder.Configuration["AzureAd:Instance"];
var entraTenantId = builder.Configuration["AzureAd:TenantId"];
var entraClientId = builder.Configuration["AzureAd:ClientId"];
var entraAudience = builder.Configuration["AzureAd:Audience"];

if (string.IsNullOrEmpty(entraInstance) || string.IsNullOrEmpty(entraTenantId) || string.IsNullOrEmpty(entraAudience))
{
    throw new InvalidOperationException("AzureAd configuration is required: Instance, TenantId, and Audience must be set");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Authority = $"{entraInstance}{entraTenantId}/v2.0";
    options.Audience = entraAudience;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = options.Authority,
        ValidAudiences = new[] { entraAudience },
        ClockSkew = TimeSpan.FromMinutes(5) // Allow some clock skew for Entra
    };
});

// ============================================================================
// AUTHORIZATION - Custom Policies for Multi-Tenant SaaS
// ============================================================================
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireModerator", policy =>
        policy.Requirements.Add(new TenantRoleRequirement("Moderator")))
    .AddPolicy("RequireAdmin", policy =>
        policy.Requirements.Add(new TenantRoleRequirement("Admin")))
    .AddPolicy("RequireTenantMembership", policy =>
        policy.Requirements.Add(new TenantMembershipRequirement()));

// Configure default policy to require authenticated users
builder.Services.Configure<AuthorizationOptions>(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Register authorization handlers (these check YOUR database, not Entra ID)
builder.Services.AddScoped<IAuthorizationHandler, TenantRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantMembershipHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerHandler>();

// ============================================================================
// DATABASE CONFIGURATION
// ============================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ============================================================================
// APPLICATION SERVICES
// ============================================================================
// CQRS Mediator - Handles command and query dispatching
builder.Services.AddMediator();

// Entra User Sync Service - Handles JIT provisioning and bidirectional sync
builder.Services.AddScoped<IEntraUserSyncService, EntraUserSyncService>();

// Role Management Service - Required by authorization handlers
builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();

// Repository implementations (required for authentication)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

// ============================================================================
// INFRASTRUCTURE SERVICES
// ============================================================================
builder.Services.AddHttpContextAccessor(); // Required for authorization handlers

// SignalR for real-time communication
builder.Services.AddSignalR();

// CORS configuration for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",  // Vite default
            "http://localhost:3000"   // CRA default
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();  // Required for SignalR
    });
});

builder.Services.AddControllers();

// Bind EntraConnector options with validation and fail-fast behavior
builder.Services.AddOptions<OnlineCommunities.Api.Configuration.EntraConnectorOptions>()
    .Bind(builder.Configuration.GetSection(OnlineCommunities.Api.Configuration.EntraConnectorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart(); // Fail at startup if credentials are missing/weak

builder.Services.AddScoped<OnlineCommunities.Api.Filters.EntraConnectorBasicAuthFilter>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 9 uses OpenAPI instead of Swagger

// ============================================================================
// MESSAGING AND NOTIFICATIONS
// ============================================================================
// Service Bus for event-driven architecture
builder.Services.AddSingleton<IMessageBusService, ServiceBusService>();
builder.Services.Configure<ServiceBusOptions>(builder.Configuration.GetSection(ServiceBusOptions.SectionName));

// Notification orchestration service
builder.Services.AddScoped<INotificationService, NotificationOrchestrator>();

// Email service integration
builder.Services.AddScoped<IEmailService, SendGridService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

// ============================================================================
// PM FLOW AND IDEMPOTENCY
// ============================================================================
// Idempotency service for message processing
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();

// CDM ingestion service
builder.Services.AddScoped<ICDMIngestionService, CDMIngestionService>();

// ============================================================================
// GOVERNANCE SERVICES
// ============================================================================
// Azure resource naming and tagging
builder.Services.AddSingleton<AzureResourceNamer>();
builder.Services.AddScoped<TaggingService>();

// Certificate rotation service
builder.Services.AddScoped<CertificateRotationService>();
builder.Services.Configure<CertificateOptions>(builder.Configuration.GetSection(CertificateOptions.SectionName));

// TODO: Add these when ready:
// - DbContext configuration
// - Repository registrations
// - Caching (Redis)
// - Logging (Serilog)

// ============================================================================
// BUILD APPLICATION
// ============================================================================
var app = builder.Build();

// ============================================================================
// MIDDLEWARE PIPELINE
// ============================================================================
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // .NET 9 OpenAPI endpoint
}

// Enable CORS before other middleware
app.UseCors();

app.UseHttpsRedirection();

// TODO: Add custom middleware when implemented:
// app.UseMiddleware<TenantContextMiddleware>();      // Resolves tenant from request
// app.UseMiddleware<TokenBlacklistMiddleware>();     // Checks for revoked tokens
// app.UseMiddleware<ExceptionHandlingMiddleware>();  // Global error handling

// CRITICAL: Authentication must come before Authorization
app.UseAuthentication();  // Validates Entra External ID JWT tokens
app.UseAuthorization();   // Checks policies and requirements

app.MapControllers();

// Map SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");

// Landing page endpoint
app.MapGet("/", () => Results.Json(new 
{ 
    message = "Online Communities API", 
    version = "1.0.0",
    authentication = "Microsoft Entra External ID",
    endpoints = new
    {
        health = "/health",
        auth = "/api/auth",
        openapi = "/openapi/v1.json"
    },
    timestamp = DateTime.UtcNow 
}))
    .WithName("LandingPage")
    .AllowAnonymous();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
