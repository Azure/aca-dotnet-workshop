targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

@description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The name of the container apps environment.')
param containerAppsEnvironmentName string

// Services
@description('The name of the service for the backend api service. The name is use as Dapr App ID.')
param backendApiServiceName string

@description('The name of the service for the backend processor service. The name is use as Dapr App ID and as the name of service bus topic subscription.')
param backendProcessorServiceName string

@description('The name of the service for the frontend web app service. The name is use as Dapr App ID.')
param frontendWebAppServiceName string

// Service Bus
@description('The name of the service bus namespace.')
param serviceBusName string

@description('The name of the service bus topic.')
param serviceBusTopicName string

@description('The name of the service bus topic\'s authorization rule.')
param serviceBusTopicAuthorizationRuleName string

// Cosmos DB
@description('The name of the provisioned Cosmos DB resource.')
param cosmosDbName string 

@description('The name of the provisioned Cosmos DB\'s database.')
param cosmosDbDatabaseName string

@description('The name of Cosmos DB\'s collection.')
param cosmosDbCollectionName string

// Key Vault
@description('The resource ID of the key vault.')
param keyVaultId string

@description('The name of the secret containing the SendGrid API key value for the Backend Background Processor Service.')
param sendGridKeySecretName string

@secure()
@description('The SendGrid API key for for Backend Background Processor Service.')
param sendGridKeySecretValue string

@description('The name of the secret containing the External Azure Storage Access key for the Backend Background Processor Service.')
param externalStorageKeySecretName string

// External Storage
@description('The name of the external Azure Storage Account.')
param externalStorageAccountName string

// Container Registry & Images
@description('The name of the container registry.')
param containerRegistryName string

@description('The username of the container registry user.')
param containerRegistryUsername string

// We disable lint of this line as it is not a secret
#disable-next-line secure-secrets-in-params
param containerRegistryPasswordRefName string

@secure()
param containerRegistryPassword string

@description('The image for the backend api service.')
param backendApiServiceImage string

@description('The image for the backend processor service.')
param backendProcessoServiceImage string

@description('The image for the frontend web app service.')
param frontendWebAppServiceImage string

@description('The name of the application insights.')
param applicationInsightsName string

// ------------------
// RESOURCES
// ------------------

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: containerAppsEnvironmentName
}

//Reference to AppInsights resource
resource applicationInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: applicationInsightsName
}

module frontendWebAppService 'container-apps/webapp-frontend-service.bicep' = {
  name: 'frontendWebAppService-${uniqueString(resourceGroup().id)}'
  params: {
    frontendWebAppServiceName: frontendWebAppServiceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironment.id
    containerRegistryName: containerRegistryName
    containerRegistryUsername: containerRegistryUsername
    containerRegistryPasswordRefName: containerRegistryPasswordRefName
    containerRegistryPassword: containerRegistryPassword
    frontendWebAppServiceImage: frontendWebAppServiceImage
    appInsightsInstrumentationKey: applicationInsights.properties.InstrumentationKey
  }
}

module backendApiService 'container-apps/webapi-backend-service.bicep' = {
  name: 'backendApiService-${uniqueString(resourceGroup().id)}'
  params: {
    backendApiServiceName: backendApiServiceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironment.id
    serviceBusName: serviceBusName
    serviceBusTopicName: serviceBusTopicName
    containerRegistryName: containerRegistryName
    containerRegistryUsername: containerRegistryUsername
    containerRegistryPasswordRefName: containerRegistryPasswordRefName
    containerRegistryPassword: containerRegistryPassword
    backendApiServiceImage: backendApiServiceImage
    cosmosDbName: cosmosDbName
    cosmosDbDatabaseName: cosmosDbDatabaseName
    cosmosDbCollectionName: cosmosDbCollectionName
    appInsightsInstrumentationKey: applicationInsights.properties.InstrumentationKey
  }
}

module backendProcessorService 'container-apps/processor-backend-service.bicep' = {
  name: 'backendProcessorService-${uniqueString(resourceGroup().id)}'
  params: {
    backendProcessorServiceName: backendProcessorServiceName
    location: location
    tags: tags
    containerAppsEnvironmentId: containerAppsEnvironment.id
    keyVaultId: keyVaultId
    serviceBusName: serviceBusName
    serviceBusTopicName: serviceBusTopicName
    serviceBusTopicAuthorizationRuleName: serviceBusTopicAuthorizationRuleName
    containerRegistryName: containerRegistryName
    containerRegistryUsername: containerRegistryUsername
    containerRegistryPasswordRefName: containerRegistryPasswordRefName
    containerRegistryPassword: containerRegistryPassword
    sendGridKeySecretName: sendGridKeySecretName
    sendGridKeySecretValue: sendGridKeySecretValue
    externalStorageAccountName: externalStorageAccountName
    externalStorageKeySecretName:externalStorageKeySecretName
    backendProcessoServiceImage: backendProcessoServiceImage
    appInsightsInstrumentationKey: applicationInsights.properties.InstrumentationKey
  }
}

// ------------------
// OUTPUTS
// ------------------

@description('The name of the container app for the backend processor service.')
output backendProcessorServiceContainerAppName string = backendProcessorService.outputs.backendProcessorServiceContainerAppName

@description('The name of the container app for the backend api service.')
output backendApiServiceContainerAppName string = backendApiService.outputs.backendApiServiceContainerAppName

@description('The name of the container app for the front end web app service.')
output frontendWebAppServiceContainerAppName string = frontendWebAppService.outputs.frontendWebAppServiceContainerAppName

@description('The FQDN of the front end web app.')
output frontendWebAppServiceFQDN string = frontendWebAppService.outputs.frontendWebAppServiceFQDN

@description('The FQDN of the backend web app')
output backendApiServiceFQDN string  = backendApiService.outputs.backendApiServiceFQDN
