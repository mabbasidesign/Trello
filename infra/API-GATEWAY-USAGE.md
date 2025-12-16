# API Gateway Usage Guide

This guide explains how to use the unified API Gateway (Azure API Management) for the Trello microservices project.

## Overview

**API Gateway URL:** `https://apim-trello-fumbtexxi35ii.azure-api.net`

**Available Endpoints:**
- Products API: `/products`
- Orders API: `/orders`

**Authentication:** All requests require a subscription key in the `Ocp-Apim-Subscription-Key` header.

---

## Getting Your Subscription Key

### Method 1: Azure Portal

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to: **apim-trello-fumbtexxi35ii**
3. In left menu, click **Subscriptions**
4. Click on your subscription (e.g., "unlimited-subscription")
5. Click **Show keys** button
6. Copy the **Primary key**

### Method 2: Create New Subscription

If no subscription exists:

1. Go to **apim-trello-fumbtexxi35ii** → **Subscriptions**
2. Click **+ Add**
3. Fill in:
   - **Name**: `my-subscription`
   - **Display name**: `My API Subscription`
   - **Scope**: Select **Product** → **Unlimited**
   - **State**: Active
4. Click **Create**
5. Click **Show keys** and copy **Primary key**

**Example Key:** `7d73c2d85d3544fdaea1d4e291e10cc3`

---

## Testing with PowerShell

### Basic Test

```powershell
# Set your subscription key
$key = "7d73c2d85d3544fdaea1d4e291e10cc3"

# Test Product API
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

**Expected Output:**
```json
{
  "items": [
    {
      "id": 1,
      "name": "Laptop",
      "description": "High-performance laptop",
      "price": 999.99,
      "stock": 10
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 4,
  "totalPages": 1
}
```

### Test with Error Handling

```powershell
$key = "7d73c2d85d3544fdaea1d4e291e10cc3"

try {
    $response = Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
        -Headers @{"Ocp-Apim-Subscription-Key"=$key}
    
    Write-Host "Success! Found $($response.totalCount) products" -ForegroundColor Green
    $response.items | Format-Table -Property id, name, price, stock
}
catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}
```

### Test with Verbose Output

```powershell
$key = "7d73c2d85d3544fdaea1d4e291e10cc3"

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key} `
  -Verbose
```

**What `-Verbose` shows:**
- Request method and URL
- Response size
- Content type
- Request/response timing

---

## All Product API Examples

### Get All Products (Paginated)

```powershell
$key = "YOUR_KEY_HERE"

# Get first page (default: 10 items)
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}

# Get with custom pagination
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products?page=2&pageSize=5" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### Get Product by ID

```powershell
$key = "YOUR_KEY_HERE"
$productId = 1

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/$productId" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### Create Product

```powershell
$key = "YOUR_KEY_HERE"

$newProduct = @{
    name = "Gaming Mouse"
    description = "RGB Gaming Mouse"
    price = 49.99
    stock = 100
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Method POST `
  -Headers @{
      "Ocp-Apim-Subscription-Key" = $key
      "Content-Type" = "application/json"
  } `
  -Body $newProduct
```

### Update Product

```powershell
$key = "YOUR_KEY_HERE"
$productId = 1

$updatedProduct = @{
    name = "Updated Laptop"
    description = "Ultra high-performance laptop"
    price = 1299.99
    stock = 5
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/$productId" `
  -Method PUT `
  -Headers @{
      "Ocp-Apim-Subscription-Key" = $key
      "Content-Type" = "application/json"
  } `
  -Body $updatedProduct
```

### Delete Product

```powershell
$key = "YOUR_KEY_HERE"
$productId = 1

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/$productId" `
  -Method DELETE `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

---

## All Order API Examples

### Get All Orders

```powershell
$key = "YOUR_KEY_HERE"

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### Get Order by ID

```powershell
$key = "YOUR_KEY_HERE"
$orderId = 1

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders/$orderId" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### Create Order

```powershell
$key = "YOUR_KEY_HERE"

$newOrder = @{
    customerId = 123
    items = @(
        @{
            productId = 1
            quantity = 2
            price = 999.99
        }
    )
    totalAmount = 1999.98
} | ConvertTo-Json -Depth 3

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders" `
  -Method POST `
  -Headers @{
      "Ocp-Apim-Subscription-Key" = $key
      "Content-Type" = "application/json"
  } `
  -Body $newOrder
```

### Update Order Status

```powershell
$key = "YOUR_KEY_HERE"
$orderId = 1

$statusUpdate = @{
    status = "Shipped"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders/$orderId/status" `
  -Method PATCH `
  -Headers @{
      "Ocp-Apim-Subscription-Key" = $key
      "Content-Type" = "application/json"
  } `
  -Body $statusUpdate
```

---

## Testing with Developer Portal

The Developer Portal provides an interactive web interface for testing APIs without code.

### Accessing the Portal

**URL:** `https://apim-trello-fumbtexxi35ii.developer.azure-api.net/`

### Steps to Test

1. **Sign In**
   - Click **Sign In** (top right)
   - Use your Microsoft/Azure account

2. **Navigate to APIs**
   - Click **APIs** in top menu
   - Select **Product API** or **Order API**

3. **Choose an Operation**
   - Click on operation (e.g., "Get all products")
   - View request/response schemas

4. **Try the API**
   - Click **Try it** button
   - Portal automatically includes subscription key
   - Add query parameters if needed
   - Click **Send**

5. **View Results**
   - See HTTP status code (200 = success)
   - View response headers
   - View response body (JSON data)

### Portal Features

- ✅ **Auto-authentication** - Subscription key included automatically
- ✅ **Interactive docs** - See all available operations
- ✅ **Schema validation** - Shows request/response formats
- ✅ **Code samples** - Generate code for different languages
- ✅ **Try it feature** - Test APIs in browser

---

## Using in Applications

### C# / .NET

```csharp
using System.Net.Http;
using System.Net.Http.Headers;

var client = new HttpClient();
client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "YOUR_KEY_HERE");

var response = await client.GetAsync("https://apim-trello-fumbtexxi35ii.azure-api.net/products");
var content = await response.Content.ReadAsStringAsync();
Console.WriteLine(content);
```

### JavaScript / Fetch API

```javascript
const apiKey = "YOUR_KEY_HERE";

fetch("https://apim-trello-fumbtexxi35ii.azure-api.net/products", {
  headers: {
    "Ocp-Apim-Subscription-Key": apiKey
  }
})
  .then(response => response.json())
  .then(data => console.log(data))
  .catch(error => console.error("Error:", error));
```

### Python

```python
import requests

api_key = "YOUR_KEY_HERE"
headers = {"Ocp-Apim-Subscription-Key": api_key}

response = requests.get(
    "https://apim-trello-fumbtexxi35ii.azure-api.net/products",
    headers=headers
)

print(response.json())
```

### cURL (Linux/Mac/Git Bash)

```bash
curl https://apim-trello-fumbtexxi35ii.azure-api.net/products \
  -H "Ocp-Apim-Subscription-Key: YOUR_KEY_HERE"
```

---

## Common Errors and Solutions

### 401 Unauthorized

**Error:**
```json
{
  "statusCode": 401,
  "message": "Access denied due to missing subscription key"
}
```

**Solution:**
- Add `Ocp-Apim-Subscription-Key` header
- Verify key is correct (no extra spaces)

**PowerShell Fix:**
```powershell
# Wrong (missing header)
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products"

# Correct
$key = "YOUR_KEY"
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### 404 Not Found

**Causes:**
- Wrong endpoint path
- Backend service down
- APIM routing misconfigured

**Check:**
```powershell
# Test backend directly
Invoke-RestMethod -Uri "https://app-trello-product-prod.azurewebsites.net/api/v1/products"

# Test through APIM
$key = "YOUR_KEY"
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### 429 Too Many Requests

**Error:**
```json
{
  "statusCode": 429,
  "message": "Rate limit exceeded"
}
```

**Cause:** Exceeded 100 requests per 60 seconds

**Solution:** Wait 60 seconds or request rate limit increase

### 500 Internal Server Error

**Causes:**
- Backend service error
- Database connection issue
- Invalid request payload

**Debug:**
```powershell
# Test backend health
Invoke-RestMethod -Uri "https://app-trello-product-prod.azurewebsites.net/api/v1/products"

# Check APIM logs in Azure Portal
# Navigate to: apim-trello-fumbtexxi35ii → Monitoring → Logs
```

---

## Rate Limiting

**Current Limits:**
- **100 calls per 60 seconds** per subscription key
- Applies to all endpoints combined

**Headers in Response:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 60
```

**Testing Rate Limit:**
```powershell
$key = "YOUR_KEY"
for ($i = 1; $i -le 101; $i++) {
    Write-Host "Request $i" -ForegroundColor Cyan
    try {
        Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
          -Headers @{"Ocp-Apim-Subscription-Key"=$key}
    }
    catch {
        Write-Host "Rate limit hit at request $i" -ForegroundColor Red
        break
    }
}
```

---

## CORS Configuration

**Allowed Origins:** All (`*`)  
**Allowed Methods:** GET, POST, PUT, PATCH, DELETE, OPTIONS  
**Allowed Headers:** All (`*`)

This means you can call the API from any frontend application (React, Angular, Vue, etc.) without CORS issues.

**Frontend Example (React):**
```javascript
const fetchProducts = async () => {
  const response = await fetch(
    "https://apim-trello-fumbtexxi35ii.azure-api.net/products",
    {
      headers: {
        "Ocp-Apim-Subscription-Key": "YOUR_KEY_HERE"
      }
    }
  );
  const data = await response.json();
  return data;
};
```

---

## Monitoring API Usage

### Via Azure Portal

1. Go to **apim-trello-fumbtexxi35ii**
2. Click **Analytics** in left menu
3. View:
   - Request count by endpoint
   - Response times
   - Error rates
   - Top consumers

### Via Application Insights (if enabled)

1. Navigate to **Application Insights** resource
2. View metrics:
   - Request duration
   - Failed requests
   - Geographic distribution

---

## Security Best Practices

### 1. Protect Your Subscription Key

❌ **Don't:**
- Commit keys to Git
- Share keys in public documentation
- Use same key for all environments

✅ **Do:**
- Store in environment variables
- Use Azure Key Vault
- Rotate keys regularly

### 2. Environment Variables (PowerShell)

```powershell
# Set environment variable
$env:APIM_SUBSCRIPTION_KEY = "7d73c2d85d3544fdaea1d4e291e10cc3"

# Use in scripts
$key = $env:APIM_SUBSCRIPTION_KEY
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

### 3. Multiple Subscriptions

Create different subscriptions for:
- **Development** - Testing and debugging
- **Production** - Live applications
- **Partners** - Third-party integrations

Each can have different rate limits and can be revoked independently.

---

## Comparing Direct Backend vs API Gateway

### Direct Backend URL

```powershell
# Product Service
Invoke-RestMethod -Uri "https://app-trello-product-prod.azurewebsites.net/api/v1/products"

# Order Service
Invoke-RestMethod -Uri "https://app-trello-order-prod.azurewebsites.net/api/v1/orders"
```

**Characteristics:**
- ❌ No authentication
- ❌ No rate limiting
- ❌ Multiple URLs to manage
- ❌ No centralized monitoring
- ✅ Direct access (slightly faster)

### API Gateway URL

```powershell
$key = "YOUR_KEY"

# Products
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}

# Orders
Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders" `
  -Headers @{"Ocp-Apim-Subscription-Key"=$key}
```

**Characteristics:**
- ✅ Subscription key authentication
- ✅ Rate limiting (100/min)
- ✅ Single unified URL
- ✅ Centralized monitoring
- ✅ CORS enabled
- ✅ Versioning support
- ⚠️ Small latency overhead (~10-50ms)

**Recommendation:** Always use API Gateway for production applications

---

## Quick Reference

| Action | PowerShell Command |
|--------|-------------------|
| **Get Products** | `Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products" -Headers @{"Ocp-Apim-Subscription-Key"="$key"}` |
| **Get Product** | `Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/1" -Headers @{"Ocp-Apim-Subscription-Key"="$key"}` |
| **Create Product** | `Invoke-RestMethod -Uri "..." -Method POST -Headers @{...} -Body $json` |
| **Update Product** | `Invoke-RestMethod -Uri "..." -Method PUT -Headers @{...} -Body $json` |
| **Delete Product** | `Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/products/1" -Method DELETE -Headers @{"Ocp-Apim-Subscription-Key"="$key"}` |
| **Get Orders** | `Invoke-RestMethod -Uri "https://apim-trello-fumbtexxi35ii.azure-api.net/orders" -Headers @{"Ocp-Apim-Subscription-Key"="$key"}` |

---

## Support & Documentation

- **APIM Setup Guide:** [APIM-SETUP.md](APIM-SETUP.md)
- **Troubleshooting:** [TROUBLESHOOTING.md](TROUBLESHOOTING.md#issue-2-apim-backend-routing-404-errors)
- **Azure Portal:** https://portal.azure.com
- **Developer Portal:** https://apim-trello-fumbtexxi35ii.developer.azure-api.net/
- **Microsoft Docs:** https://docs.microsoft.com/azure/api-management/

---

**Last Updated:** December 16, 2025  
**API Gateway:** apim-trello-fumbtexxi35ii  
**Region:** Canada East
