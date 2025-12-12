@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the App Service Plan')
param appServicePlanName string = 'asp-trello-microservices'

@description('SKU for the App Service Plan')
@allowed([
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1v2'
  'P2v2'
  'P3v2'
])
param appServicePlanSku string = 'B1'

@description('Container Registry URL')
param containerRegistryUrl string = 'trelloacr.azurecr.io'

@description('Container Registry Username')
@secure()
param containerRegistryUsername string

@description('Container Registry Password')
@secure()
param containerRegistryPassword string

@description('Service Bus Connection String')
@secure()
param serviceBusConnectionString string

@description('Product Service Database Connection String')
@secure()
param productDbConnectionString string

@description('Order Service Database Connection String')
@secure()
param orderDbConnectionString string

@description('Docker image tag')
param imageTag string = 'latest'

// App Service Plan (Linux)
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: appServicePlanSku
    capacity: 1
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Product Service App Service
resource productAppService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-trello-product-prod'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistryUrl}/trello-microservices-product:${imageTag}'
      appCommandLine: ''
      alwaysOn: true
      ftpsState: 'Disabled'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: 35
      detailedErrorLoggingEnabled: true
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${containerRegistryUrl}'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: containerRegistryUsername
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: containerRegistryPassword
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ServiceBus__ConnectionString'
          value: serviceBusConnectionString
        }
        {
          name: 'ASPNETCORE_HTTP_PORTS'
          value: '8080'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultSQLConnection'
          connectionString: productDbConnectionString
          type: 'SQLAzure'
        }
      ]
    }
    httpsOnly: true
    clientAffinityEnabled: false
  }
}

// Order Service App Service
resource orderAppService 'Microsoft.Web/sites@2022-09-01' = {
  name: 'app-trello-order-prod'
  location: location
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistryUrl}/trello-microservices-order:${imageTag}'
      appCommandLine: ''
      alwaysOn: true
      ftpsState: 'Disabled'
      httpLoggingEnabled: true
      logsDirectorySizeLimit: 35
      detailedErrorLoggingEnabled: true
      appSettings: [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${containerRegistryUrl}'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: containerRegistryUsername
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: containerRegistryPassword
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ServiceBus__ConnectionString'
          value: serviceBusConnectionString
        }
        {
          name: 'ASPNETCORE_HTTP_PORTS'
          value: '8080'
        }
      ]
      connectionStrings: [
        {
          name: 'DefaultSQLConnection'
          connectionString: orderDbConnectionString
          type: 'SQLAzure'
        }
      ]
    }
    httpsOnly: true
    clientAffinityEnabled: false
  }
}

output productAppServiceName string = productAppService.name
output productAppServiceUrl string = 'https://${productAppService.properties.defaultHostName}'
output orderAppServiceName string = orderAppService.name
output orderAppServiceUrl string = 'https://${orderAppService.properties.defaultHostName}'
