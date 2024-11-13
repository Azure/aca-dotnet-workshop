![Azure Container Apps](docs/assets/images/00-workshop-intro/azure-container-apps-image.png)

There is no doubt that building containerized applications and following a microservices architecture is one of the most common software architecture patterns observed in the past couple of years.

Microsoft Azure offers different services to package, deploy, and manage cloud-native applications, each of which serves a certain purpose and has its own pros and cons. This [page](https://learn.microsoft.com/azure/container-apps/compare-options) provides a good comparison between the available services to host and manage cloud-native containerized applications in Azure.

Whereas building cloud-native apps on Azure Kubernetes Service (AKS) is powerful,  there is a bit of a learning curve needed when it comes to creating and configuring the cluster, configuring networking between microservices, services discovery, certificates provisioning, and, lastly, managing the cluster over the lifetime of the application.

In this workshop, we will be focusing on a new containerization service offered by Microsoft called Azure Container Apps (ACA). Microsoft announced the public preview of Azure Container Apps in November 2021, and in May 2022 it announced the General Availability of Azure Container Apps. In brief, Azure Container Apps is a fully managed, serverless, Kubernetes-based container runtime for building and running cloud-native applications which focuses on the business logic of the apps rather than on cloud infrastructure management.

##### Delivery Instructions

This workshop is intended to be completed by going through the different modules which are hosted under our Github page located [here](https://azure.github.io/aca-dotnet-workshop/).

We also include an optional [slides](https://github.com/Azure/aca-dotnet-workshop/tree/main/slides) folder which includes supporting material for those delivering the content. You can think about the slides as supporting material for an instructor but are not required for the attendees to complete the workshop.

##### Contributions

We are most grateful for community involvement. Please see [CONTRIBUTING.md](https://github.com/Azure/aca-dotnet-workshop/blob/main/CONTRIBUTING.md) for details. Thank you!

##### Acknowledgment

The workshop's material, concepts, and code samples draw inspiration from a collection of blog articles authored by [Taiseer Joudeh](https://github.com/tjoudeh) and published on his [personal blog](https://bitoftech.net). The [workshop authors](https://azure.github.io/aca-dotnet-workshop/aca/29-about-the-authors/) have worked collaboratively to modify and augment the content, resulting in the current version of the workshop.
