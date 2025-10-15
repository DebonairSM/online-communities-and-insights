using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using OnlineCommunities.Api.Authorization.Handlers;
using OnlineCommunities.Api.Authorization.Requirements;
using OnlineCommunities.Application.Interfaces;
using OnlineCommunities.Application.Services.Identity;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// AUTHENTICATION - Dual JWT Bearer (Entra External ID + Legacy) + OAuth Social Login
// ============================================================================
var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] 
    ?? throw new InvalidOperationException("JwtSettings:SecretKey must be configured");

// Get Entra External ID configuration
var entraInstance = builder.Configuration["AzureAd:Instance"];
var entraTenantId = builder.Configuration["AzureAd:TenantId"];
var entraClientId = builder.Configuration["AzureAd:ClientId"];
var entraAudience = builder.Configuration["AzureAd:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
// Entra External ID JWT Bearer - for API access after Entra authentication
.AddJwtBearer("Entra", options =>
{
    if (!string.IsNullOrEmpty(entraInstance) && !string.IsNullOrEmpty(entraTenantId))
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
    }
})
// Legacy JWT Bearer - for backward compatibility during migration period
.AddJwtBearer("Legacy", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
})
// Google OAuth
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    options.CallbackPath = "/api/auth/callback/Google";
    options.Scope.Add("profile");
    options.Scope.Add("email");
})
// GitHub OAuth
.AddGitHub(options =>
{
    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? "";
    options.CallbackPath = "/api/auth/callback/GitHub";
    options.Scope.Add("user:email");
})
// Microsoft Personal Accounts (Outlook, Hotmail) - NOT Entra ID!
.AddMicrosoftAccount(options =>
{
    options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"] ?? "";
    options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"] ?? "";
    options.CallbackPath = "/api/auth/callback/Microsoft";
});

// ============================================================================
// AUTHORIZATION - Custom Policies for Multi-Tenant SaaS with Dual Authentication
// ============================================================================
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("RequireModerator", policy =>
        policy.Requirements.Add(new TenantRoleRequirement("Moderator")))
    .AddPolicy("RequireAdmin", policy =>
        policy.Requirements.Add(new TenantRoleRequirement("Admin")))
    .AddPolicy("RequireTenantMembership", policy =>
        policy.Requirements.Add(new TenantMembershipRequirement()));

// Configure default policy to accept both Entra and Legacy tokens during migration
builder.Services.Configure<AuthorizationOptions>(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Entra", "Legacy")
        .Build();
});

// Register authorization handlers (these check YOUR database, not Entra ID)
builder.Services.AddScoped<IAuthorizationHandler, TenantRoleHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantMembershipHandler>();
builder.Services.AddScoped<IAuthorizationHandler, ResourceOwnerHandler>();

// ============================================================================
// APPLICATION SERVICES
// ============================================================================
// External Authentication Service - Handles OAuth social login
builder.Services.AddScoped<IExternalAuthService, ExternalAuthService>();

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
app.UseAuthentication();  // Validates JWT tokens + handles OAuth redirects
app.UseAuthorization();   // Checks policies and requirements

app.MapControllers();

// Temporary health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
    .WithName("HealthCheck")
    .AllowAnonymous();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
