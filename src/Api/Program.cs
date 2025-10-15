using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OnlineCommunities.Api.Authorization.Handlers;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Application.Services.Identity;

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
// APPLICATION SERVICES
// ============================================================================
// Entra User Sync Service - Handles JIT provisioning and bidirectional sync
builder.Services.AddScoped<IEntraUserSyncService, EntraUserSyncService>();

// TODO: Register UserRepository implementation (required for authentication)
// builder.Services.AddScoped<OnlineCommunities.Core.Interfaces.IUserRepository, 
//     OnlineCommunities.Infrastructure.Repositories.UserRepository>();

// TODO: Add DbContext when ready:
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// TODO: Register other application services here
// builder.Services.AddApplicationServices(builder.Configuration);
// builder.Services.AddInfrastructureServices(builder.Configuration);

// ============================================================================
// INFRASTRUCTURE SERVICES
// ============================================================================
builder.Services.AddHttpContextAccessor(); // Required for authorization handlers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi(); // .NET 9 uses OpenAPI instead of Swagger

// TODO: Add these when ready:
// - DbContext configuration
// - Repository registrations
// - Service registrations
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

app.UseHttpsRedirection();

// TODO: Add custom middleware when implemented:
// app.UseMiddleware<TenantContextMiddleware>();      // Resolves tenant from request
// app.UseMiddleware<TokenBlacklistMiddleware>();     // Checks for revoked tokens
// app.UseMiddleware<ExceptionHandlingMiddleware>();  // Global error handling

// CRITICAL: Authentication must come before Authorization
app.UseAuthentication();  // Validates Entra External ID JWT tokens
app.UseAuthorization();   // Checks policies and requirements

app.MapControllers();

// Temporary health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
