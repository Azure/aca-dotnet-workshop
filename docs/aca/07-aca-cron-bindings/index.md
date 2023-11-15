---
canonical_url: https://bitoftech.net/2022/09/05/azure-container-apps-with-dapr-bindings-building-block/
---

# Module 7 - ACA Scheduled Jobs with Dapr Cron Binding

!!! info "Module Duration"
    60 minutes

??? tip "Curious about Azure Container Apps jobs?"

    There is a new kid on the block. [Azure Container Apps jobs](https://learn.microsoft.com/en-us/azure/container-apps/jobs){target=_blank} became generally available in late August 2023. This workshop is not yet updated to account for this new type of container app. Stay tuned for updates!

## Objective

In this module, we will accomplish three objectives:

1. Learn how the Cron binding can trigger actions.
1. Add a Cron binding to the Backend Background Processor.
1. Deploy updated Background Processor and API projects to Azure.

## Module Sections

--8<-- "snippets/restore-variables.md"

### 1. The Cron Binding

In the preceding module, we discussed how Dapr bindings can simplify the integration process with external systems by facilitating the handling of events and the invocation of external resources.  

In this module we will focus on a special type of Dapr input binding named [Cron Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/){target=_blank}.

The Cron binding doesn't subscribe to events coming from an external system. Instead, this binding can be used to trigger application code in our service periodically based on a configurable interval. The binding provides a simple way to implement a background worker to wake up and do some work at a regular interval, without the need to implement an endless loop with a configurable delay. We intend to utilize this binding for a specific use case, wherein it will be triggered once daily at a particular time (12:05 am), and search for tasks that have a due date matching the previous day of its execution and are still pending. Once the service identifies tasks that meet these criteria, it will designate them as overdue tasks and save the revised status on Azure Cosmos DB.

Contrasting the binding to Azure Container Apps jobs, we do not need a separate container app and can integrate this binding into our existing backend service.

### 2. Updating the Backend Background Processor Project

#### 2.1 Add Cron Binding Configuration

To set up the Cron binding, we add a component file that specifies the code that requires triggering and the intervals at which it should occur.  

To accomplish this, create a new file called **dapr-scheduled-cron.yaml** within the **components** folder and insert the following code:

=== "dapr-scheduled-cron.yaml"

    ```yaml
    --8<-- "docs/aca/07-aca-cron-bindings/dapr-scheduled-cron.yaml"
    ```

??? tip "Curious to learn more about above yaml file configuration?"

    The actions performed above are as follows:

    * Added a new input binding of type `bindings.cron`.
    * Provided the name `ScheduledTasksManager` for this binding. This means that an HTTP POST endpoint on the URL `/ScheduledTasksManager` should be added as it will be invoked when the job is triggered based on 
    the Cron interval.
    * Setting the interval for this Cron job to be triggered once a day at 12:05am. For full details and available options on how to set this value, 
    visit the [Cron binding specs.](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/#schedule-format){target=_blank}.

#### 2.2 Add the Endpoint Which Will be Invoked by Cron Binding

Let's add an endpoint which will be triggered when the Cron configuration is met. This endpoint will contain the routine needed to run at a regular interval.

Add a new file under the **Controllers** folder in the project **TasksTracker.Processor.Backend.Svc** as shown below:

=== "ScheduledTasksManagerController.cs"

    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/ScheduledTasksManagerController.cs"
    ```
Here, we have added a new action method called `CheckOverDueTasksJob`, which includes the relevant business logic that will be executed by the Cron job configuration at specified intervals.
This action method must be of the `POST` type, allowing it to be invoked when the job is triggered in accordance with the Cron interval.

#### 2.3 Update the Backend Web API Project

Now we need to add two new methods which are used by the scheduled job.

Update these files under the **Services** folder in the project **TasksTracker.TasksManager.Backend.Api** as highlighted below:

=== "ITasksManager.cs"

    ```csharp hl_lines="13-14"
    --8<-- "docs/aca/07-aca-cron-bindings/ITasksManager.cs"
    ```

=== "TasksStoreManager.cs"

    ```csharp hl_lines="2-4 95-134"
    --8<-- "docs/aca/07-aca-cron-bindings/TasksStoreManager.cs"
    ```

Add a new file in a new **Utilities** folder in the project **TasksTracker.TasksManager.Backend.Api** as shown below:

=== "DateTimeConverter.cs"

    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/DateTimeConverter.cs"
    ```

??? tip "Curious to learn more about above code?"

    What we've implemented here is the following:

    - Method `GetYesterdaysDueTasks` will query the Cosmos DB state store using Dapr State API to lookup all the yesterday's task which are not completed yet. Remember that Cron job is configured to run each day 
    at 12:05am so we are interested to check only the day before when the service runs. We initially made this implementation simple. There might be some edge cases not handled with the current implementation.
    - Method `MarkOverdueTasks` will take list of all tasks which passed the due date and set the flag `IsOverDue` to `true`.

Add the new methods to the fake implementation for class `FakeTasksManager.cs` so the project **TasksTracker.TasksManager.Backend.Api** builds successfully.

=== "FakeTasksManager.cs"

    ```csharp hl_lines="102-112"
    --8<-- "docs/aca/07-aca-cron-bindings/FakeTasksManager.cs"
    ```

#### 2.4 Add Action Methods to Backend Web API project

As you've seen previously, we are using a Dapr Service-to-Service invocation API to call methods `api/overduetasks` and `api/overduetasks/markoverdue` in the Backend Web API from the Backend Background Processor.

Add a new file under the **Controllers** folder in the project **TasksTracker.TasksManager.Backend.Api** as shown below:

=== "OverdueTasksController.cs"

    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/OverdueTasksController.cs"
    ```

#### 2.5 Add Cron Binding Configuration Matching ACA Specs

Add a new file folder **aca-components**. This file will be used when updating the Azure Container App Env and enable this binding.

=== "containerapps-scheduled-cron.yaml"

    ```yaml
    --8<-- "docs/aca/07-aca-cron-bindings/containerapps-scheduled-cron.yaml"
    ```

!!! note
    The name of the binding is not part of the file metadata. We are going to set the name of the binding to the value `ScheduledTasksManager` when we update the Azure Container Apps Env.

### 3. Deploy the Backend Background Processor and the Backend API Projects to Azure Container Apps

#### 3.1 Build the Backend Background Processor and the Backend API App Images and Push them to ACR

To prepare for deployment to Azure Container Apps, we must build and deploy both application images to ACR, just as we did before. We can use the same PowerShell console use the following code (make sure you are on directory **TasksTracker.ContainerApps**):

```shell
az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$BACKEND_API_NAME" `
--file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' . 

az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$BACKEND_SERVICE_NAME" `
--file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .
```

#### 3.2 Add the Cron Dapr Component to ACA Environment

```shell
# Cron binding component
az containerapp env dapr-component set `
--name $ENVIRONMENT --resource-group $RESOURCE_GROUP `
--dapr-component-name scheduledtasksmanager `
--yaml '.\aca-components\containerapps-scheduled-cron.yaml'
```

#### 3.3 Deploy New Revisions of the Backend API and Backend Background Processor to ACA

As we did before, we need to update the Azure Container App hosting the Backend API & Backend Background Processor with a new revision so our code changes are available for the end users.
To accomplish this run the PowerShell script below:

```shell
# Update Backend API App container app and create a new revision 
az containerapp update `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY-4

# Update Backend Background Processor container app and create a new revision 
az containerapp update `
--name $BACKEND_SERVICE_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY-4
```

!!! note
    The service `ScheduledTasksManager` which will be triggered by the Cron job on certain intervals is hosted in the ACA service `ACA-Processor Backend`. In the future module we are going to scale this ACA `ACA-Processor Backend` to multiple replicas/instances.

    It is highly recommended that background periodic jobs are hosted in a container app with **one single replica**, you don't want your background periodic job to run on multiple replicas trying to do the same thing. This, in fact, would be a limitation that could call for a separate Azure Container App jobs implementation as we typically want or app/API/service to scale.

!!! success
    With those changes in place and deployed, from the [Azure portal](https://portal.azure.com){target=_blank}, you can open the log streams of the container app hosting the `ACA-Processor-Backend` and check the logs generated when the Cron job is triggered,
    you should see logs similar to the below image

    ![app-logs](../../assets/images/07-aca-cron-bindings/cron-logs.jpg)
    
    !!! note
        Keep in mind though that you won't be able to see the results instantaneously as the cron job searches for tasks that have a due date matching the previous day of its execution and are still pending.

--8<-- "snippets/persist-state.md:module7"

## Review

In this module, we have accomplished three objectives:

1. Learned how the Cron binding can trigger actions.
1. Added a Cron binding to the Backend Background Processor.
1. Deployed updated Background Processor and API projects to Azure.