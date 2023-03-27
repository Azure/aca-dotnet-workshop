---
canonical_url:https://bitoftech.net/2022/09/09/azure-container-apps-monitoring-and-observability-with-application-insights-part-8/
---

# Module 8 - ACA Monitoring and Observability with Application Insights

In this module, we will explore how we can configure ACA and ACA Environment with [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview) which will provide a holistic
view of our container apps health, performance metrics, logs data, various telemetries and traces.
ACA do not support [Auto-Instrumentation](https://learn.microsoft.com/en-us/azure/azure-monitor/app/codeless-overview#supported-environments-languages-and-resource-providers) for Application Insights, 
so in this module, we will be focusing on how we can integrate Application Insights into our microservice application.

### Application Insights Overview

Application Insights is an offering from Azure Monitor that will help us to monitor all ACAs under the same Container App Environment and collect telemetry about the services within the solution, as well as understand the usage of the services and users' engagement via integrated analytics tools.

The term "Telemetry" refers to the information gathered to monitor our application, which can be classified into three distinct groups.

1. Distributed Tracing: Distributed Tracing allows for visibility into the communication between services participating in distributed transactions. For instance, when the Frontend Web Application interacts with the Backend API Application to add or retrieve information. An application map of how calls are flowing between services is very important for any distributed application.
2. Metrics: This offers a view of a service's performance and its use of resources. For instance, it helps in monitoring the CPU and memory usage of the Backend Background Processor, and identifying when it is necessary to scale up the number of replicas.
3. Logging: provides insights into how code is executing and if errors have occurred.

In [module 1](../../aca/01-deploy-api-to-aca/index.md#2-deploy-web-api-backend-project-to-aca) we have already provisioned a Workspace-based Application Insights Instance and configured it with ACA environment by setting the property `--dapr-instrumentation-key` when creating the environment. We will presume that you have already set up an instance of Application Insights that is available for use across the three Container Apps.

### Installing Application Insights SDK Into the Three Microservices Apps

#### 1. Install the Application Insights SDK Using NuGet

Our next step is to incorporate the Application Insights SDK into the **three services**, which is a uniform procedure.

!!! note
    While we will outline the process of configuring Application Insights for the Backend API service, the identical steps must be followed for the other two services.

To incorporate the SDK, use the NuGet reference below in the `csproj` file of the Backend API project. You may locate the csproj file in the project directory **TasksTracker.TasksManager.Backend.Api**:

=== "TasksTracker.TasksManager.Backend.Api.csproj"
    
    ```xml hl_lines="3"
      <ItemGroup>
        <!--Other packages are removed for brevity-->
        <PackageReference Include="Microsoft.ApplicationInsights.AspNetCore" Version="2.21.0" />
      </ItemGroup>
    ```

#### 2. Set RoleName Property in All the Services

For each project, we will add a new file on the root directory of the project **TasksTracker.TasksManager.Backend.Api**.

=== "AppInsightsTelemetryInitializer.cs"

    ```csharp
    --8<-- "docs/aca/08-aca-monitoring/AppInsightsTelemetryInitializer.cs"
    ```

!!! important "RoleName property for three services"
    
    The only difference between each file on the 3 projects is the **RoleName** property value. 
    
    
    Application Insights will utilize this property to recognize the elements on the application map. Additionally, it will prove beneficial for us in case we want to filter through all the warning logs produced by the Backend API service. Therefore, we will apply the tasksmanager-backend-api value for filtering purposes.

    ??? example "RoleName property values for other two service"
        
        You can check the `AppInsightsTelemetryInitializer.cs` files and the RoleName value used in the other projects below: 
        
        === "TasksTracker.WebPortal.Frontend.Ui"
            
            ```csharp hl_lines="13"
            --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/a642c296d8f35b9c81a17e63c11797aee9066f5a/TasksTracker.WebPortal.Frontend.Ui/AppInsightsTelemetryInitializer.cs"
            ```

        === "TasksTracker.Processor.Backend.Svc"
            
            ```csharp hl_lines="13"
            --8<-- "https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/a642c296d8f35b9c81a17e63c11797aee9066f5a/TasksTracker.Processor.Backend.Svc/AppInsightsTelemetryInitializer.cs"
            ```

Next, we need to register this `AppInsightsTelemetryInitializer` class. Update the Program.cs file located under **TasksTracker.TasksManager.Backend.Api** as highlighted below:

!!! note
    Don't forget that you need to do the same for the remaining **two projects**.

=== "Program.cs"

    ```csharp hl_lines="1 2 6-9"
    using Microsoft.ApplicationInsights.Extensibility;
    using TasksTracker.TasksManager.Backend.Api;
    
    //Code removed for brevity 
    
    builder.Services.AddApplicationInsightsTelemetry();
    builder.Services.Configure<TelemetryConfiguration>((o) => {
        o.TelemetryInitializers.Add(new AppInsightsTelemetryInitializer());
    });
    var app = builder.Build();
    //Code removed for brevity
    ```

#### 3. Set the Application Insights Instrumentation Key

In the previous module, we've used Dapr Secret Store to store connection strings and keys. In this module we will demonstrate how we can use another approach to secrets in Container Apps.

We need to set the Application Insights Instrumentation Key so the projects are able to send telemetry data to the Application Insights instance. We are going to set this via secrets and environment variables once we redeploy the Container Apps and create new revisions.

=== "appsettings.json"

    ```json
    {
      "ApplicationInsights": {
        "InstrumentationKey": "<Application Insights Key here for local development purposes. Not required for ACA as it is being set via --set-env-vars as you will see later>"
      } 
    }
    ```
With this step completed, we have done all the changes needed. Let's now deploy the changes and create new ACA revisions.

### Deploy Services to ACA and Create New Revisions

#### 1. Add Application Insights Instrumentation key as a secret

Let's create a secret named `appinsights-key` on each Container App which contains the value of the Application Insights instrumentation key. 
Remember that we can obtain this value from Azure Portal by going to Application Insights instance we created in module 1, or we can get it from Azure CLI as we did in module 1. To create the secret use your existing PowerShell session and paste the code below:

```powershell
az containerapp secret set `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--secrets "appinsights-key=<Application Insights Key Here>"

az containerapp secret set `
--name $FRONTEND_WEBAPP_NAME `
--resource-group $RESOURCE_GROUP `
--secrets "appinsights-key=<Application Insights Key Here>"

az containerapp secret set `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--secrets "appinsights-key=<Application Insights Key Here>"
```

##### 2. Build New Images and Push Them to ACR

As we did before, we are required to build and push the images of the three applications to ACR. By doing so, they will be prepared to be deployed in ACA.

To accomplish this, continue using the same PowerShell console and paste the code below (make sure you are on the following directory **TasksTracker.ContainerApps**):

```powershell
## Build Backend API on ACR and Push to ACR
az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_API_NAME" --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' . 
## Build Backend Service on ACR and Push to ACR
az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_SVC_NAME" --file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .
## Build Frontend Web App on ACR and Push to ACR
az acr build --registry $ACR_NAME --image "tasksmanager/$FRONTEND_WEBAPP_NAME" --file 'TasksTracker.WebPortal.Frontend.Ui/Dockerfile' .
```

#### 3. Deploy New Revisions of the Services to ACA and Set a New Environment Variable

We need to update the ACA hosting the three services with a new revision so our code changes are available for end users. 
 
!!! tip
    Notice how we used the property `--set-env-vars` to set new environment variable named `ApplicationInsights__InstrumentationKey`. Its value is a secret reference obtained from the secret `appinsights-key` we added in [step 1](#1-add-application-insights-instrumentation-key-as-a-secret).

```powershell
## Update Backend API App container app and create a new revision 
az containerapp update `
--name $BACKEND_API_NAME  `
--resource-group $RESOURCE_GROUP `
--revision-suffix v20230301-1 `
--set-env-vars "ApplicationInsights__InstrumentationKey=secretref:appinsights-key"

## Update Frontend Web App container app and create a new revision 
az containerapp update `
--name $FRONTEND_WEBAPP_NAME  `
--resource-group $RESOURCE_GROUP `
--revision-suffix v20230301-1 `
--set-env-vars "ApplicationInsights__InstrumentationKey=secretref:appinsights-key"

## Update Backend Background Service container app and create a new revision 
az containerapp update `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v20230301-1 `
--set-env-vars "ApplicationInsights__InstrumentationKey=secretref:appinsights-key"
```
!!! success
    With those changes in place, you should start seeing telemetry coming to the Application Insights instance provisioned. Let's review Application Insights' key dashboards and panels in Azure Portal.

### Distributed Tracing Via Application Map

Application Map will help us spot any performance bottlenecks or failure hotspots across all our services of our distributed microservice application. 
Each node on the map represents an application component (service) or its dependencies and has a health KPI and alerts status.

![distributed-tracing-ai](../../assets/images/08-aca-monitoring/distributed-tracing-ai.jpg)

Looking at the image above, you will see for example how the Backend Api with could RoleName `tasksmanager-backend-api` is depending on the Cosmos DB instance, showing the number of calls and average time to service these calls. The application map is interactive so you can select a service/component and drill down into details.

For example, when we drill down into the Dapr State node to understand how many times the backend API invoked the Dapr Sidecar state service to Save/Delete state, you will see results similar to the  image below:

![distributed-tracing-ai-details](../../assets/images/08-aca-monitoring/distributed-tracing-ai-details.jpg)

!!! note
    It will take some time for the application map to fully populate.


### Monitor Production Application Using Live Metrics

This is one of the key monitoring panels. It provides you with near real-time (1-second latency) status of your entire distributed application. We have the ability to observe both the successes and failures of our system, monitor any occurring exceptions and trace them in real-time. Additionally, we can monitor the live servers (including replicas) and track their CPU and memory usage, as well as the number of requests they are currently handling.

These live metrics provide very powerful diagnostics for our production microservice application. Check the image below and see the server names and some of the incoming requests to the system.

![live-metrics-ai](../../assets/images/08-aca-monitoring/live-metrics-ai.jpg)

### Logs Search Using Transaction Search

Transaction search in Application Insights will help us find and explore individual telemetry items, such as exceptions, web requests, or dependencies as well as any log traces and events that we’ve added to the application.

For example, if we want to see all the event types of type `Request` for the cloud RoleName `tasksmanager-backend-api` in the past 24 hours, we can use the transaction search dashboard to do this. 
See how the filters are set and the results are displayed nicely. We can drill down on each result to have more details and what telemetry was captured before and after. A very useful feature when troubleshooting exceptions and reading logs.

![transaction-search-ai](../../assets/images/08-aca-monitoring/transaction-search-ai.jpg)

### Failures and Performance Panels

The failure panel enables us to assess the frequency of failures across various operations, which assists us in prioritizing our efforts towards the ones that have the most significant impact.

![failures-ai](../../assets/images/08-aca-monitoring/failures-ai.jpg)

The Performance panel displays performance details for the different operations in our system. By identifying those operations with the longest duration, we can diagnose potential problems or best target our ongoing development to improve the overall performance of the system.

![failures-ai](../../assets/images/08-aca-monitoring/performance-ai.jpg)