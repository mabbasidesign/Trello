# API Management (APIM) Setup Guide

## Overview

This document explains how Azure API Management is created and configured to provide a unified gateway endpoint for the Trello microservices.

## What is API Management?

API Management is a **reverse proxy** and **API gateway** that sits between clients and backend services. It provides:

- **Unified Endpoint**: Single URL for all microservices
- **Security**: API key authentication, rate limiting, IP filtering
- **Analytics**: Request/response logging, performance metrics
- **Transformation**: Request/response modification, versioning
- **Developer Portal**: Interactive API documentation

## Architecture

```
Client Applications
        ↓
[API Management Gateway]
  ├─ Authentication (Subscription Keys)
  ├─ Rate Limiting (100 calls/min)
  ├─ CORS Policies
  └─ Routing Rules
        ↓
Backend Services
  ├─ Product Service: https://app-trello-product-prod.azurewebsites.net/api/v1
  └─ Order Service: https://app-trello-order-prod.azurewebsites.net/api/v1
```

## Infrastructure Components

### 1. API Management Service (`apim.bicep`)

**Resource Type:** `Microsoft.ApiManagement/service`

**Configuration:**
- **SKU:** Consumption (serverless, pay-per-call)
- **Location:** Canada East
- **Name:** `apim-trello-{uniqueString}` (automatically generated)
- **Publisher:** Trello Microservices

**What Azure Creates:**
- Gateway endpoint: `https://apim-trello-*.azure-api.net`
- Developer portal: `https://apim-trello-*.developer.azure-api.net`
- Management API for configuration
- SSL certificates for HTTPS

### 2. Product API

**Resource Type:** `Microsoft.ApiManagement/service/apis`

**Configuration:**
- **Display Name:** Product API
- **Path:** `/products`
- **Backend URL:** `https://app-trello-product-prod.azurewebsites.net/api/v1`
- **Subscription Required:** Yes

**Operations Defined:**
- `GET /` - Get all products
- `GET /{id}` - Get product by ID
- `POST /` - Create product
- `PUT /{id}` - Update product
- `DELETE /{id}` - Delete product

**Routing Example:**
```
Client Request:  GET https://apim-gateway/products/123
       ↓
APIM Routes to:  GET https://backend/api/v1/123
```

### 3. Order API

**Resource Type:** `Microsoft.ApiManagement/service/apis`

**Configuration:**
- **Display Name:** Order API
- **Path:** `/orders`
- **Backend URL:** `https://app-trello-order-prod.azurewebsites.net/api/v1`
- **Subscription Required:** Yes

**Operations Defined:**
- `GET /` - Get all orders
- `GET /{id}` - Get order by ID
- `POST /` - Create order
- `PATCH /{id}/status` - Update order status

### 4. Policies

**Resource Type:** `Microsoft.ApiManagement/service/apis/policies`

**Applied to Both APIs:**

**CORS Policy:**
```xml
<cors allow-credentials="false">
  <allowed-origins>
    <origin>*</origin>
  </allowed-origins>
  <allowed-methods>
    <method>GET</method>
    <method>POST</method>
    <method>PUT</method>
    <method>DELETE</method>
    <method>PATCH</method>
    <method>OPTIONS</method>
  </allowed-methods>
  <allowed-headers>
    <header>*</header>
  </allowed-headers>
</cors>
```
- Allows cross-origin requests from any domain
- Production should restrict to specific origins

**Rate Limiting:**
```xml
<rate-limit calls="100" renewal-period="60" />
```
- Maximum 100 API calls per 60 seconds per subscription key
- Returns HTTP 429 (Too Many Requests) when exceeded
- Prevents abuse and controls costs

**Policy Execution Flow:**
```
Inbound (Before Backend):
  1. Validate subscription key
  2. Apply CORS headers
  3. Check rate limit
  4. Forward to backend

Backend:
  Process request

Outbound (After Backend):
  1. Apply response transformations
  2. Add custom headers
  3. Return to client

On Error:
  Return custom error response
```

### 5. Product (Subscription Management)

**Resource Type:** `Microsoft.ApiManagement/service/products`

**Configuration:**
- **Name:** Unlimited
- **Subscription Required:** Yes
- **Approval Required:** No (instant access)
- **State:** Published

**Purpose:**
- Groups multiple APIs under single subscription key
- One key works for both Product API and Order API
- Simplifies client authentication

**API Associations:**
- Product API linked to Unlimited product
- Order API linked to Unlimited product

## Deployment Process

### Automated Deployment (CI/CD Pipeline)

**File:** `azure-pipelines/azure-pipelines.yml`

**Stage 3: DeployInfrastructure**

```yaml
- task: AzureCLI@2
  displayName: 'Deploy API Management'
  inputs:
    azureSubscription: $(azureSubscription)
    scriptType: 'bash'
    scriptLocation: 'inlineScript'
    inlineScript: |
      az deployment group create \
        --resource-group rg-trello-microservices \
        --template-file infra/apim.bicep \
        --parameters \
          location=canadaeast \
          publisherEmail="admin@trello.com" \
          publisherName="Trello Microservices" \
          productServiceUrl="https://app-trello-product-prod.azurewebsites.net/api/v1" \
          orderServiceUrl="https://app-trello-order-prod.azurewebsites.net/api/v1" \
        --name "apim-deployment-$(Build.BuildId)"
```

**Trigger:** Automatic on push to `main` branch

**Timeline:**
1. Code pushed to GitHub → Pipeline triggered (1-2 min)
2. Build stage completes → Docker images built (5-10 min)
3. DeployInfrastructure stage → APIM deployed (5-10 min)
4. **Total:** ~15-20 minutes

### Manual Deployment (PowerShell)

**File:** `infra/deploy-apim.ps1`

**Usage:**
```powershell
cd C:\Users\mabba\Desktop\Trello
.\infra\deploy-apim.ps1
```

**What It Does:**
1. Sets Azure subscription context
2. Retrieves App Service URLs
3. Deploys Bicep template
4. Outputs Gateway URL and Developer Portal URL

**Note:** Requires Azure CLI installed locally

## How Backend URLs are Configured

### 1. Parameters Passed During Deployment

```bicep
param productServiceUrl string
param orderServiceUrl string
```

These are provided by:
- **Pipeline:** Hardcoded in `azure-pipelines.yml`
- **PowerShell Script:** Hardcoded in `deploy-apim.ps1`

### 2. Backend URL Stored in API Definition

```bicep
resource productApi = {
  properties: {
    serviceUrl: productServiceUrl  // Backend URL
    path: 'products'               // Frontend path
  }
}
```

### 3. Request Routing Logic

**Path Construction:**
```
Frontend URL = Gateway + API path + Operation URL
Backend URL  = serviceUrl + Operation URL

Example:
  Client calls:    GET https://apim-gateway/products/123
  APIM routes to:  GET https://backend/api/v1/123
```

**URL Mapping Table:**

| Client Request | APIM Path | Backend Request |
|----------------|-----------|-----------------|
| `GET /products/` | `/products` + `/` | `GET https://backend/api/v1/` |
| `GET /products/123` | `/products` + `/{id}` | `GET https://backend/api/v1/123` |
| `POST /products/` | `/products` + `/` | `POST https://backend/api/v1/` |
| `GET /orders/` | `/orders` + `/` | `GET https://backend-order/api/v1/` |
| `PATCH /orders/123/status` | `/orders` + `/{id}/status` | `PATCH https://backend-order/api/v1/123/status` |

## Usage

### Getting Subscription Key

**Azure Portal:**
1. Navigate to: Resource Groups → `rg-trello-microservices`
2. Click on: `apim-trello-{uniqueString}`
3. Go to: **Subscriptions** (left menu)
4. Click: **Unlimited** product
5. Click: **Show/hide keys**
6. Copy: **Primary key**

### Making API Calls

**With Subscription Key:**
```bash
# Get all products
curl https://apim-trello-xyz.azure-api.net/products \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"

# Get product by ID
curl https://apim-trello-xyz.azure-api.net/products/123 \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"

# Create product
curl -X POST https://apim-trello-xyz.azure-api.net/products \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE" \
  -H "Content-Type: application/json" \
  -d '{"name": "Laptop", "price": 999, "description": "Gaming laptop"}'

# Get all orders
curl https://apim-trello-xyz.azure-api.net/orders \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"
```

**Without Subscription Key (Will Fail):**
```bash
curl https://apim-trello-xyz.azure-api.net/products
# Response: 401 Unauthorized
# Message: "Access denied due to missing subscription key"
```

### Developer Portal

**URL:** `https://apim-trello-xyz.developer.azure-api.net`

**Features:**
- Interactive API documentation
- Try-it functionality (test APIs in browser)
- Subscription key management
- API reference with request/response examples

## Benefits

### Before API Gateway (Direct Service Access)
```
✗ Clients need to know 2 different URLs
✗ No centralized authentication
✗ No rate limiting
✗ No request analytics
✗ CORS configured per service
✗ Hard to implement API versioning
```

**Client Code:**
```javascript
// Product requests
fetch('https://app-trello-product-prod.azurewebsites.net/api/v1/')

// Order requests  
fetch('https://app-trello-order-prod.azurewebsites.net/api/v1/')
```

### After API Gateway (Unified Access)
```
✓ Single unified endpoint
✓ Centralized API key authentication
✓ Rate limiting (100 calls/min)
✓ Request/response analytics
✓ Centralized CORS policy
✓ Easy to version APIs (route v1 vs v2)
✓ Blue/green deployments (switch backends)
✓ Developer portal for documentation
```

**Client Code:**
```javascript
const apiKey = 'your-subscription-key';
const baseURL = 'https://apim-trello-xyz.azure-api.net';

// Product requests
fetch(`${baseURL}/products`, {
  headers: { 'Ocp-Apim-Subscription-Key': apiKey }
});

// Order requests
fetch(`${baseURL}/orders`, {
  headers: { 'Ocp-Apim-Subscription-Key': apiKey }
});
```

## Monitoring

### Azure Portal Metrics

**Navigation:** API Management → Monitoring → Metrics

**Available Metrics:**
- **Requests:** Total API calls
- **Failed Requests:** 4xx and 5xx errors
- **Capacity:** Gateway utilization
- **Duration:** Average response time
- **Bandwidth:** Data transferred

### Analytics Dashboard

**Navigation:** API Management → Analytics

**Insights:**
- Most called APIs
- Response time trends
- Geographic distribution of requests
- Top users by subscription key
- Error rate analysis

## Security Best Practices

### Production Recommendations

1. **Restrict CORS Origins:**
```xml
<allowed-origins>
  <origin>https://yourdomain.com</origin>
  <origin>https://app.yourdomain.com</origin>
</allowed-origins>
```

2. **Enable IP Filtering:**
```xml
<ip-filter action="allow">
  <address>192.168.1.1</address>
  <address-range from="10.0.0.1" to="10.0.0.255" />
</ip-filter>
```

3. **Use Products for Different Tiers:**
- Free tier: 100 calls/day
- Premium tier: Unlimited calls
- Partner tier: Higher rate limits

4. **Enable Request/Response Logging:**
```xml
<log-to-eventhub>
  <!-- Log requests for compliance -->
</log-to-eventhub>
```

5. **Rotate Subscription Keys Regularly:**
- Primary key for active use
- Secondary key for rotation without downtime

## Troubleshooting

### Common Issues

**Issue:** 401 Unauthorized
- **Cause:** Missing or invalid subscription key
- **Fix:** Include `Ocp-Apim-Subscription-Key` header

**Issue:** 429 Too Many Requests
- **Cause:** Rate limit exceeded (100 calls/min)
- **Fix:** Implement client-side throttling or request higher limits

**Issue:** 404 Not Found
- **Cause:** Incorrect API path
- **Fix:** Verify path matches APIM configuration (`/products` not `/api/v1/products`)

**Issue:** CORS Error in Browser
- **Cause:** OPTIONS preflight request blocked
- **Fix:** Ensure `OPTIONS` method allowed in CORS policy

## Cost

**Consumption Tier Pricing:**
- **API Calls:** $3.50 per million calls
- **Execution Time:** $0.28 per million executions
- **No monthly base cost**

**Example Monthly Cost:**
- 1 million API calls = $3.50
- 10 million API calls = $35.00

**Alternative Tiers:**
- **Developer:** $50/month (non-production use)
- **Basic:** $150/month (99.95% SLA)
- **Standard:** $700/month (99.95% SLA, higher limits)

## References

- **Bicep Template:** `infra/apim.bicep`
- **Deployment Script:** `infra/deploy-apim.ps1`
- **Pipeline Configuration:** `azure-pipelines/azure-pipelines.yml`
- **Azure Documentation:** https://docs.microsoft.com/en-us/azure/api-management/
- **Bicep Reference:** https://docs.microsoft.com/en-us/azure/templates/microsoft.apimanagement/

## Next Steps

After APIM is deployed:

1. ✓ Verify deployment in Azure Portal
2. ✓ Get subscription key from Subscriptions tab
3. ✓ Test API endpoints with Postman/curl
4. ✓ Access Developer Portal for documentation
5. ☐ Share gateway URL with frontend developers
6. ☐ Configure production CORS policies
7. ☐ Set up monitoring alerts
8. ☐ Implement different product tiers (optional)
