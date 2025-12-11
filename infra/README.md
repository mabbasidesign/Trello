# Azure Service Bus Infrastructure

This folder contains Bicep templates for deploying Azure Service Bus infrastructure for the Trello microservices application.

## Architecture

- **Service Bus Namespace**: Central messaging hub
- **Queues**:
  - `orders`: Queue for order-related messages
  - `products`: Queue for product-related messages
- **Topics & Subscriptions**:
  - `order-notifications`: Topic for broadcasting order events
    - `email-subscription`: Subscription for email notifications
    - `sms-subscription`: Subscription for SMS notifications

## Prerequisites

- Azure CLI installed and configured
- Azure subscription
- Appropriate permissions to create resources

## Deployment

### Deploy to Azure

```powershell
# Login to Azure
az login

# Set subscription (if you have multiple)
az account set --subscription "<your-subscription-id>"

# Deploy the infrastructure
az deployment sub create `
  --location eastus `
  --template-file main.bicep `
  --parameters environment=dev

# For production deployment
az deployment sub create `
  --location eastus `
  --template-file main.bicep `
  --parameters environment=prod serviceBusSku=Premium
```

### Get Connection String

After deployment, retrieve the connection string:

```powershell
az deployment sub show `
  --name main `
  --query properties.outputs.serviceBusConnectionString.value `
  --output tsv
```

### Validate Templates

```powershell
# Validate main template
az deployment sub validate `
  --location eastus `
  --template-file main.bicep

# Validate Service Bus template
az deployment group validate `
  --resource-group rg-trello-microservices `
  --template-file servicebus.bicep
```

## Parameters

### main.bicep

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| resourceGroupName | string | rg-trello-microservices | Name of the resource group |
| location | string | eastus | Azure region |
| environment | string | dev | Environment (dev/staging/prod) |
| serviceBusSku | string | Standard | Service Bus SKU |

### servicebus.bicep

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| serviceBusNamespaceName | string | trello-servicebus-{unique} | Service Bus namespace name |
| location | string | resourceGroup().location | Azure region |
| serviceBusSku | string | Standard | Pricing tier |
| zoneRedundant | bool | false | Enable zone redundancy |

## Outputs

- `serviceBusConnectionString`: Connection string for the Service Bus namespace
- `serviceBusNamespaceName`: Name of the deployed Service Bus namespace
- `resourceGroupName`: Name of the resource group

## Service Bus Configuration

### Queues
- **Lock Duration**: 1 minute
- **Max Size**: 1 GB
- **Message TTL**: 14 days
- **Max Delivery Count**: 10
- **Dead Letter Queue**: Enabled

### Topics
- **Max Size**: 1 GB
- **Message TTL**: 14 days
- **Duplicate Detection**: Disabled
- **Ordering Support**: Enabled

## Integration with Services

Update your `appsettings.json` in both services:

```json
{
  "ServiceBus": {
    "ConnectionString": "<connection-string-from-output>",
    "OrderQueueName": "orders",
    "ProductQueueName": "products",
    "OrderNotificationTopic": "order-notifications"
  }
}
```

## Clean Up

To delete all resources:

```powershell
az group delete --name rg-trello-microservices --yes --no-wait
```

## Monitoring

View Service Bus metrics in Azure Portal:
1. Navigate to your Service Bus namespace
2. Select "Metrics" from the left menu
3. Monitor message counts, throughput, and errors

## Cost Optimization

- **Development**: Use Basic or Standard tier
- **Production**: Use Standard or Premium tier with zone redundancy
- **Scaling**: Premium tier supports auto-scaling and VNet integration
