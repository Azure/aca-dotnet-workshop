targetScope = 'resourceGroup'

// ------------------
//    PARAMETERS
// ------------------

@description('The location where the resources will be created.')
param location string = resourceGroup().location

@description('Optional. The tags to be assigned to the created resources.')
param tags object = {}

@description('The resource Id of the container apps environment.')
param containerAppsEnvironmentId string

@description('The name of the service for the frontend web app service. The name is use as Dapr App ID.')
param frontendWebAppServiceName string

// Container Registry & Image
@description('The name of the container registry.')
param containerRegistryName string

@description('The username of the container registry user.')
param containerRegistryUsername string

@description('The password name of the container registry.')
// We disable lint of this line as it is not a secret
#disable-next-line secure-secrets-in-params
param containerRegistryPasswordRefName string

@secure()
param containerRegistryPassword string

@description('The image for the frontend web app service.')
param frontendWebAppServiceImage string


@secure()
@description('The Application Insights Instrumentation.')
param appInsightsInstrumentationKey string

// ------------------
// VARIABLES
// ------------------

// var containerAppName = 'ca-${frontendWebAppServiceName}'
var containerAppName = frontendWebAppServiceName

// ------------------
// RESOURCES
// ------------------

resource frontendWebAppService 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: containerAppName
  location: location
  tags: tags
  properties: {
    managedEnvironmentId: containerAppsEnvironmentId
    configuration: {
      activeRevisionsMode: 'single'
      ingress: {
        external: true
        targetPort: 80
      }
      dapr: {
        enabled: true
        appId: frontendWebAppServiceName
        appProtocol: 'http'
        appPort: 80
        logLevel: 'info'
        enableApiLogging: true
      }
      secrets: [
        {
          name: 'appinsights-key'
          value: appInsightsInstrumentationKey
        }
        {
          name: containerRegistryPasswordRefName
          value: containerRegistryPassword
        }
      ]
      registries: [
        {
          server: '${containerRegistryName}.azurecr.io'
          username: containerRegistryUsername
          passwordSecretRef: containerRegistryPasswordRefName
        }
      ]
    }
    template: {
      containers: [
        {
          name: frontendWebAppServiceName
          image: frontendWebAppServiceImage
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ApplicationInsights__InstrumentationKey'
              secretRef: 'appinsights-key'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// ------------------
// OUTPUTS
// ------------------

@description('The name of the container app for the frontend web app service.')
output frontendWebAppServiceContainerAppName string = frontendWebAppService.name

@description('The FQDN of the frontend web app service.')
output frontendWebAppServiceFQDN string = frontendWebAppService.properties.configuration.ingress.fqdn
