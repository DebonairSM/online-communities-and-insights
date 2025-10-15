# Microsoft Entra External ID Integration with React and Vite

## Overview

This guide shows how to integrate Microsoft Entra External ID (formerly Azure AD B2C) with a React + Vite frontend for authentication in the Online Communities Platform. This provides a managed authentication service that handles user registration, login, and multi-tenant access control.

## Prerequisites

1. **Microsoft Entra External ID tenant** configured with:
   - Custom attributes for tenant management
   - User flows for sign-up/sign-in
   - Social identity providers (Google, GitHub, Microsoft)
   
2. **React application** with Vite build tool

## Setup and Configuration

### 1. Install Required Packages

```bash
npm install @azure/msal-browser @azure/msal-react
npm install --save-dev @types/node
```

### 2. Environment Configuration

**`.env.local`:**
```env
VITE_ENTRA_CLIENT_ID=your-client-id-here
VITE_ENTRA_AUTHORITY=https://yourtenant.ciamlogin.com/yourtenant.onmicrosoft.com/b2c_1_signupsignin
VITE_ENTRA_REDIRECT_URI=http://localhost:5173
VITE_API_BASE_URL=https://your-api.azurewebsites.net/api/v1
```

**`vite.config.ts`:**
```typescript
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: true,
  },
  define: {
    global: 'globalThis',
  }
})
```

### 3. MSAL Configuration

**`src/auth/msalConfig.ts`:**
```typescript
import { Configuration, PopupRequest } from '@azure/msal-browser'

// MSAL configuration
export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_ENTRA_CLIENT_ID,
    authority: import.meta.env.VITE_ENTRA_AUTHORITY,
    redirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
    knownAuthorities: ['yourtenant.ciamlogin.com'],
    postLogoutRedirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
  },
  cache: {
    cacheLocation: 'sessionStorage', // Configures cache location
    storeAuthStateInCookie: false, // Set to true for IE11/Edge
  },
}

// Request configuration for token acquisition
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'email'],
  extraQueryParameters: {},
}

export const tokenRequest = {
  scopes: ['openid', 'profile', 'email'],
}
```

### 4. MSAL Provider Setup

**`src/main.tsx`:**
```typescript
import React from 'react'
import ReactDOM from 'react-dom/client'
import { PublicClientApplication } from '@azure/msal-browser'
import { MsalProvider } from '@azure/msal-react'
import App from './App.tsx'
import { msalConfig } from './auth/msalConfig.ts'

const msalInstance = new PublicClientApplication(msalConfig)

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  </React.StrictMode>,
)
```

### 5. Authentication Context

**`src/auth/AuthContext.tsx`:**
```typescript
import React, { createContext, useContext, useEffect, useState } from 'react'
import { useMsal, useAccount } from '@azure/msal-react'
import { AuthenticationResult } from '@azure/msal-browser'

interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  tenantId: string
  tenantName: string
  roles: string[]
}

interface AuthContextType {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  login: () => Promise<void>
  logout: () => Promise<void>
  getAccessToken: () => Promise<string | null>
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export const useAuth = () => {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { instance, accounts } = useMsal()
  const account = useAccount(accounts[0] || {})
  const [user, setUser] = useState<User | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        if (account) {
          await loadUserProfile()
        }
      } catch (error) {
        console.error('Failed to initialize auth:', error)
      } finally {
        setIsLoading(false)
      }
    }

    initializeAuth()
  }, [account])

  const loadUserProfile = async () => {
    if (!account) return

    try {
      // Call your backend API to get user details
      const response = await fetch(`${import.meta.env.VITE_API_BASE_URL}/auth/profile`, {
        headers: {
          'Authorization': `Bearer ${await getAccessToken()}`,
          'Content-Type': 'application/json',
        },
      })

      if (response.ok) {
        const userData = await response.json()
        setUser(userData)
      }
    } catch (error) {
      console.error('Failed to load user profile:', error)
    }
  }

  const login = async () => {
    try {
      const loginResponse: AuthenticationResult = await instance.loginPopup({
        scopes: ['openid', 'profile', 'email'],
      })
      
      if (loginResponse.account) {
        await loadUserProfile()
      }
    } catch (error) {
      console.error('Login failed:', error)
    }
  }

  const logout = async () => {
    try {
      await instance.logoutPopup({
        postLogoutRedirectUri: import.meta.env.VITE_ENTRA_REDIRECT_URI,
      })
      setUser(null)
    } catch (error) {
      console.error('Logout failed:', error)
    }
  }

  const getAccessToken = async (): Promise<string | null> => {
    if (!account) return null

    try {
      const response = await instance.acquireTokenSilent({
        scopes: ['openid', 'profile', 'email'],
        account: account,
      })
      return response.accessToken
    } catch (error) {
      console.error('Failed to acquire token:', error)
      return null
    }
  }

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    login,
    logout,
    getAccessToken,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
```

### 6. Protected Route Component

**`src/components/ProtectedRoute.tsx`:**
```typescript
import React from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

interface ProtectedRouteProps {
  children: React.ReactNode
  requiredRoles?: string[]
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ 
  children, 
  requiredRoles = [] 
}) => {
  const { user, isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (requiredRoles.length > 0) {
    const hasRequiredRole = requiredRoles.some(role => user.roles.includes(role))
    if (!hasRequiredRole) {
      return <Navigate to="/unauthorized" replace />
    }
  }

  return <>{children}</>
}
```

### 7. Login Component

**`src/components/LoginPage.tsx`:**
```typescript
import React from 'react'
import { useAuth } from '../auth/AuthContext'
import { useMsal } from '@azure/msal-react'

export const LoginPage: React.FC = () => {
  const { login } = useAuth()
  const { instance } = useMsal()

  const handleLogin = async () => {
    try {
      await login()
    } catch (error) {
      console.error('Login failed:', error)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to your account
          </h2>
        </div>
        
          <div className="mt-8 space-y-6">
            <div>
              <button
                onClick={handleLogin}
                className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                Sign in with Microsoft
              </button>
            </div>
            <div className="text-xs text-center text-gray-500">
              Also supports Google and GitHub accounts through Microsoft Entra External ID
            </div>
          </div>
      </div>
    </div>
  )
}
```

### 8. API Client with Authentication

**`src/services/apiClient.ts`:**
```typescript
class ApiClient {
  private baseURL: string
  private getAccessToken: (() => Promise<string | null>) | null = null

  constructor() {
    this.baseURL = import.meta.env.VITE_API_BASE_URL || '/api/v1'
  }

  setTokenProvider(tokenProvider: () => Promise<string | null>) {
    this.getAccessToken = tokenProvider
  }

  private async getAuthHeaders(): Promise<HeadersInit> {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    }
    
    if (this.getAccessToken) {
      const token = await this.getAccessToken()
      if (token) {
        headers.Authorization = `Bearer ${token}`
      }
    }
    
    return headers
  }

  async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${this.baseURL}${endpoint}`
    const headers = await this.getAuthHeaders()
    
    const config: RequestInit = {
      ...options,
      headers: {
        ...headers,
        ...options.headers,
      },
    }

    const response = await fetch(url, config)
    
    if (!response.ok) {
      if (response.status === 401) {
        // Handle unauthorized - redirect to login or refresh token
        throw new Error('Unauthorized')
      }
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return response.json()
  }

  async get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' })
  }

  async post<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: data ? JSON.stringify(data) : undefined,
    })
  }

  async put<T>(endpoint: string, data?: any): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: data ? JSON.stringify(data) : undefined,
    })
  }

  async delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' })
  }
}

export const apiClient = new ApiClient()
```

### 9. React Query Integration

**`src/services/useApi.ts`:**
```typescript
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useAuth } from '../auth/AuthContext'

export const useAuthenticatedFetch = () => {
  const { getAccessToken } = useAuth()

  return async (url: string, options: RequestInit = {}) => {
    const token = await getAccessToken()
    
    const headers = {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    }

    const response = await fetch(url, {
      ...options,
      headers,
    })

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`)
    }

    return response.json()
  }
}

// Example usage
export const useCommunities = () => {
  const authenticatedFetch = useAuthenticatedFetch()

  return useQuery({
    queryKey: ['communities'],
    queryFn: () => 
      authenticatedFetch(`${import.meta.env.VITE_API_BASE_URL}/communities`),
  })
}

export const useCreatePost = () => {
  const authenticatedFetch = useAuthenticatedFetch()
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (data: any) =>
      authenticatedFetch(`${import.meta.env.VITE_API_BASE_URL}/posts`, {
        method: 'POST',
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['posts'] })
    },
  })
}
```

### 10. App Router Setup

**`src/App.tsx`:**
```typescript
import React from 'react'
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './components/ProtectedRoute'
import { LoginPage } from './components/LoginPage'
import { Dashboard } from './components/Dashboard'
import { Communities } from './components/Communities'

function App() {
  return (
    <Router>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route 
            path="/dashboard" 
            element={
              <ProtectedRoute>
                <Dashboard />
              </ProtectedRoute>
            } 
          />
          <Route 
            path="/communities" 
            element={
              <ProtectedRoute requiredRoles={['Admin', 'Moderator']}>
                <Communities />
              </ProtectedRoute>
            } 
          />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </AuthProvider>
    </Router>
  )
}

export default App
```

## Backend Integration

The backend is configured to handle Microsoft Entra External ID tokens:

### 1. Backend Configuration (ASP.NET Core)

**`Program.cs`:**
```csharp
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add Microsoft Identity Web for Entra External ID
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("EntraExternalId"));

builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**`appsettings.json`:**
```json
{
  "EntraExternalId": {
    "Instance": "https://yourtenant.ciamlogin.com/",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "Audience": "your-client-id"
  }
}
```

### 2. User Profile Endpoint

**`Controllers/AuthController.cs`:**
```csharp
[ApiController]
[Route("api/v1/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value;
        var tenantId = User.FindFirst("extension_TenantId")?.Value;
        var tenantName = User.FindFirst("extension_TenantName")?.Value;

        // Get user from database using the Entra External ID subject
        var user = await _userService.GetByExternalIdAsync(userId);
        
        if (user == null)
        {
            // JIT provisioning - create user if doesn't exist
            user = await _userService.CreateFromExternalIdAsync(userId, email, tenantId);
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            tenantId = user.TenantId,
            tenantName = tenantName,
            roles = user.Roles.Select(r => r.Name).ToArray()
        });
    }
}
```

## Custom Attributes Setup

To support tenant management, configure these custom attributes in Microsoft Entra External ID:

1. **`extension_TenantId`** - GUID of the tenant
2. **`extension_TenantName`** - Friendly tenant name
3. **`extension_TenantRoles`** - JSON array of user roles

### User Flow Configuration

In the Azure portal, configure your sign-up/sign-in user flow to collect these attributes during registration and include them in tokens.

## Key Benefits of This Approach

1. **Managed Authentication**: No need to handle OAuth flows manually
2. **Multiple Identity Providers**: Supports Google, GitHub, Microsoft accounts through one interface
3. **Custom Attributes**: Store tenant and role information in Entra External ID
4. **Security**: Microsoft handles token validation and refresh automatically
5. **Scalability**: Built for consumer-scale applications

This integration provides a robust, scalable authentication solution that aligns with your multi-tenant architecture while leveraging Microsoft's managed authentication service.
