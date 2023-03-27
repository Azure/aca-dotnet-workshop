---
canonical_url: https://bitoftech.net/2022/09/05/azure-container-apps-with-dapr-bindings-building-block/
---

# Module 7 - ACA Scheduled Jobs with Dapr Cron Binding
In the preceding module, we discussed how Dapr bindings can simplify the integration process with external systems by facilitating the handling of events and the invocation of external resources. 
In this module we will focus on a special type of Dapr input binding named [Cron Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/).

The Cron binding doesn't subscribe for events coming from an external system. Instead, this binding can be used to trigger application code in our service periodically based on a configurable interval. 
The binding provides a simple way to implement a background worker to wake up and do some work at a regular interval, without the need to implement an endless loop with a configurable delay.

We intend to utilize this binding for a specific use case, wherein it will be triggered once daily at a particular time (12:05 am), and search for tasks that have a due date matching the previous day of its 
execution and are still pending. Once the service identifies tasks that meet these criteria, it will designate them as overdue tasks and save the revised status on Azure Cosmos DB.

### Updating the Backend Background Processor Project

#### 1. Add Cron Binding Configuration

To set up the Cron binding, the initial step involves adding a component file that specifies the location of the code that requires triggering and the intervals at which it should occur. 
To accomplish this, create a new file called dapr-scheduled-cron.yaml within the components folder and insert the following code:

Add new file under **components** as shown below:

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
    visit the [Cron binding specs.](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/#schedule-format).

#### 2. Add the Endpoint Which Will be Invoked by Cron Binding

Let's add an endpoint which will be triggered when the Cron configuration is met. This endpoint will contain the routine needed to run at a regular interval. 

Add new file under **controllers** folder in the project **TasksTracker.Processor.Backend.Svc** as shown below:

=== "ScheduledTasksManagerController.cs"
    
    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/ScheduledTasksManagerController.cs"
    ```
Here, we have added a new action method called `CheckOverDueTasksJob`, which includes the relevant business logic that will be executed by the Cron job configuration at specified intervals. 
This action method must be of the `POST` type, allowing it to be invoked when the job is triggered in accordance with the Cron interval.

#### 3. Update the Backend Web API Project

Now we need to add two new methods which are used by the scheduled job. 

Update below **files** under **services** folder in the project **TasksTracker.TasksManager.Backend.Api** as highlighted below:

=== "ITasksManager.cs"

    ```csharp hl_lines="3 4"
    public interface ITasksManager
    {
        Task MarkOverdueTasks(List<TaskModel> overdueTasksList);
        Task<List<TaskModel>> GetYesterdaysDueTasks();
    }
    ```
=== "TasksStoreManager.cs"

    ```csharp hl_lines="1-3 6-37 39-47"
    using System.Text.Json;
    using System.Text.Encodings.Web;
    using System.Text.Json.Serialization;

    
    public async Task<List<TaskModel>> GetYesterdaysDueTasks()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Converters =
            {
                new JsonStringEnumConverter(),
                new DateTimeConverter("yyyy-MM-ddTHH:mm:ss")
            },
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        var yesterday = DateTime.Today.AddDays(-1);
    
        var jsonDate = JsonSerializer.Serialize(yesterday, options);
        
        
        
        _logger.LogInformation("Getting overdue tasks for yesterday date: '{0}'", jsonDate);
        
        var query = "{" +
                "\"filter\": {" +
                    "\"EQ\": { \"taskDueDate\": " + jsonDate + " }" +
                "}}";
    
        var queryResponse = await _daprClient.QueryStateAsync<TaskModel>(STORE_NAME, query);
        var tasksList = queryResponse.Results.Select(q => q.Data).Where(q=>q.IsCompleted==false && q.IsOverDue==false).OrderBy(o=>o.TaskCreatedOn);
        return tasksList.ToList();
    }
    
    public async Task MarkOverdueTasks(List<TaskModel> overDueTasksList)
    {
        foreach (var taskModel in overDueTasksList)
        {
            _logger.LogInformation("Mark task with Id: '{0}' as OverDue task", taskModel.TaskId);
            taskModel.IsOverDue = true;
            await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);
        }
    }
    ```

Add below **file** under **Utilities** folder in the project **TasksTracker.TasksManager.Backend.Api** as shown below:

=== "DateTimeConverter.cs"

    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/DateTimeConverter.cs"
    ```

??? tip "Curious to learn more about above code?"
    
    What we've implemented here is the following:

    - Method `GetYesterdaysDueTasks` will query the Cosmos DB state store using Dapr State API to lookup all the yesterday's task which are not completed yet. Remember that Cron job is configured to run each day 
    at 12:05am so we are interested to check only the day before when the service runs. We initially made this implementation simple. There might be some edge cases not handled with the current implementation.
    - Method `MarkOverdueTasks` will take list of all tasks which passed the due date and set the flag `IsOverDue` to `true`.

Do not forget to add fake implementation for class `FakeTasksManager.cs` so the project **TasksTracker.TasksManager.Backend.Api** builds successfully.

=== "FakeTasksManager.cs"
    
    ```csharp hl_lines="1-4 6-11"
    public Task MarkOverdueTasks(List<TaskModel> overDueTasksList)
    {
        throw new NotImplementedException();
    }
    
    public Task<List<TaskModel>> GetYesterdaysDueTasks()
    {
        var tasksList = _tasksList.Where(t => t.TaskDueDate.Equals(DateTime.Today.AddDays(-1))).ToList();
    
        return Task.FromResult(tasksList);
    }     
    ```

#### 4. Add Action Methods to Backend Web API project

As you've seen in the [previous step](#2-add-the-endpoint-which-will-be-invoked-by-cron-binding), we are using Dapr Service to Service invocation API to call
methods `api/overduetasks` and `api/overduetasks/markoverdue` in the Backend Web API from the Backend Background Processor.

Add below **file** under **controllers** folder in the project **TasksTracker.TasksManager.Backend.Api** as shown below:

=== "OverdueTasksController.cs"
    
    ```csharp
    --8<-- "docs/aca/07-aca-cron-bindings/OverdueTasksController.cs"
    ```
#### 5. Add Cron Binding Configuration Matching ACA Specs

Add a new file folder **aca-components**. This file will be used when updating the Azure Container App Env and enable this binding.

=== "containerapps-scheduled-cron.yaml"

    ```yaml
    --8<-- "docs/aca/07-aca-cron-bindings/containerapps-scheduled-cron.yaml"
    ```
!!! note
    The name of the binding is not part of the file metadata. We are going to set the name of the binding to the value `ScheduledTasksManager` when we update the Azure Container Apps Env.

### Deploy the Backend Background Processor and the Backend API Projects to Azure Container Apps

#### 1. Build the Backend Background Processor and the Backend API App Images and Push them to ACR

To prepare for deployment to Azure Container Apps, we must build and deploy both application images to ACR, just as we did before. We can use the same PowerShell console use the 
following code (make sure you are on directory **TasksTracker.ContainerApps**):

```powershell
az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_API_NAME" --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' . 

az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_SVC_NAME" --file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .
```

#### 2. Add Cron Dapr Component to ACA Environment

```powershell
##Cron binding component
az containerapp env dapr-component set `
  --name $ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name scheduledtasksmanager `
  --yaml '.\aca-components\containerapps-scheduled-cron.yaml'
```

##### 3. Deploy New Revisions of the Backend API and Backend Background Processor to ACA
As we did before, we need to update the Azure Container App hosting the Backend API & Backend Background Processor with a new revision so our code changes are available for the end users. 
To accomplish this run the PowerShell script below:

```powershell
## Update Backend API App container app and create a new revision 
az containerapp update `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v20230227-1 

## Update Backend Background Processor container app and create a new revision 
az containerapp update `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v20230227-1 
```

!!! note
    The service `ScheduledTasksManager` which will be triggered by the Cron job on certain intervals is hosted in the ACA service `ACA-Processor Backend`. In the future module we are going to scale this 
    ACA `ACA-Processor Backend` to multiple replicas/instances. 

    It is highly recommended that background periodic jobs are hosted in a container app with **one single replica**, you don't want your background periodic job to run on multiple replicas trying to do the same thing.

!!! success
    With those changes in place and deployed, from the Azure Portal, you can open the log streams of the container app hosting the `ACA-Processor-Backend` and check the logs generated when the Cron job is triggered,
    you should see logs similar to the below image
    
    ![app-logs](../../assets/images/07-aca-cron-bindings/cron-logs.jpg)
    
    !!! note
        Keep in mind though that you won't be able to see the results instantaneously as the cron job searches for tasks that have a due date matching the previous day of its execution and are still pending.