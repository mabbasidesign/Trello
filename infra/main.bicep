targetScope = 'subscription'

@description('The name of the resource group')
param resourceGroupName string = 'rg-trello-microservices'

@description('The location for all resources')
param location string = 'eastus'

@description('The environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('The Service Bus SKU')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param serviceBusSku string = 'Standard'

// Create Resource Group
resource resourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: {
    environment: environment
    project: 'Trello'
    managedBy: 'Bicep'
  }
}

// Deploy Service Bus
module serviceBus 'servicebus.bicep' = {
  scope: resourceGroup
  name: 'serviceBusDeployment'
  params: {
    location: location
    serviceBusSku: serviceBusSku
    zoneRedundant: environment == 'prod'
  }
}

@description('The Service Bus connection string')
output serviceBusConnectionString string = serviceBus.outputs.serviceBusConnectionString

@description('The Service Bus namespace name')
output serviceBusNamespaceName string = serviceBus.outputs.serviceBusNamespaceName

@description('The resource group name')
output resourceGroupName string = resourceGroup.name
