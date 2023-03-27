---
canonical_url: https://bitoftech.net/2022/08/25/tutorial-building-microservice-applications-azure-container-apps-dapr/
---

## Workshop Scenario

In this workshop we will build a tasks management application following the microservices architecture pattern. This application will consist of 3 microservices where each microservice has certain capabilities to demonstrate how ACA and Dapr can simplify the building of a microservices application. Below is the architecture diagram of the application we are going to build in this workshop.

## Solution Architecture 

![Solution Architecture](../../assets/images/00-workshop-intro/ACA-Architecture-workshop.jpg)

1. **ACA Web App-Frontend** is a simple ASP.NET Razor pages web app that accepts requests from public users to manage their tasks. It invokes the component "ACA WebAPI-Backend" endpoints via HTTP or gRPC.
2. **ACA WebAPI-Backend** is a backend Web API which contains the business logic of tasks management service, data storage, and publishing messages to Azure Service Bus Topic.
3. **ACA Processor-Backend** is an event-driven backend processor which is responsible for sending emails to task owners based on messages coming from Azure Service Bus Topic. Here there is a continuously running background processor, which is based on Dapr Cron timer configuration, to flag overdue tasks.
4. Autoscaling rules using KEDA are configured in the "ACA Processor-Backend" service to scale out/in replicas based on the the number of messages in the Azure Service Bus Topic. 
5. Azure Container Registry is used to build and host container images and deploy images from ACR to Azure Container Apps.
6. Application Insights and Azure Log Analytics are used for Monitoring, Observability, and distributed tracings of ACA.