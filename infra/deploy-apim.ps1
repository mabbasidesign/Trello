# Deploy Azure API Management
# This script deploys API Management in front of Product and Order services

param(
    [string]$ResourceGroupName = "rg-trello-microservices",
    [string]$Location = "canadaeast",
    [string]$SubscriptionId = "f2f7f036-0d40-48ca-a532-eb590c2eac8b"
)

Write-Host "Deploying Azure API Management..." -ForegroundColor Cyan

# Set subscription
az account set --subscription $SubscriptionId

# Get App Service URLs
Write-Host "Getting App Service URLs..." -ForegroundColor Yellow
$productServiceUrl = "https://app-trello-product-prod.azurewebsites.net/api/v1"
$orderServiceUrl = "https://app-trello-order-prod.azurewebsites.net/api/v1"

Write-Host "Product Service URL: $productServiceUrl" -ForegroundColor Green
Write-Host "Order Service URL: $orderServiceUrl" -ForegroundColor Green

# Deploy APIM
Write-Host "Deploying API Management instance..." -ForegroundColor Yellow
Write-Host "Note: APIM deployment can take 5-10 minutes for Consumption tier" -ForegroundColor Cyan

$deployment = az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file "infra/apim.bicep" `
    --parameters `
        location=$Location `
        publisherEmail="admin@trello.com" `
        publisherName="Trello Microservices" `
        productServiceUrl=$productServiceUrl `
        orderServiceUrl=$orderServiceUrl `
    --query 'properties.outputs' `
    --output json | ConvertFrom-Json

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nAPI Management deployed successfully!" -ForegroundColor Green
    Write-Host "Gateway URL: $($deployment.apimGatewayUrl.value)" -ForegroundColor Cyan
    Write-Host "Developer Portal: $($deployment.apimDeveloperPortalUrl.value)" -ForegroundColor Cyan
    Write-Host "`nAPI Endpoints:" -ForegroundColor Yellow
    Write-Host "  Product API: $($deployment.productApiPath.value)" -ForegroundColor Green
    Write-Host "  Order API: $($deployment.orderApiPath.value)" -ForegroundColor Green
    
    Write-Host "`nNext Steps:" -ForegroundColor Yellow
    Write-Host "1. Get API subscription key from Azure Portal" -ForegroundColor White
    Write-Host "2. Test APIs using: Ocp-Apim-Subscription-Key header" -ForegroundColor White
    Write-Host "3. Access Developer Portal to manage API subscriptions" -ForegroundColor White
} else {
    Write-Host "Deployment failed!" -ForegroundColor Red
    exit 1
}
