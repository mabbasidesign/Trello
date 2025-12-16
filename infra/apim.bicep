param location string = resourceGroup().location
param apimName string = 'apim-trello-${uniqueString(resourceGroup().id)}'
param publisherEmail string = 'admin@trello.com'
param publisherName string = 'Trello Microservices'
param productServiceUrl string
param orderServiceUrl string

// API Management Service
resource apim 'Microsoft.ApiManagement/service@2023-05-01-preview' = {
  name: apimName
  location: location
  sku: {
    name: 'Consumption'
    capacity: 0
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
    virtualNetworkType: 'None'
  }
}

// Product API
resource productApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apim
  name: 'product-api'
  properties: {
    displayName: 'Product API'
    apiRevision: '1'
    description: 'Product microservice API'
    subscriptionRequired: true
    serviceUrl: '${productServiceUrl}/products'
    path: 'products'
    protocols: [
      'https'
    ]
    isCurrent: true
  }
}

// Product API Operations
resource productGetAll 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: productApi
  name: 'get-products'
  properties: {
    displayName: 'Get all products'
    method: 'GET'
    urlTemplate: '/'
    description: 'Get all products with pagination'
  }
}

resource productGetById 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: productApi
  name: 'get-product-by-id'
  properties: {
    displayName: 'Get product by ID'
    method: 'GET'
    urlTemplate: '/{id}'
    templateParameters: [
      {
        name: 'id'
        type: 'string'
        required: true
      }
    ]
  }
}

resource productCreate 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: productApi
  name: 'create-product'
  properties: {
    displayName: 'Create product'
    method: 'POST'
    urlTemplate: '/'
  }
}

resource productUpdate 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: productApi
  name: 'update-product'
  properties: {
    displayName: 'Update product'
    method: 'PUT'
    urlTemplate: '/{id}'
    templateParameters: [
      {
        name: 'id'
        type: 'string'
        required: true
      }
    ]
  }
}

resource productDelete 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: productApi
  name: 'delete-product'
  properties: {
    displayName: 'Delete product'
    method: 'DELETE'
    urlTemplate: '/{id}'
    templateParameters: [
      {
        name: 'id'
        type: 'string'
        required: true
      }
    ]
  }
}

// Order API
resource orderApi 'Microsoft.ApiManagement/service/apis@2023-05-01-preview' = {
  parent: apim
  name: 'order-api'
  properties: {
    displayName: 'Order API'
    apiRevision: '1'
    description: 'Order microservice API'
    subscriptionRequired: true
    serviceUrl: '${orderServiceUrl}/orders'
    path: 'orders'
    protocols: [
      'https'
    ]
    isCurrent: true
  }
}

// Order API Operations
resource orderGetAll 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: orderApi
  name: 'get-orders'
  properties: {
    displayName: 'Get all orders'
    method: 'GET'
    urlTemplate: '/'
  }
}

resource orderGetById 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: orderApi
  name: 'get-order-by-id'
  properties: {
    displayName: 'Get order by ID'
    method: 'GET'
    urlTemplate: '/{id}'
    templateParameters: [
      {
        name: 'id'
        type: 'string'
        required: true
      }
    ]
  }
}

resource orderCreate 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: orderApi
  name: 'create-order'
  properties: {
    displayName: 'Create order'
    method: 'POST'
    urlTemplate: '/'
  }
}

resource orderUpdateStatus 'Microsoft.ApiManagement/service/apis/operations@2023-05-01-preview' = {
  parent: orderApi
  name: 'update-order-status'
  properties: {
    displayName: 'Update order status'
    method: 'PATCH'
    urlTemplate: '/{id}/status'
    templateParameters: [
      {
        name: 'id'
        type: 'string'
        required: true
      }
    ]
  }
}

// Product for API subscriptions
resource unlimitedProduct 'Microsoft.ApiManagement/service/products@2023-05-01-preview' = {
  parent: apim
  name: 'unlimited'
  properties: {
    displayName: 'Unlimited'
    description: 'Unlimited access to all APIs'
    subscriptionRequired: true
    approvalRequired: false
    state: 'published'
  }
}

// Associate APIs with product
resource productApiToProduct 'Microsoft.ApiManagement/service/products/apiLinks@2023-05-01-preview' = {
  parent: unlimitedProduct
  name: 'product-api-link'
  properties: {
    apiId: productApi.id
  }
}

resource orderApiToProduct 'Microsoft.ApiManagement/service/products/apiLinks@2023-05-01-preview' = {
  parent: unlimitedProduct
  name: 'order-api-link'
  properties: {
    apiId: orderApi.id
  }
}

// CORS Policy for Product API
resource productApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  parent: productApi
  name: 'policy'
  properties: {
    value: '''
      <policies>
        <inbound>
          <base />
          <cors allow-credentials="false">
            <allowed-origins>
              <origin>*</origin>
            </allowed-origins>
            <allowed-methods>
              <method>GET</method>
              <method>POST</method>
              <method>PUT</method>
              <method>DELETE</method>
              <method>OPTIONS</method>
            </allowed-methods>
            <allowed-headers>
              <header>*</header>
            </allowed-headers>
          </cors>
          <rate-limit calls="100" renewal-period="60" />
        </inbound>
        <backend>
          <base />
        </backend>
        <outbound>
          <base />
        </outbound>
        <on-error>
          <base />
        </on-error>
      </policies>
    '''
    format: 'xml'
  }
}

// CORS Policy for Order API
resource orderApiPolicy 'Microsoft.ApiManagement/service/apis/policies@2023-05-01-preview' = {
  parent: orderApi
  name: 'policy'
  properties: {
    value: '''
      <policies>
        <inbound>
          <base />
          <cors allow-credentials="false">
            <allowed-origins>
              <origin>*</origin>
            </allowed-origins>
            <allowed-methods>
              <method>GET</method>
              <method>POST</method>
              <method>PATCH</method>
              <method>OPTIONS</method>
            </allowed-methods>
            <allowed-headers>
              <header>*</header>
            </allowed-headers>
          </cors>
          <rate-limit calls="100" renewal-period="60" />
        </inbound>
        <backend>
          <base />
        </backend>
        <outbound>
          <base />
        </outbound>
        <on-error>
          <base />
        </on-error>
      </policies>
    '''
    format: 'xml'
  }
}

output apimName string = apim.name
output apimGatewayUrl string = apim.properties.gatewayUrl
output apimDeveloperPortalUrl string = apim.properties.developerPortalUrl
output productApiPath string = 'https://${apim.properties.gatewayUrl}/products'
output orderApiPath string = 'https://${apim.properties.gatewayUrl}/orders'
