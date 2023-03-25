---
title: Module 7 - ACA scheduled jobs with Dapr Cron Binding
has_children: false
nav_order: 7
canonical_url: 'https://bitoftech.net/2022/09/05/azure-container-apps-with-dapr-bindings-building-block/'
---
# Module 7 - ACA Scheduled Jobs with Dapr Cron Binding
In the preceding module, we discussed how Dapr bindings can simplify the integration process with external systems by facilitating the handling of events and the invocation of external resources. In this module we will focus on a special type of Dapr input binding named [Cron Binding](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/).
The Cron binding doesn't subscribe for events coming from an external system. Instead, this binding can be used to trigger application code in our service periodically based on a configurable interval. The binding provides a simple way to implement a background worker to wake up and do some work at a regular interval, without the need to implement an endless loop with a configurable delay.

We intend to utilize this binding for a specific use case, wherein it will be triggered once daily at a particular time (12:05 am), and search for tasks that have a due date matching the previous day of its execution and are still pending. Once the service identifies tasks that meet these criteria, it will designate them as overdue tasks and save the revised status on Azure Cosmos DB.

### Updating the Backend Background Processor Project

##### 1. Add Cron Binding Configuration
To set up the Cron binding, the initial step involves adding a component file that specifies the location of the code that requires triggering and the intervals at which it should occur. 
To accomplish this, create a new file called dapr-scheduled-cron.yaml within the components folder and insert the following code:

To accomplish this add a new file named `dapr-scheduled-cron.yaml` under folder `components` and use the code below:
```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: ScheduledTasksManager
  namespace: default
spec:
  type: bindings.cron
  version: v1
  metadata:
  - name: schedule
    value: "5 0 * * *"
scopes:
- tasksmanager-backend-processor
```

The actions performed here are as follows:

* Added a new input binding of type `bindings.cron`.
* Provided the name `ScheduledTasksManager` for this binding. This means that an HTTP POST endpoint on the URL `/ScheduledTasksManager` should be added as it will be invoked when the job is triggered based on the Cron interval.
* Setting the interval for this Cron job to be triggered once a day at 12:05am. For full details and available options on how to set this value, visit the [Cron binding specs.](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/#schedule-format).

##### 2. Add the Endpoint Which Will be Invoked by Cron Binding
Let's add an endpoint which will be triggered when the Cron configuration is met. This endpoint will contain the routine needed to run at a regular interval. To accomplish this add a new controller named `ScheduledTasksManagerController.cs` under `controllers` folder in the project `TasksTracker.Processor.Backend.Svc` and use the code below:

```csharp
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using TasksTracker.Processor.Backend.Svc.Models;

namespace TasksTracker.Processor.Backend.Svc.Controllers
{
    [Route("ScheduledTasksManager")]
    [ApiController]
    public class ScheduledTasksManagerController : ControllerBase
    {
        private readonly ILogger<ScheduledTasksManagerController> _logger;
        private readonly DaprClient _daprClient;
        public ScheduledTasksManagerController(ILogger<ScheduledTasksManagerController> logger,
                                                DaprClient daprClient)
        {
            _logger = logger;
            _daprClient = daprClient;
        }

        [HttpPost]
        public async Task CheckOverDueTasksJob()
        {
            var runAt = DateTime.UtcNow;
            
            _logger.LogInformation($"ScheduledTasksManager::Timer Services triggered at: {runAt}");

            var overdueTasksList = new List<TaskModel>();
            
            var tasksList = await _daprClient.InvokeMethodAsync<List<TaskModel>>(HttpMethod.Get, "tasksmanager-backend-api", $"api/overduetasks");
            
            _logger.LogInformation($"ScheduledTasksManager::completed query state store for tasks, retrieved tasks count: {tasksList?.Count()}");
            
            tasksList?.ForEach(taskModel =>
            {
                if (runAt.Date> taskModel.TaskDueDate.Date)
                {
                    overdueTasksList.Add(taskModel);
                }
            });

            if (overdueTasksList.Count> 0)
            {
                _logger.LogInformation($"ScheduledTasksManager::marking {overdueTasksList.Count()} as overdue tasks");

                await _daprClient.InvokeMethodAsync(HttpMethod.Post, "tasksmanager-backend-api", $"api/overduetasks/markoverdue", overdueTasksList);
            }
        }
    }
}
```
Here, we have added a new action method called `CheckOverDueTasksJob`, which includes the relevant business logic that will be executed by the Cron job configuration at specified intervals. This action method must be of the `POST` type, allowing it to be invoked when the job is triggered in accordance with the Cron interval.

##### 3. Update the Backend Web API Project
Now we need to add two new methods which are used by the scheduled job. Open the interface named `ITasksManager.cs` which is located under the `services` folder of the `TasksTracker.TasksManager.Backend.Api` project. Incorporate the following two methods:

```csharp
public interface ITasksManager
{
    Task MarkOverdueTasks(List<TaskModel> overdueTasksList);
    Task<List<TaskModel>> GetYesterdaysDueTasks();
}
```
Next we need to provide the implementation for those two methods. Open the file `TasksStoreManager.cs` which is located under the `Services` folder of the `TasksTracker.TasksManager.Backend.Api` project and place the two following methods inside the existing class:

```csharp
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

Make sure you add the following using statements at the the top of the `TasksStoreManager.cs` which is located under the `Services` folder of the `TasksTracker.TasksManager.Backend.Api` project

```csharp
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
```

We will also need to add a utility method called `DateTimeConverter` under a file called `DateTimeConverter.cs` which itself should live under a folder called `Utilities`which in turn should be created under `TasksTracker.TasksManager.Backend.Api` project. Add the following code under `DateTimeConverter.cs`:


```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TasksTracker.TasksManager.Backend.Api.Services
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string _dateFormatString;

        public DateTimeConverter(string dateFormatString)
        {
            _dateFormatString = dateFormatString;
        }

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.ParseExact(reader.GetString(), _dateFormatString, System.Globalization.CultureInfo.InvariantCulture);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString(_dateFormatString));
        }
    }
}
```
What we've implemented here is the following:
- Method `GetYesterdaysDueTasks` will query the Cosmos DB state store using Dapr State API to lookup all the yesterday's task which are not completed yet. Remember that Cron job is configured to run each day at 12:05am so we are interested to check only the day before when the service runs. We initially made this implementation simple. There might be some edge cases not handled with the current implementation.
- Method `MarkOverdueTasks` will take list of all tasks which passed the due date and set the flag `IsOverDue` to `true`.

Do not forget to add fake implementation for class `FakeTasksManager` so the project `TasksTracker.TasksManager.Backend.Api` builds successfully. Add the following action methods at the end of the `FakeTasksManager` class:

```csharp
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

##### 4. Add Action Methods to Backend Web API project

As you've seen in the [previous step](#2-add-the-endpoint-which-will-be-invoked-by-cron-binding), we are using Dapr Service to Service invocation API to call methods `api/overduetasks` and `api/overduetasks/markoverdue` in the Backend Web API from the Backend Background Processor. To accomplish this add a new file named `OverdueTasksController.cs` in folder `controllers` under project `TasksTracker.TasksManager.Backend.Api` and use the code below:
```csharp
using Microsoft.AspNetCore.Mvc;
using TasksTracker.TasksManager.Backend.Api.Models;
using TasksTracker.TasksManager.Backend.Api.Services;

namespace TasksTracker.TasksManager.Backend.Api.Controllers
{
    [Route("api/overduetasks")]
    [ApiController]
    public class OverdueTasksController : ControllerBase
    {
        private readonly ILogger<TasksController> _logger;
        private readonly ITasksManager _tasksManager;

        public OverdueTasksController(ILogger<TasksController> logger, ITasksManager tasksManager)
        {
            _logger = logger;
            _tasksManager = tasksManager;
        }

        [HttpGet]
        public async Task<IEnumerable<TaskModel>> Get()
        {
            return await _tasksManager.GetYesterdaysDueTasks();
        }

        [HttpPost("markoverdue")]
        public async Task<IActionResult> Post([FromBody] List<TaskModel> overdueTasksList)
        {
            await _tasksManager.MarkOverdueTasks(overdueTasksList);

            return Ok();
        }
    }
}
```
##### 5. Add Cron Binding Configuration Matching ACA Specs

Now we will add a new file named `containerapps-scheduled-cron.yaml` under folder `aca-components`. this file will be used when updating the Azure Container App Env and enable this binding, and use the code below:
```yaml
componentType: bindings.cron
version: v1
metadata:
- name: schedule
  value: "5 0 * * *" # Everyday at 12:05am
scopes:
- tasksmanager-backend-processor
```
Note that the name of the binding is not part of the file metadata. We are going to set the name of the binding to the value `ScheduledTasksManager` when we update the Azure Container Apps Env.

### Deploy the Backend Background Processor and the Backend API Projects to Azure Container Apps
##### 1. Build the Backend Background Processor and the Backend API App Images and Push them to ACR
To prepare for deployment to Azure Container Apps, we must build and deploy both application images to ACR, just as we did before. To do this, we can use the same PowerShell console and copy and paste the following code (make sure you are on directory `TasksTracker.ContainerApps`):

```powershell
az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_API_NAME" --file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' . 

az acr build --registry $ACR_NAME --image "tasksmanager/$BACKEND_SVC_NAME" --file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .
```

##### 2. Add Cron Dapr Component to ACA Environment

```powershell
##Cron binding component
az containerapp env dapr-component set `
  --name $ENVIRONMENT --resource-group $RESOURCE_GROUP `
  --dapr-component-name scheduledtasksmanager `
  --yaml '.\aca-components\containerapps-scheduled-cron.yaml'
```

##### 3. Deploy New Revisions of the Backend API and Backend Background Processor to ACA
As we did before, we need to update the Azure Container App hosting the Backend API & Backend Background Processor with a new revision so our code changes are available for the end users. To accomplish this run the PowerShell script below:

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

{: .note }
The service `ScheduledTasksManager` which will be triggered by the Cron job on certain intervals is hosted in the ACA service `ACA-Processor Backend`. In the future module we are going to scale this ACA `ACA-Processor Backend` to multiple replicas/instances. It is highly recommended that background periodic jobs are hosted in a container app with **one single replica**, you don't want your background periodic job to run on multiple replicas trying to do the same thing.

With those changes in place and deployed, from the Azure Portal, you can open the log streams of the container app hosting the `ACA-Processor-Backend` and check the logs generated when the Cron job is triggered, you should see logs similar to the below image
![app-logs](../../assets/images/07-aca-cron-bindings/cron-logs.jpg). Keep in mind though that you won't be able to see the results instantaneously as the cron job searches for tasks that have a due date matching the previous day of its execution and are still pending.