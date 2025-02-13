---
canonical_url: 'https://azure.github.io/aca-dotnet-workshop'
---

# Module 11 - Integration with Azure Container Apps landing zone accelerator

Azure landing zone accelerators provide architectural guidance, reference architectures, reference implementations, and automation to deploy workload platforms on Azure at scale. They are aligned with industry proven practices, such as those presented in Azure landing zones guidance in the Cloud Adoption Framework.

This Azure Container Apps landing zone accelerator represents the strategic design path and target technical state for an Azure Container Apps deployment, owned and operated by a workload team.

The application created as part of the workshop is integrated with the Azure Container Apps landing zone accelerator.

## Deploy the landing zone

To deploy the landing zone, you can follow the complete guide in [Azure Container Apps - Internal environment secure baseline [Bicep]](https://github.com/Azure/aca-landing-zone-accelerator/blob/main/scenarios/aca-internal/bicep/README.md){target=_blank}.

The deployment of the sample app deploys also an application gateway with the same name as the one of the landing zone.
It is recommended to deploy only the first four building blocks of the landing zone and then deploy this sample app, i.e. do not deploy hello world sample app and application gateway. To do so, you can set the attribute `deployHelloWorldSampleApp` to `false` in the parameters file of the landing zone.

To have Dapr observability in Application Insights, you need to set the attributes `enableApplicationInsights` and `enableDaprInstrumentation` to `true` in the parameters file of the landing zone. To know more about monitoring and observability, you can follow this documentation [Operations management considerations for Azure Container Apps](https://github.com/Azure/aca-landing-zone-accelerator/blob/main/docs/design-areas/management.md){target=_blank}.

Please see the [Azure Container Apps Landing Zone Accelerator Task Tracker Service](https://github.com/Azure/aca-landing-zone-accelerator/blob/main/scenarios/aca-internal/bicep/sample-apps/dotnet-task-tracker-service/docs/02-container-apps.md) for details as to how to deploy the container apps.
