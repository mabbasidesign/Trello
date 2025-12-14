# Deploy Key Vault to Azure
# Run this script to deploy the Key Vault using Azure PowerShell

# Variables
$resourceGroup = "rg-trello-microservices"
$location = "canadaeast"
$sqlPassword = "TrelloSQL@2025!"

# Get Service Bus connection string from user
Write-Host "Please paste the Service Bus Primary Connection String:" -ForegroundColor Cyan
$serviceBusConnectionString = Read-Host

# Deploy Key Vault
Write-Host "`nDeploying Key Vault..." -ForegroundColor Green
az deployment group create `
  --resource-group $resourceGroup `
  --template-file infra/keyvault.bicep `
  --parameters sqlAdminPassword=$sqlPassword `
               serviceBusConnectionString=$serviceBusConnectionString

# Get Key Vault details
$kvName = az deployment group show `
  --resource-group $resourceGroup `
  --name keyvault `
  --query properties.outputs.keyVaultName.value -o tsv

$kvUri = az deployment group show `
  --resource-group $resourceGroup `
  --name keyvault `
  --query properties.outputs.keyVaultUri.value -o tsv

Write-Host "`nKey Vault deployed: $kvName" -ForegroundColor Green
Write-Host "Key Vault URI: $kvUri" -ForegroundColor Green

# Enable App Service managed identity
Write-Host "`nEnabling managed identity for App Services..." -ForegroundColor Green
az webapp identity assign --name app-trello-product-prod --resource-group $resourceGroup
az webapp identity assign --name app-trello-order-prod --resource-group $resourceGroup

# Get managed identity principal IDs
$productPrincipalId = az webapp identity show --name app-trello-product-prod --resource-group $resourceGroup --query principalId -o tsv
$orderPrincipalId = az webapp identity show --name app-trello-order-prod --resource-group $resourceGroup --query principalId -o tsv

# Grant Key Vault access to App Services
Write-Host "`nGranting Key Vault access to App Services..." -ForegroundColor Green
az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee $productPrincipalId `
  --scope "/subscriptions/f2f7f036-0d40-48ca-a532-eb590c2eac8b/resourceGroups/$resourceGroup/providers/Microsoft.KeyVault/vaults/$kvName"

az role assignment create `
  --role "Key Vault Secrets User" `
  --assignee $orderPrincipalId `
  --scope "/subscriptions/f2f7f036-0d40-48ca-a532-eb590c2eac8b/resourceGroups/$resourceGroup/providers/Microsoft.KeyVault/vaults/$kvName"

# Configure App Services to use Key Vault
Write-Host "`nConfiguring App Services to use Key Vault secrets..." -ForegroundColor Green

# SQL Connection String from Key Vault
$sqlSecretUri = "${kvUri}secrets/sql-admin-password"
$serviceBusSecretUri = "${kvUri}secrets/servicebus-connection-string"

# Build connection string with Key Vault reference
$sqlConnectionString = "Server=sql-trello-ziutkivc6smnk.database.windows.net;Database=TrelloProducts;User Id=sqladmin;Password=@Microsoft.KeyVault(SecretUri=$sqlSecretUri);TrustServerCertificate=true"
$orderConnectionString = "Server=sql-trello-ziutkivc6smnk.database.windows.net;Database=TrelloOrders;User Id=sqladmin;Password=@Microsoft.KeyVault(SecretUri=$sqlSecretUri);TrustServerCertificate=true"

az webapp config appsettings set `
  --name app-trello-product-prod `
  --resource-group $resourceGroup `
  --settings ConnectionStrings__DefaultSQLConnection="$sqlConnectionString" `
             ServiceBus__ConnectionString="@Microsoft.KeyVault(SecretUri=$serviceBusSecretUri)"

az webapp config appsettings set `
  --name app-trello-order-prod `
  --resource-group $resourceGroup `
  --settings ConnectionStrings__DefaultSQLConnection="$orderConnectionString" `
             ServiceBus__ConnectionString="@Microsoft.KeyVault(SecretUri=$serviceBusSecretUri)"

Write-Host "`n✅ Key Vault deployment complete!" -ForegroundColor Green
Write-Host "App Services now use Key Vault for secrets." -ForegroundColor Green
