# Phase 0: Foundation (2-3 weeks)

## Overview

This phase establishes the technical foundation for the Insight Community Platform. The goal is to set up infrastructure, development environment, and core authentication before building features.

**Timeline**: 2-3 weeks
**Team Size**: 2-3 developers (1 backend, 1 frontend, 1 DevOps/full-stack)

## Success Criteria

- [ ] Developers can run the application locally
- [ ] CI/CD pipelines automatically deploy to dev environment
- [ ] Users can register and authenticate via JWT
- [ ] Basic tenant management works
- [ ] Infrastructure is provisioned and monitored
- [ ] Team has established development workflows

---

## Milestone 1: Development Environment Setup (Days 1-3)

### 1.1 Repository Setup

**Acceptance Criteria**:
- [ ] GitHub repository created with branch protection
- [ ] Main branch requires PR approval
- [ ] `.gitignore` configured for .NET and Node.js
- [ ] `.editorconfig` established for consistent formatting
- [ ] README.md with setup instructions

**Tasks**:
```bash
# Create repository structure
online-communities-platform/
├── .github/
│   └── workflows/              # GitHub Actions
├── src/
│   ├── Api/                    # ASP.NET Core Web API
│   ├── Core/                   # Domain models
│   ├── Application/            # Business logic
│   ├── Infrastructure/         # Data access
│   └── Web/                    # React frontend
├── tests/
│   ├── Api.Tests/
│   ├── Application.Tests/
│   └── Integration.Tests/
├── infra/                      # Bicep templates
│   ├── modules/
│   └── environments/
└── docs/
```

**Jira Stories**:
- `FOUND-1`: Create GitHub repository and configure branch protection
- `FOUND-2`: Set up .NET solution structure with projects
- `FOUND-3`: Initialize React application with Vite
- `FOUND-4`: Create README with local setup instructions

---

### 1.2 .NET Backend Scaffolding

**Acceptance Criteria**:
- [ ] .NET 8 solution compiles successfully
- [ ] Projects follow clean architecture pattern
- [ ] Swagger/OpenAPI documentation generates
- [ ] Basic health check endpoint responds

**Technical Setup**:
```bash
# Create .NET solution
dotnet new sln -n OnlineCommunities

# Create projects
dotnet new webapi -n OnlineCommunities.Api -o src/Api
dotnet new classlib -n OnlineCommunities.Core -o src/Core
dotnet new classlib -n OnlineCommunities.Application -o src/Application
dotnet new classlib -n OnlineCommunities.Infrastructure -o src/Infrastructure

# Create test projects
dotnet new xunit -n OnlineCommunities.Api.Tests -o tests/Api.Tests
dotnet new xunit -n OnlineCommunities.Application.Tests -o tests/Application.Tests
dotnet new xunit -n OnlineCommunities.Integration.Tests -o tests/Integration.Tests

# Add projects to solution
dotnet sln add src/**/*.csproj
dotnet sln add tests/**/*.csproj
```

**Package Dependencies**:
- `Microsoft.EntityFrameworkCore` (8.0.x)
- `Microsoft.EntityFrameworkCore.SqlServer`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Serilog.AspNetCore`
- `Swashbuckle.AspNetCore`
- `StackExchange.Redis`

**Jira Stories**:
- `FOUND-5`: Create .NET solution with clean architecture structure
- `FOUND-6`: Add NuGet packages and configure Swagger
- `FOUND-7`: Set up Serilog structured logging
- `FOUND-8`: Create health check endpoint

---

### 1.3 React Frontend Scaffolding

**Acceptance Criteria**:
- [ ] React app runs on `localhost:5173`
- [ ] TypeScript configured with strict mode
- [ ] ESLint and Prettier configured
- [ ] Basic routing structure established
- [ ] Material-UI installed and themed

**Technical Setup**:
```bash
# Create Vite React app
npm create vite@latest src/Web -- --template react-ts

# Install dependencies
cd src/Web
npm install @mui/material @emotion/react @emotion/styled
npm install @reduxjs/toolkit react-redux
npm install react-router-dom
npm install axios
npm install @microsoft/signalr

# Dev dependencies
npm install -D @types/react @types/react-dom
npm install -D eslint prettier eslint-config-prettier
npm install -D @testing-library/react @testing-library/jest-dom vitest
```

**Folder Structure**:
```
src/Web/src/
├── components/          # Reusable UI components
├── pages/              # Page-level components
├── services/           # API clients
├── store/              # Redux slices
├── hooks/              # Custom React hooks
├── types/              # TypeScript definitions
├── utils/              # Helper functions
└── App.tsx             # Root component
```

**Jira Stories**:
- `FOUND-9`: Create React application with Vite and TypeScript
- `FOUND-10`: Install and configure Material-UI with theme
- `FOUND-11`: Set up React Router with basic routes
- `FOUND-12`: Configure Redux Toolkit with auth slice
- `FOUND-13`: Set up ESLint and Prettier

---

## Milestone 2: Infrastructure Provisioning (Days 4-7)

### 2.1 Azure Resource Groups

**Acceptance Criteria**:
- [ ] Resource groups created for dev, staging, prod
- [ ] Naming conventions followed
- [ ] Tags applied for cost tracking

**Azure Resources**:
```
Resource Naming:
- rg-communities-{env}              (Resource Group)
- app-communities-{env}-{uniqueid}  (App Service)
- plan-communities-{env}            (App Service Plan)
- sql-communities-{env}-{uniqueid}  (SQL Server)
- st{env}communities{uniqueid}      (Storage Account)
- redis-communities-{env}           (Redis Cache)
- sb-communities-{env}              (Service Bus)
- kv-comm-{env}-{uniqueid}          (Key Vault)
- appi-communities-{env}            (Application Insights)
```

**Jira Stories**:
- `FOUND-14`: Create Azure resource groups for all environments
- `FOUND-15`: Document naming conventions

---

### 2.2 Development Environment Infrastructure

**Acceptance Criteria**:
- [ ] Azure SQL Database provisioned (S2 tier for dev)
- [ ] Azure Storage Account created
- [ ] Redis Cache deployed (Basic tier for dev)
- [ ] Service Bus namespace created
- [ ] Application Insights configured
- [ ] Key Vault set up with access policies

**Bicep Template** (`infra/environments/dev.bicep`):
```bicep
@description('Environment name')
param environmentName string = 'dev'

param location string = resourceGroup().location

var uniqueId = uniqueString(resourceGroup().id)

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: 'plan-communities-${environmentName}'
  location: location
  sku: {
    name: 'P1V3'
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: 'sql-communities-${environmentName}-${uniqueId}'
  location: location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: '${uniqueString(resourceGroup().id)}Pwd123!'
    minimalTlsVersion: '1.2'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: 'communities'
  location: location
  sku: {
    name: 'S2'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: 'st${environmentName}comm${uniqueId}'
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
  }
}

// Redis Cache
resource redisCache 'Microsoft.Cache/redis@2022-06-01' = {
  name: 'redis-communities-${environmentName}'
  location: location
  properties: {
    sku: {
      name: 'Basic'
      family: 'C'
      capacity: 0
    }
    enableNonSslPort: false
    minimumTlsVersion: '1.2'
  }
}

// Application Insights
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-communities-${environmentName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

// Key Vault
resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: 'kv-comm-${environmentName}-${uniqueId}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

output sqlServerName string = sqlServer.name
output databaseName string = sqlDatabase.name
output storageAccountName string = storageAccount.name
output redisName string = redisCache.name
output keyVaultName string = keyVault.name
output appInsightsInstrumentationKey string = applicationInsights.properties.InstrumentationKey
```

**Deployment Commands**:
```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-communities-dev --location eastus

# Deploy infrastructure
az deployment group create \
  --resource-group rg-communities-dev \
  --template-file infra/environments/dev.bicep \
  --parameters environmentName=dev
```

**Jira Stories**:
- `FOUND-16`: Create Bicep template for dev environment
- `FOUND-17`: Provision Azure SQL Database
- `FOUND-18`: Provision Azure Storage Account
- `FOUND-19`: Provision Redis Cache
- `FOUND-20`: Provision Application Insights
- `FOUND-21`: Provision Key Vault and store secrets

---

### 2.3 CI/CD Pipeline Setup

**Acceptance Criteria**:
- [ ] GitHub Actions workflow builds backend
- [ ] GitHub Actions workflow builds frontend
- [ ] Tests run on every PR
- [ ] Automatic deployment to dev on merge to `develop`
- [ ] Build artifacts stored

**GitHub Actions Workflow** (`.github/workflows/build-test.yml`):
```yaml
name: Build and Test

on:
  push:
    branches: [develop, main]
  pull_request:
    branches: [develop, main]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --no-restore --configuration Release
      
      - name: Test
        run: dotnet test --no-build --configuration Release --logger "trx"
      
      - name: Publish
        run: dotnet publish src/Api/Api.csproj -c Release -o ./publish
      
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: api
          path: ./publish

  frontend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node.js
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          cache: 'npm'
          cache-dependency-path: src/Web/package-lock.json
      
      - name: Install dependencies
        working-directory: src/Web
        run: npm ci
      
      - name: Lint
        working-directory: src/Web
        run: npm run lint
      
      - name: Build
        working-directory: src/Web
        run: npm run build
      
      - name: Test
        working-directory: src/Web
        run: npm test
      
      - name: Upload artifact
        uses: actions/upload-artifact@v3
        with:
          name: web
          path: src/Web/dist
```

**Deployment Workflow** (`.github/workflows/deploy-dev.yml`):
```yaml
name: Deploy to Dev

on:
  push:
    branches: [develop]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: development
    steps:
      - uses: actions/checkout@v3
      
      - name: Azure Login
        uses: azure/login@v1
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS_DEV }}
      
      - name: Deploy to App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ secrets.APP_SERVICE_NAME_DEV }}
          package: ./publish
```

**Jira Stories**:
- `FOUND-22`: Create GitHub Actions workflow for build and test
- `FOUND-23`: Create GitHub Actions workflow for deployment
- `FOUND-24`: Configure Azure credentials in GitHub secrets
- `FOUND-25`: Test end-to-end CI/CD pipeline

---

## Milestone 3: Core Authentication (Days 8-12)

### 3.1 Database Schema - Identity

**Acceptance Criteria**:
- [ ] Entity Framework Core configured with SQL Server
- [ ] ASP.NET Core Identity tables created
- [ ] Custom User and Tenant entities defined
- [ ] Initial migration applied

**Domain Models** (`src/Core/Entities/User.cs`):
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public UserStatus Status { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    // Navigation
    public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}

public enum UserStatus
{
    Active,
    Suspended,
    Deleted
}
```

**Domain Models** (`src/Core/Entities/Tenant.cs`):
```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Domain { get; set; } = null!;
    public TenantStatus Status { get; set; }
    public string SubscriptionTier { get; set; } = "Standard";
    public string? Settings { get; set; } // JSON
    public string? BrandingConfig { get; set; } // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public ICollection<Community> Communities { get; set; } = new List<Community>();
}

public enum TenantStatus
{
    Active,
    Suspended,
    Inactive
}
```

**DbContext** (`src/Infrastructure/Data/AppDbContext.cs`):
```csharp
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Community> Communities { get; set; }
    public DbSet<Membership> Memberships { get; set; }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configure schema
        builder.HasDefaultSchema("dbo");
        
        // Configure entities
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

**Create Migration**:
```bash
# Add Entity Framework tools
dotnet tool install --global dotnet-ef

# Create initial migration
cd src/Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../Api --output-dir Data/Migrations

# Apply migration
dotnet ef database update --startup-project ../Api
```

**Jira Stories**:
- `FOUND-26`: Define User and Tenant domain models
- `FOUND-27`: Configure Entity Framework DbContext
- `FOUND-28`: Create initial database migration
- `FOUND-29`: Apply migration to dev database

---

### 3.2 JWT Authentication Implementation

**Acceptance Criteria**:
- [ ] User registration endpoint works
- [ ] Login endpoint returns JWT token
- [ ] Token refresh endpoint works
- [ ] JWT validation middleware configured
- [ ] Password hashing uses bcrypt

**Authentication Service** (`src/Application/Services/AuthService.cs`):
```csharp
public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<AuthResult> RefreshTokenAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    
    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };
        
        var result = await _userManager.CreateAsync(user, request.Password);
        
        if (!result.Succeeded)
        {
            return new AuthResult { Success = false, Errors = result.Errors.Select(e => e.Description) };
        }
        
        var tokens = await _tokenService.GenerateTokens(user);
        
        return new AuthResult
        {
            Success = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            User = MapToUserDto(user)
        };
    }
    
    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return new AuthResult { Success = false, Errors = new[] { "Invalid credentials" } };
        }
        
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        
        var tokens = await _tokenService.GenerateTokens(user);
        
        return new AuthResult
        {
            Success = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            User = MapToUserDto(user)
        };
    }
}
```

**Authentication Controller** (`src/Api/Controllers/AuthController.cs`):
```csharp
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }
        
        return Ok(result);
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        
        return Ok(result);
    }
    
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResult>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken);
        
        if (!result.Success)
        {
            return Unauthorized(result);
        }
        
        return Ok(result);
    }
}
```

**JWT Configuration** (`src/Api/Program.cs`):
```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)
        ),
        ClockSkew = TimeSpan.Zero
    };
});
```

**Jira Stories**:
- `FOUND-30`: Implement user registration endpoint
- `FOUND-31`: Implement login endpoint with JWT generation
- `FOUND-32`: Implement token refresh endpoint
- `FOUND-33`: Configure JWT authentication middleware
- `FOUND-34`: Write unit tests for authentication service

---

### 3.3 Frontend Authentication Flow

**Acceptance Criteria**:
- [ ] Login page renders correctly
- [ ] Registration page renders correctly
- [ ] JWT token stored securely
- [ ] Protected routes redirect to login
- [ ] User state persists across page refresh

**Auth Slice** (`src/Web/src/store/authSlice.ts`):
```typescript
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { authApi } from '../services/authApi';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  token: localStorage.getItem('token'),
  isAuthenticated: false,
  loading: false,
  error: null
};

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: { email: string; password: string }) => {
    const response = await authApi.login(credentials);
    localStorage.setItem('token', response.accessToken);
    return response;
  }
);

export const register = createAsyncThunk(
  'auth/register',
  async (data: RegisterRequest) => {
    const response = await authApi.register(data);
    localStorage.setItem('token', response.accessToken);
    return response;
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout: (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
      localStorage.removeItem('token');
    }
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.loading = false;
        state.isAuthenticated = true;
        state.user = action.payload.user;
        state.token = action.payload.accessToken;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.error = action.error.message || 'Login failed';
      });
  }
});

export const { logout } = authSlice.actions;
export default authSlice.reducer;
```

**Login Page** (`src/Web/src/pages/LoginPage.tsx`):
```typescript
import React, { useState } from 'react';
import { useDispatch } from 'react-redux';
import { useNavigate } from 'react-router-dom';
import { TextField, Button, Container, Typography } from '@mui/material';
import { login } from '../store/authSlice';

export const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const dispatch = useDispatch();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await dispatch(login({ email, password }));
    navigate('/feed');
  };

  return (
    <Container maxWidth="sm">
      <Typography variant="h4" gutterBottom>
        Login
      </Typography>
      <form onSubmit={handleSubmit}>
        <TextField
          fullWidth
          label="Email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          margin="normal"
          required
        />
        <TextField
          fullWidth
          label="Password"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          margin="normal"
          required
        />
        <Button
          type="submit"
          fullWidth
          variant="contained"
          sx={{ mt: 2 }}
        >
          Login
        </Button>
      </form>
    </Container>
  );
};
```

**Protected Route** (`src/Web/src/components/ProtectedRoute.tsx`):
```typescript
import { Navigate } from 'react-router-dom';
import { useSelector } from 'react-redux';
import { RootState } from '../store';

export const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const isAuthenticated = useSelector((state: RootState) => state.auth.isAuthenticated);
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  return <>{children}</>;
};
```

**Jira Stories**:
- `FOUND-35`: Create login page UI
- `FOUND-36`: Create registration page UI
- `FOUND-37`: Implement auth Redux slice
- `FOUND-38`: Create protected route component
- `FOUND-39`: Set up axios interceptor for auth token

---

## Milestone 4: Basic Tenant Management (Days 13-15)

### 4.1 Tenant CRUD Operations

**Acceptance Criteria**:
- [ ] Create tenant endpoint works
- [ ] Tenant middleware extracts tenant context
- [ ] Database queries scoped to tenant
- [ ] Admin can list tenants

**Tenant Service** (`src/Application/Services/TenantService.cs`):
```csharp
public interface ITenantService
{
    Task<Tenant> CreateTenantAsync(CreateTenantRequest request);
    Task<Tenant?> GetTenantByIdAsync(Guid tenantId);
    Task<Tenant?> GetTenantByDomainAsync(string domain);
    Task<IEnumerable<Tenant>> GetAllTenantsAsync();
}

public class TenantService : ITenantService
{
    private readonly AppDbContext _context;
    
    public async Task<Tenant> CreateTenantAsync(CreateTenantRequest request)
    {
        // Check domain uniqueness
        if (await _context.Tenants.AnyAsync(t => t.Domain == request.Domain))
        {
            throw new InvalidOperationException("Domain already exists");
        }
        
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Domain = request.Domain,
            Status = TenantStatus.Active,
            SubscriptionTier = request.SubscriptionTier ?? "Standard",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        _context.Tenants.Add(tenant);
        await _context.SaveChangesAsync();
        
        return tenant;
    }
}
```

**Tenant Middleware** (`src/Api/Middleware/TenantMiddleware.cs`):
```csharp
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context, ITenantService tenantService)
    {
        // Extract tenant from subdomain or header
        var tenantDomain = ExtractTenantDomain(context);
        
        if (!string.IsNullOrEmpty(tenantDomain))
        {
            var tenant = await tenantService.GetTenantByDomainAsync(tenantDomain);
            
            if (tenant != null)
            {
                context.Items["TenantId"] = tenant.Id;
                context.Items["Tenant"] = tenant;
            }
        }
        
        await _next(context);
    }
    
    private string? ExtractTenantDomain(HttpContext context)
    {
        // Try header first (X-Tenant-Domain)
        if (context.Request.Headers.TryGetValue("X-Tenant-Domain", out var headerValue))
        {
            return headerValue.ToString();
        }
        
        // Try subdomain (e.g., acme.communities.com)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        
        if (parts.Length > 2)
        {
            return parts[0]; // First part is tenant subdomain
        }
        
        return null;
    }
}
```

**Jira Stories**:
- `FOUND-40`: Implement tenant CRUD service
- `FOUND-41`: Create tenant middleware for context extraction
- `FOUND-42`: Add tenant endpoints to API
- `FOUND-43`: Write integration tests for tenant operations

---

## Milestone 5: Documentation and Standards (Days 14-15)

### 5.1 Development Documentation

**Acceptance Criteria**:
- [ ] README has setup instructions
- [ ] CONTRIBUTING guide created
- [ ] API documentation available via Swagger
- [ ] Architecture Decision Records started

**Documents to Create**:
- `README.md` - Project overview and setup
- `CONTRIBUTING.md` - Development guidelines
- `docs/setup/development-environment.md` - Detailed setup guide
- `docs/architecture/decisions/001-use-clean-architecture.md` - First ADR

**Jira Stories**:
- `FOUND-44`: Write comprehensive README
- `FOUND-45`: Create CONTRIBUTING guide
- `FOUND-46`: Document local development setup
- `FOUND-47`: Create first Architecture Decision Record

---

## Definition of Done (Phase 0)

A checklist to confirm Phase 0 is complete:

### Infrastructure
- [ ] Azure dev environment fully provisioned
- [ ] All resources accessible and monitored
- [ ] Key Vault contains all secrets
- [ ] Application Insights receiving telemetry

### Development Environment
- [ ] Developers can clone and run locally
- [ ] Database migrations apply successfully
- [ ] Frontend connects to backend API
- [ ] Hot reload works for both frontend and backend

### CI/CD
- [ ] Build pipeline runs on every PR
- [ ] Tests execute and must pass
- [ ] Deployment to dev is automatic on merge
- [ ] Rollback procedure documented

### Authentication
- [ ] Users can register
- [ ] Users can login and receive JWT
- [ ] JWT validation works on protected endpoints
- [ ] Token refresh works before expiry
- [ ] Frontend stores and uses token correctly

### Tenant Management
- [ ] Tenants can be created via API
- [ ] Tenant context extracted from requests
- [ ] Tenant isolation works at query level

### Documentation
- [ ] README complete with setup steps
- [ ] API documented in Swagger
- [ ] First ADR written
- [ ] Development standards established

### Quality
- [ ] Unit test coverage > 70%
- [ ] No critical security vulnerabilities
- [ ] Code follows established conventions
- [ ] PR review process in place

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Azure provisioning delays | High | Start infrastructure setup on Day 1 |
| Team unfamiliar with .NET 8 | Medium | Allocate time for learning; pair programming |
| Authentication complexity | Medium | Use ASP.NET Core Identity (battle-tested) |
| CI/CD issues | High | Set up early and test thoroughly |
| Database migration conflicts | Medium | One person manages migrations initially |

---

## Next Phase Preview

**Phase 1: MVP (Weeks 4-11)** will build on this foundation:
- Community management (create, configure, members)
- Basic engagement (posts, comments, reactions)
- Simple surveys without logic
- User profiles
- Basic admin dashboard

Phase 1 requires everything from Phase 0 to be solid and stable.

