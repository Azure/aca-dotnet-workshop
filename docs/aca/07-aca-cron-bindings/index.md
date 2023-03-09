---
title: Module 7 - ACA scheduled jobs with Dapr Cron Binding
has_children: false
nav_order: 7
canonical_url: 'https://bitoftech.net/2022/09/05/azure-container-apps-with-dapr-bindings-building-block/'
---
# Module 7 - ACA scheduled jobs with Dapr Cron Binding
In the previous module we have covered how Dapr bindings help us to simplify integrating with external systems when an event occurs, and how to trigger an event that invokes an external resource. In this module we will focus on a special type of Dapr input binding named [Cron Binding.](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/).
The Cron binding doesn't subscribe for events coming from an external system, Instead, this binding can be used to trigger application code in our service periodically based on a configurable interval. The binding provides a simple way to implement a background worker to wake up and do some work at a regular interval, without the need to implement an endless loop with a configurable delay.

The use case we want to cover with this binding that it will trigger one time daily at certain time (i.e. 12:05 am) and lookup for tasks which have due date matching the day before it run and are not completed yet, if the service was able to find some tasks with this criteria; it will mark them as overdue tasks and store the updated state on Azure Cosmos DB. 

### Updating the Backend Background Processor Project

##### 1. Add Cron binding configuration
The first step to configuring Cron binding is to add a component file that describes where is the code that needs to be triggered and on which intervals it should be triggered, to do so add a new file named `dapr-scheduled-cron.yaml` under folder `components` and use the code below:
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

What we have done here is the following:

* Added new input binding of type `bindings.cron`.
* Provided the name `ScheduledTasksManager` for this binding, this means that an HTTP POST endpoint on the URL `/ScheduledTasksManager` should be added as it will be invoked when the job is triggered based on the Cron interval.
* Setting the interval for this Cron job to be triggered one time each day at 12:05am, for full details and available options on how to set this value, visit the [Cron binding specs.](https://docs.dapr.io/reference/components-reference/supported-bindings/cron/#schedule-format).

##### 2. Add the endpoint which will be invoked by Cron binding
Let's add an endpoint which will be triggered when the Cron configuration is met, this endpoint will contain the routine needed to run at a regular interval, to do so add a new controller named `ScheduledTasksManagerController.cs` under `controllers` folder in the project `TasksTracker.Processor.Backend.Svc` and use the code below:

```csharp
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
What we have done here that we've added new action method named `CheckOverDueTasksJob` contains the business logic which will be triggered by the Cron job configuration on a certain interval. This action method should be of type `POST` so it will be invoked when the job is triggered based on the Cron interval.

##### 3. Update the Backend Web API Project
Now we need to add 2 new methods which are used by the scheduled job, open interface named `ITasksManager.cs` in the project `TasksTracker.TasksManager.Backend.Api` and add the below 2 methods:

```csharp
public interface ITasksManager
{
    Task MarkOverdueTasks(List<TaskModel> overdueTasksList);
    Task<List<TaskModel>> GetYesterdaysDueTasks();
}
```
Next we need to provide implementation for those 2 methods, so open file `TasksStoreManager.cs` in the same project and use the code below:

```csharp
public class TasksStoreManager : ITasksManager
{
    public async Task<List<TaskModel>> GetYesterdaysDueTasks()
    {
        var settings=new Newtonsoft.Json.JsonSerializerSettings { DateFormatString ="yyyy-MM-ddTHH:mm:ss"};
        var yesterday = DateTime.Today.AddDays(-1);
        var jsonDate= Newtonsoft.Json.JsonConvert.SerializeObject(yesterday,settings);
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
}
```

What we've implemented here is the following:
- Method `GetYesterdaysDueTasks` will query the Cosmos DB state store using Dapr State API to lookup all the yesterday's task which are not completed yet, remember that Cron job is configured to run each day at 12:05am so we are interested to check only to check the day before when the service rus. We initially made this implementation simple. There might be some edge cases not handled with the current implementation.
- Method `MarkOverdueTasks` will take list of all tasks which passed the due date and set the flag `IsOverDue` to `true`.

Do not forget to add fake implementation for class `FakeTasksManager` so the project `TasksTracker.TasksManager.Backend.Api` builds successfully. 

##### 4. Add actions methods to Backend Web API project

As you've seen in the [previous step](#2-add-the-endpoint-which-will-be-invoked-by-cron-binding), we are using Dapr Service to Service invocation API to call methods `api/overduetasks` and `api/overduetasks/markoverdue` in the Backend Web API from the Backend Background Processor, to do so add a new file named `OverdueTasksController` in folder `controllers` under project `TasksTracker.TasksManager.Backend.Api`, use the code below:
```csharp
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
##### 5. Add Cron binding configuration matching ACA Specs

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
Note that the name of the binding is not part of the file metadata, we are going to set the name of the binding to the value `ScheduledTasksManager` when we update the Azure Container Apps Env.

### Deploy the Backend Background Processor and the Backend API Projects to Azure Container Apps
##### 1. Build the Backend Background Processor and the Backend API App images and push them to ACR
As we have done previously we need to build and deploy both app images to ACR so they are ready to be deployed to Azure Container Apps, to do so, continue using the same PowerShell console and paste the code below (Make sure you are on directory `TasksTracker.ContainerApps`):

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

##### 3. Deploy new revisions of the Backend API and Backend Background Processor to ACA
As we've done multiple times, we need to update the Azure Container App hosting the Backend API & Backend Background Processor with a new revision so our code changes are available for end users, to do so run the below PowerShell script

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
The service `ScheduledTasksManager` which will be triggered by the Cron job on certain intervals is hosted in the ACA service `ACA-Processor Backend`. In the future module we are going to scale this ACA `ACA-Processor Backend` to multiple replicas/instances. It is highly recommended that background periodic jobs to be hosted in a container app with **one single replica**, you don't want your background periodic job to run on multiple replicas trying to do the same thing.

With those changes in place and deployed, from the Azure Portal, you can open the log streams of the container app hosting the `ACA-Processor-Backend` and check the logs generated when the Cron job is triggered, you should see logs similar to the below image
![app-logs](../../assets/images/07-aca-cron-bindings/cron-logs.jpg)