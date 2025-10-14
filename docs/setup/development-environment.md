# Development Environment Setup

This guide walks through setting up your local development environment for the Insight Community Platform.

---

## Prerequisites

### Required Software

| Tool | Version | Purpose | Installation |
|------|---------|---------|--------------|
| .NET SDK | 8.0+ | Backend development | [Download](https://dotnet.microsoft.com/download) |
| Node.js | 18+ LTS | Frontend development | [Download](https://nodejs.org/) |
| Docker Desktop | Latest | Local services (SQL, Redis) | [Download](https://www.docker.com/products/docker-desktop) |
| Git | 2.0+ | Version control | [Download](https://git-scm.com/) |
| VS Code | Latest | Code editor | [Download](https://code.visualstudio.com/) |

### Recommended VS Code Extensions

```json
{
  "recommendations": [
    "ms-dotnettools.csharp",
    "ms-azuretools.vscode-azurefunctions",
    "ms-azuretools.vscode-bicep",
    "dbaeumer.vscode-eslint",
    "esbenp.prettier-vscode",
    "bradlc.vscode-tailwindcss",
    "eamodio.gitlens",
    "rangav.vscode-thunder-client"
  ]
}
```

### Optional but Useful

- **Azure CLI**: For infrastructure management
- **SQL Server Management Studio** or **Azure Data Studio**: For database management
- **Postman** or **Thunder Client**: For API testing
- **Redis Insight**: For Redis cache inspection

---

## Step 1: Clone Repository

```bash
# Clone the repository
git clone https://github.com/your-org/online-communities-platform.git
cd online-communities-platform

# Checkout develop branch
git checkout develop
```

---

## Step 2: Local Services with Docker

We use Docker Compose to run SQL Server and Redis locally.

### Create `docker-compose.yml` (if not exists)

```yaml
version: '3.8'

services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    container_name: communities-sql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong!Passw0rd
      - MSSQL_PID=Developer
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    networks:
      - communities-network

  redis:
    image: redis:7-alpine
    container_name: communities-redis
    ports:
      - "6379:6379"
    networks:
      - communities-network

volumes:
  sqldata:

networks:
  communities-network:
    driver: bridge
```

### Start Services

```bash
# Start SQL Server and Redis
docker-compose up -d

# Verify containers are running
docker ps

# Check logs if issues
docker logs communities-sql
docker logs communities-redis
```

### Test SQL Server Connection

```bash
# Using sqlcmd (if installed)
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd'

# Or using Azure Data Studio
# Connection string: Server=localhost,1433;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True
```

---

## Step 3: Backend Setup (.NET)

### Install Dependencies

```bash
# Navigate to API project
cd src/Api

# Restore NuGet packages
dotnet restore

# Navigate back to root
cd ../..
```

### Configure App Settings

Create `src/Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=Communities;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-change-in-production-minimum-32-characters",
    "Issuer": "https://localhost:5001",
    "Audience": "https://localhost:5001",
    "ExpiryMinutes": 15,
    "RefreshExpiryDays": 7
  },
  "AzureStorage": {
    "ConnectionString": "UseDevelopmentStorage=true"
  },
  "ApplicationInsights": {
    "InstrumentationKey": ""
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:3000"
    ]
  }
}
```

### Run Database Migrations

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Navigate to Infrastructure project
cd src/Infrastructure

# Create database and run migrations
dotnet ef database update --startup-project ../Api

# Verify database created
# Should see "Communities" database with tables
```

### Run Backend

```bash
# From root directory
cd src/Api

# Run in development mode
dotnet run

# Or use watch mode for auto-reload
dotnet watch run

# Backend should be running on:
# https://localhost:5001
# http://localhost:5000
```

### Test Backend

Open browser to https://localhost:5001/swagger to see API documentation.

Test health endpoint:
```bash
curl https://localhost:5001/health
```

---

## Step 4: Frontend Setup (React)

### Install Dependencies

```bash
# Navigate to Web project
cd src/Web

# Install npm packages
npm install
```

### Configure Environment

Create `src/Web/.env.development`:

```env
VITE_API_URL=https://localhost:5001
VITE_API_BASE_PATH=/api/v1
VITE_ENVIRONMENT=development
VITE_SIGNALR_URL=https://localhost:5001/hubs
```

### Run Frontend

```bash
# From src/Web directory
npm run dev

# Frontend should be running on:
# http://localhost:5173
```

Open browser to http://localhost:5173

---

## Step 5: Verify Everything Works

### Quick Smoke Test

1. **Backend Health Check**:
   ```bash
   curl https://localhost:5001/health
   # Should return: {"status":"Healthy"}
   ```

2. **Database Connection**:
   ```bash
   # Check if tables exist
   sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -d Communities -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES"
   ```

3. **Frontend**: 
   - Open http://localhost:5173
   - Should see login/landing page

4. **Create Test User**:
   ```bash
   curl -X POST https://localhost:5001/api/v1/auth/register \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!",
       "firstName": "Test",
       "lastName": "User"
     }'
   ```

5. **Login**:
   ```bash
   curl -X POST https://localhost:5001/api/v1/auth/login \
     -H "Content-Type: application/json" \
     -d '{
       "email": "test@example.com",
       "password": "Test123!"
     }'
   # Should return JWT token
   ```

---

## Troubleshooting

### SQL Server Won't Start

**Issue**: Port 1433 already in use

**Solution**:
```bash
# Check what's using port 1433
netstat -ano | findstr :1433  # Windows
lsof -i :1433                  # macOS/Linux

# Stop existing SQL Server service or use different port in docker-compose.yml
```

**Issue**: Authentication failed

**Solution**:
- Verify password matches `SA_PASSWORD` in docker-compose.yml
- Ensure `TrustServerCertificate=True` in connection string

---

### Backend Won't Start

**Issue**: Port already in use

**Solution**:
```bash
# Change port in launchSettings.json
# Or kill process using port 5001
netstat -ano | findstr :5001  # Windows
kill -9 $(lsof -t -i:5001)    # macOS/Linux
```

**Issue**: Migration fails

**Solution**:
```bash
# Delete database and recreate
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "DROP DATABASE Communities"
dotnet ef database update --startup-project ../Api
```

---

### Frontend Won't Start

**Issue**: `npm install` fails

**Solution**:
```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm cache clean --force
npm install
```

**Issue**: CORS errors in browser

**Solution**:
- Verify frontend URL in `appsettings.Development.json` under `Cors:AllowedOrigins`
- Restart backend after config change

---

### Redis Connection Issues

**Issue**: Can't connect to Redis

**Solution**:
```bash
# Verify Redis is running
docker ps | grep redis

# Test connection
docker exec -it communities-redis redis-cli ping
# Should return: PONG
```

---

## IDE Setup

### Visual Studio Code

**Recommended Settings** (`.vscode/settings.json`):
```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "eslint.validate": ["javascript", "typescript", "typescriptreact"],
  "typescript.tsdk": "node_modules/typescript/lib",
  "dotnet.defaultSolution": "OnlineCommunities.sln"
}
```

**Recommended Launch Configuration** (`.vscode/launch.json`):
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (API)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/Api/bin/Debug/net8.0/Api.dll",
      "args": [],
      "cwd": "${workspaceFolder}/src/Api",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Chrome (Frontend)",
      "type": "chrome",
      "request": "launch",
      "url": "http://localhost:5173",
      "webRoot": "${workspaceFolder}/src/Web"
    }
  ]
}
```

### Visual Studio 2022 (Optional)

1. Open `OnlineCommunities.sln`
2. Set `Api` project as startup project
3. Press F5 to run with debugging
4. Swagger UI opens automatically

---

## Daily Development Workflow

```bash
# 1. Start local services
docker-compose up -d

# 2. Start backend (terminal 1)
cd src/Api
dotnet watch run

# 3. Start frontend (terminal 2)
cd src/Web
npm run dev

# 4. Code and test
# Changes auto-reload in both backend and frontend

# 5. Run tests before committing
dotnet test                    # Backend tests
cd src/Web && npm test        # Frontend tests

# 6. Stop services when done
docker-compose down
```

---

## Testing

### Backend Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Api.Tests

# Run tests in watch mode
dotnet watch test
```

### Frontend Tests

```bash
cd src/Web

# Run tests
npm test

# Run with coverage
npm run test:coverage

# Run in watch mode
npm run test:watch

# Run E2E tests (if configured)
npm run test:e2e
```

---

## Code Quality

### Backend Linting

```bash
# Install dotnet-format tool
dotnet tool install -g dotnet-format

# Format code
dotnet format

# Check for issues without fixing
dotnet format --verify-no-changes
```

### Frontend Linting

```bash
cd src/Web

# Lint code
npm run lint

# Fix auto-fixable issues
npm run lint:fix

# Format with Prettier
npm run format
```

---

## Useful Commands

### Database

```bash
# Create new migration
dotnet ef migrations add MigrationName --startup-project src/Api --project src/Infrastructure

# Remove last migration
dotnet ef migrations remove --startup-project src/Api --project src/Infrastructure

# Generate SQL script
dotnet ef migrations script --startup-project src/Api --project src/Infrastructure --output migration.sql

# Reset database (WARNING: destroys data)
dotnet ef database drop --startup-project src/Api --force
dotnet ef database update --startup-project src/Api
```

### Docker

```bash
# View logs
docker-compose logs -f

# Restart service
docker-compose restart sqlserver

# Stop and remove volumes (clean slate)
docker-compose down -v

# Enter container shell
docker exec -it communities-sql /bin/bash
docker exec -it communities-redis redis-cli
```

---

## Next Steps

Once your environment is set up:

1. Review [Contributing Guidelines](../../CONTRIBUTING.md)
2. Pick a story from Jira
3. Create feature branch: `git checkout -b feature/STORY-123-description`
4. Make changes and test locally
5. Create pull request

---

## Getting Help

- **Slack**: #development channel
- **Documentation**: `docs/` folder
- **API Docs**: https://localhost:5001/swagger
- **Wiki**: [Project Wiki](https://wiki.example.com)

---

## Environment Variables Reference

### Backend (`appsettings.Development.json`)

| Variable | Purpose | Example |
|----------|---------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection | `Server=localhost,1433;...` |
| `Redis:ConnectionString` | Redis connection | `localhost:6379` |
| `Jwt:SecretKey` | JWT signing key | Min 32 characters |
| `Jwt:ExpiryMinutes` | Access token TTL | `15` |
| `AzureStorage:ConnectionString` | Blob storage | `UseDevelopmentStorage=true` |

### Frontend (`.env.development`)

| Variable | Purpose | Example |
|----------|---------|---------|
| `VITE_API_URL` | Backend API URL | `https://localhost:5001` |
| `VITE_API_BASE_PATH` | API base path | `/api/v1` |
| `VITE_ENVIRONMENT` | Environment name | `development` |

