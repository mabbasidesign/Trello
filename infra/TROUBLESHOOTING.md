# Cosmos DB Deployment Fails: Missing Provider Registration

**Symptom:**
Deployment fails with an error like:
```
The subscription is not registered to use namespace 'Microsoft.DocumentDB'.
```

**Solution:**
1. Register the provider using Azure CLI:
  ```
  az provider register --namespace Microsoft.DocumentDB
  ```
2. Wait a few minutes, then check status:
  ```
  az provider show -n Microsoft.DocumentDB --query "registrationState"
  ```
  It should return `"Registered"`.
3. Re-run your deployment or pipeline.

**Note:** This is a one-time action per subscription.
# Azure Deployment Issues & Solutions

This document records issues encountered during Azure deployments and their solutions.

## Issue History

### 1. Missing Resource Provider Registrations

**Issue Type:** Azure Subscription Configuration  
**Frequency:** Multiple occurrences across different resource types  
**Impact:** Deployment failures in CI/CD pipeline

#### Affected Resources

All Azure resources required provider registration before first deployment:

1. **Microsoft.Sql** - SQL Server and Databases
2. **Microsoft.ServiceBus** - Service Bus Namespace
3. **Microsoft.Web** - App Services and App Service Plans
4. **Microsoft.ContainerRegistry** - Azure Container Registry
5. **Microsoft.ApiManagement** - API Management Gateway

#### Error Message

```
ERROR: MissingSubscriptionRegistration: The subscription is not registered to use namespace 'Microsoft.{ResourceProvider}'

Details:
{
  "code": "MissingSubscriptionRegistration",
  "message": "The subscription is not registered to use namespace 'Microsoft.{ResourceProvider}'. 
             See https://aka.ms/rps-not-found for how to register subscriptions.",
  "target": "Microsoft.{ResourceProvider}"
}
```

#### Root Cause

Azure subscriptions require explicit registration for each resource provider before resources of that type can be created. This is a security and quota management feature.

**Why this happens:**
- New Azure subscriptions don't have all providers pre-registered
- Each service (SQL, Web, API Management, etc.) is a separate provider
- Registration is a one-time requirement per subscription

#### Solution

**Option 1: Azure Portal (Recommended)**

1. Navigate to https://portal.azure.com
2. Click **Subscriptions** from left menu
3. Select your subscription: **Azure subscription 1**
4. Click **Resource providers** (under Settings)
5. Search for the missing provider (e.g., `Microsoft.ApiManagement`)
6. Click the provider row
7. Click **Register** button at top
8. Wait 1-2 minutes for status: **Registered** → **NotRegistered**

**Option 2: Azure Cloud Shell**

```bash
# Single provider
az provider register --namespace Microsoft.ApiManagement

# Multiple providers at once
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.ServiceBus
az provider register --namespace Microsoft.ApiManagement
az provider register --namespace Microsoft.ContainerRegistry

# Check registration status
az provider show --namespace Microsoft.ApiManagement --query "registrationState"
```

**Option 3: PowerShell (Requires Azure CLI)**

```powershell
# Login first
az login

az provider register --namespace Microsoft.ApiManagement

# Verify
az provider list --query "[?namespace=='Microsoft.ApiManagement'].{Namespace:namespace, State:registrationState}" -o table
```

#### Timeline of Occurrences

1. **First Deployment** - All core providers needed registration:
   - Microsoft.Web
   - Microsoft.ContainerRegistry
2. **APIM Deployment (December 16, 2025, 19:56 UTC)** - API Management provider:
   - Error occurred in pipeline Stage 3: DeployInfrastructure
   - Build ID: 25
   - Deployment name: apim-deployment-25

#### Prevention for Future Resources

**Before deploying new resource types, register the provider:**

| Resource Type | Provider Namespace | Registration Command |
| Key Vault | Microsoft.KeyVault | `az provider register --namespace Microsoft.KeyVault` |
| Cosmos DB | Microsoft.DocumentDB | `az provider register --namespace Microsoft.DocumentDB` |
| Redis Cache | Microsoft.Cache | `az provider register --namespace Microsoft.Cache` |
| Storage Account | Microsoft.Storage | Usually pre-registered |
| Virtual Network | Microsoft.Network | Usually pre-registered |

#### After Registration

   - Push empty commit: `git commit --allow-empty -m "Retry after provider registration"; git push`
3. **Verify deployment succeeds**
4. **Check Azure Portal** for created resources

#### Best Practices

1. **Pre-register all providers** when starting new Azure subscription
2. **Document required providers** in README for new team members
3. **Add provider registration** to infrastructure setup scripts
4. **Check provider status** before running deployments

#### Automated Solution (Future Enhancement)

Create a PowerShell script to register all required providers:

# infra/register-providers.ps1
    'Microsoft.Sql',
    'Microsoft.ServiceBus',
    'Microsoft.Web',
    'Microsoft.ApiManagement',
    'Microsoft.ContainerRegistry',
    'Microsoft.Insights',
    'Microsoft.KeyVault'
)

foreach ($provider in $providers) {
    Write-Host "Registering $provider..." -ForegroundColor Cyan
    az provider register --namespace $provider
}

Write-Host "`nAll providers registered. Wait 2-3 minutes before deploying." -ForegroundColor Green
```

---

## Issue 2: APIM Backend Routing 404 Errors

**Date:** December 16, 2025  
**Resource:** API Management Gateway  
**Status:** Resolved

### Problem

API Gateway returned 404 Not Found when calling endpoints through APIM, even though:
- Subscription key was accepted (no 401 error)
- Backend services were running and accessible directly
- APIM deployment succeeded

**Error When Testing:**
```powershell
$key = "7d73c2d85d3544fdaea1d4e291e10cc3"
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" -Headers @{"Ocp-Apim-Subscription-Key"=$key}

# Result: 404 Not Found
```

**But Direct Backend Works:**
```powershell
Invoke-RestMethod -Uri "https://app-trello-product-prod.azurewebsites.net/api/v1/products"

# Result: Returns products successfully
```

### Root Cause

Incorrect `serviceUrl` configuration in APIM Bicep template.

**Problem Configuration:**
```bicep
resource productApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  properties: {
    serviceUrl: productServiceUrl  // ❌ Points to /api/v1
    path: 'products'
  }
}
```

When APIM received request: `GET /products`  
It routed to backend: `https://backend/api/v1/` (missing `/products`)  
Backend expected: `https://backend/api/v1/products`

### Solution

Updated `serviceUrl` to include the full path to the backend endpoint:

```bicep
resource productApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  properties: {
    serviceUrl: '${productServiceUrl}/products'  // ✅ Points to /api/v1/products
    path: 'products'
  }
}

resource orderApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  properties: {
    serviceUrl: '${orderServiceUrl}/orders'  // ✅ Points to /api/v1/orders
    path: 'orders'
  }
}
```

**Commit:** `607991a` - Fix APIM backend routing paths

### How APIM Routing Works

**Request Flow:**
1. Client calls: `https://apim-gateway.net/products`
2. APIM matches path: `products`
3. APIM finds API with `path: 'products'`
4. APIM takes the **remaining path** after `/products` (in this case `/`)
5. APIM appends to `serviceUrl` and calls backend

**Example:**
- Client: `GET /products` → Backend: `GET {serviceUrl}/`
- Client: `GET /products/123` → Backend: `GET {serviceUrl}/123`

**Therefore:**
- If `serviceUrl = https://backend/api/v1`, client `/products` → backend `/api/v1/` ❌
- If `serviceUrl = https://backend/api/v1/products`, client `/products` → backend `/api/v1/products/` ✅

### Testing Commands

**Test APIM Gateway (After Fix):**
```powershell
# Set your subscription key
$key = "YOUR_SUBSCRIPTION_KEY_HERE"

# Test Product API
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}

# Test Get Product by ID
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/1" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}

# Test Order API
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

**Test Backend Directly (For Comparison):**
```powershell
# Product API
Invoke-RestMethod -Uri "https://app-trello-product-prod.azurewebsites.net/api/v1/products"

# Order API
Invoke-RestMethod -Uri "https://app-trello-order-prod.azurewebsites.net/api/v1/orders"
```

**Get Subscription Key:**
1. Azure Portal → apim-trello-fumbtexxi35ii
2. Left menu → **Subscriptions**
3. Click **+ Add** if no subscription exists
   - Name: `unlimited-subscription`
   - Scope: **Product** → **Unlimited**
   - State: **Active**
4. Click on the subscription
5. Click **Show keys**
6. Copy **Primary key**

### Lesson Learned

- APIM `serviceUrl` should point to the **exact backend endpoint**, not just the base URL
- APIM routing appends the request path to `serviceUrl`
- Always test both direct backend and APIM gateway after configuration changes
- Use `Invoke-RestMethod` with `-Headers @{"Ocp-Apim-Subscription-Key"=$key}` for testing

### Related Configuration

**Pipeline Parameters (Correct):**
```yaml
productServiceUrl: "https://app-trello-product-prod.azurewebsites.net/api/v1"
orderServiceUrl: "https://app-trello-order-prod.azurewebsites.net/api/v1"
```

**Bicep Usage (Fixed):**
```bicep
serviceUrl: '${productServiceUrl}/products'  // Full path
serviceUrl: '${orderServiceUrl}/orders'      // Full path
```

---

## Issue 3: SQL Server Regional Availability

**Date:** December 2025  
**Resource:** SQL Server  
**Status:** Resolved

### Problem

SQL Server creation failed in multiple regions due to capacity constraints.

**Attempted Regions:**
- Canada East (preferred) - ❌ Failed
- East US - ❌ Failed  
- West US - ❌ Failed
- Central US - ❌ Failed
- **West US 2** - ✅ Succeeded

### Error Message

```
The subscription does not have sufficient capacity for the requested operation in this region
```

### Solution

1. Changed deployment region to West US 2 in pipeline parameters
2. Updated `infra/sqlserver.bicep` location parameter
3. Successfully deployed SQL Server in West US 2

### Lesson Learned

- Azure capacity varies by region
- Have backup regions ready in pipeline configuration
- SQL Server and App Services can be in different regions (acceptable latency for this architecture)

---

## Issue 4: Swagger API Version Placeholder Not Substituting

**Date:** December 2025  
**Resource:** Product Service, Order Service  
**Status:** Resolved

### Problem

Swagger UI showed URLs with `{version}` placeholder instead of actual version number:
- Expected: `/api/v1/products`
- Actual: `/api/v{version}/products`

### Root Cause

Missing configuration in API versioning setup.

### Solution

Added `SubstituteApiVersionInUrl` option in `Program.cs`:

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;  // ← THIS LINE FIXED IT
});
```

**Commits:** 
- `964420a` - Product Service fix
- Applied to both services

---

## Issue 5: Package Version Conflicts

**Date:** December 2025  
**Resources:** Product Service, Order Service  
**Status:** Resolved

### Problem 1: OpenApi Version Mismatch

**Error:** CS1501 at `Program.cs` line 46 in order-service

**Cause:** 
- Product Service: `Microsoft.AspNetCore.OpenApi` 10.0.1
- Order Service: `Microsoft.AspNetCore.OpenApi` 10.0.0

**Solution:** Updated order-service to 10.0.1 (commit `e4ed5a6`)

### Problem 2: Missing ApiExplorer Package

**Error:** CS1501 in both services after OpenApi update

**Cause:** `AddApiExplorer()` method requires separate package

**Solution:** Added `Asp.Versioning.Mvc.ApiExplorer` 8.1.0 to both services (commit `d7d8dc5`)

### Lesson Learned

- Keep package versions synchronized across microservices
- API versioning requires multiple packages:
  - `Asp.Versioning.Http` (core)
  - `Asp.Versioning.Mvc.ApiExplorer` (Swagger integration)
  - `Microsoft.AspNetCore.OpenApi` (OpenAPI spec generation)

---

## Issue 6: Hardcoded Passwords in Source Code

**Date:** December 2025  
**Status:** Resolved  
**Security Level:** HIGH PRIORITY

### Problem

Passwords and connection strings were hardcoded in:
- `docker-compose.product.yml`
- `docker-compose.order.yml`
- Configuration files

### Solution

Implemented 3-tier security architecture:

1. **Docker (.env files)** - Local development
2. **.NET User Secrets** - Local .NET development  
3. **Azure Key Vault** - Production

**Commits:**
- `f968580` - Remove hardcoded passwords
- `02cf7f8` - Add Key Vault deployment
- Manual Azure Portal configuration

---

## General Troubleshooting Tips

### Pipeline Failures

1. **Check pipeline logs** in Azure DevOps
2. **Look for ERROR:** lines in output
3. **Search for the error code** (e.g., `MissingSubscriptionRegistration`)
4. **Check Azure Portal** for partial deployments

### Resource Creation Issues

1. **Verify subscription quotas** in Azure Portal → Subscriptions → Usage + quotas
2. **Check service availability** by region
3. **Ensure resource names are unique** (use `uniqueString()` in Bicep)
4. **Validate Bicep templates** locally: `az bicep build --file template.bicep`

### Common Azure CLI Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `az: command not found` | Azure CLI not installed | Install from https://aka.ms/installazurecli |
| `Please run 'az login'` | Not authenticated | Run `az login` and follow prompts |
| `Subscription not found` | Wrong subscription context | `az account set --subscription {id}` |
| `ResourceNotFound` | Resource doesn't exist | Check resource name and region |

### Useful Azure CLI Commands

```bash
# Check current subscription
az account show

# List all resource groups
az group list -o table

# Check deployment status
az deployment group show \
  --resource-group rg-trello-microservices \
  --name apim-deployment-25

# List all providers and their status
az provider list --query "[].{Namespace:namespace, State:registrationState}" -o table

# Get resource details
az resource list --resource-group rg-trello-microservices -o table
```

---

## Contact & Support

- **Azure Support:** https://portal.azure.com → Support + troubleshooting
- **Azure DevOps Support:** https://dev.azure.com → Help
- **Documentation:** https://docs.microsoft.com/azure

## Related Documents

- [APIM-SETUP.md](APIM-SETUP.md) - API Management setup guide
- [README.md](../README.md) - Project overview and architecture
- Azure Portal: https://portal.azure.com
- Azure DevOps: https://dev.azure.com
