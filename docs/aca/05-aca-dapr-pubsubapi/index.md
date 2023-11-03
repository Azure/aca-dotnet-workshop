---
canonical_url: https://bitoftech.net/2022/09/02/azure-container-apps-async-communication-with-dapr-pub-sub-api-part-6/
---

# Module 5 - ACA Async Communication with Dapr Pub/Sub API

!!! info "Module Duration"
    90 minutes

In this module, we will introduce a new background service which is named `ACA-Processor Backend` according to our [architecture diagram](../../assets/images/00-workshop-intro/ACA-Architecture-workshop.jpg). This new service will be responsible for sending notification emails (simulated) to the task owners to notify them that a new task has been assigned to them. We can do this in the Backend API and send the email right after saving the task, but we want to offload this process to another service and keep the Backend API service responsible for managing tasks data only.

As a best practice, it is recommended that we decouple the two services from each other. So this means we are going to rely on the Publisher-Subscriber pattern (Pub/Sub Pattern).
The main advantage of this pattern is that it offers loose coupling between services, where the sender/publisher of the message doesn't know anything about the receiver/consumers.
You can even have multiple consumers consuming a copy of the message in a totally different way. You can imagine adding another consumer which is responsible to send push notification for the task owner (e.g. if we had a mobile app channel).

The publisher/subscriber pattern relies on a message broker which is responsible for receiving the message from the publisher, storing the message to ensure durability, and delivering this message to the interested
consumer(s) to process it. There is no need for the consumers to be available when the message is stored in the message broker. Consumers can process the messages at a later time in an async fashion.
The below diagram gives a high-level overview of how the pub/sub pattern works:

![pubsub-arch](../../assets/images/05-aca-dapr-pubsubapi/tutorial-pubsub-arch.jpg)

If you implemented the Pub/Sub pattern before, you already know that there is a lot of plumbing needed on the publisher and subscriber components in order to publish and consume messages.
In addition, each message broker has its own SDK and implementation. So you need to write your code in an abstracted way to hide the specific implementation details for each message broker SDK and make it easier for the publisher and consumers to re-use this functionality. What Dapr offers here is a building block that significantly simplifies implementing pub/sub functionality.

Put simply, the Dapr pub/sub building block provides a platform-agnostic API framework to send and receive messages. Your producer/publisher services publish messages to a named topic. Your consumer services subscribe to a topic to consume messages.

To try this out we can directly invoke the Pub/Sub API and publish a message to Redis locally. If you remember from [module 3](../../aca/03-aca-dapr-integration/index.md) once we initialized Dapr in a local development environment, it installed Redis container instance locally. So we can use Redis locally to publish and subscribe to a message.
If you navigate to the path `%USERPROFILE%\.dapr\components (assuming you are using windows)` you will find a file named `pubsub.yaml`. Inside this file, you will see the properties needed to access the local Redis instance.
The publisher/subscriber brokers template component file structure can be found [here](https://docs.dapr.io/operations/components/setup-pubsub/){target=_blank}.

--8<-- "snippets/restore-variables.md"

We want to have more control and provide our own component file, so let's create pub/sub component file in our **components** folder as shown below:

=== "dapr-pubsub-redis.yaml"

    ```yaml
    --8<-- "docs/aca/05-aca-dapr-pubsubapi/dapr-pubsub-redis.yaml"
    ```

To try out the Pub/Sub API, run the Backend API from VS Code by running the below command or using the Run and Debug tasks we have created in the [appendix](../13-appendix/01-run-debug-dapr-app-vscode.md).

!!! note
    Don't forget to include the property `--resources-path`.  Remember to replace the placeholders with your own values

=== ".NET 6 or below"

    ```powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run
    ```

=== ".NET 7 or above"

    ```powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    ```

Now let's try to publish a message by sending a **POST** request to [http://localhost:3500/v1.0/publish/taskspubsub/tasksavedtopic](http://localhost:3500/v1.0/publish/taskspubsub/tasksavedtopic) with the below request body, don't forget to set the `Content-Type` header to `application/json`

```json
{
    "taskId": "fbc55b2c-d9fa-405e-aec8-22e53f4306dd",
    "taskName": "Testing Pub Sub Publisher",
    "taskCreatedBy": "user@mail.net",
    "taskCreatedOn": "2023-02-12T00:24:37.7361348Z",
    "taskDueDate": "2023-02-20T00:00:00",
    "taskAssignedTo": "user2@mail.com"
}
```

??? tip "Curious about the details of the endpoint"
    We can break endpoint into the following:

    - The value `3500`: is the Dapr app listing port, it is the port number upon which the Dapr sidecar is listening.
    - The value `taskspubsub`: is the name of the selected Dapr pub/sub-component.
    - The value `tasksavedtopic`: is the name of the topic to which the message is published.

If all is configured correctly, you should receive HTTP response 204 from this endpoint which indicates that the message is published successfully by the service broker (Redis) into the topic named `tasksavedtopic`.
You can also check that topic is created successfully by using the [Redis Xplorer extension](https://marketplace.visualstudio.com/items?itemName=davidsekar.redis-xplorer){target=_blank} in VS Code which should look like this:

![redis-xplorer](../../assets/images/05-aca-dapr-pubsubapi/task-saved-topic-redis-xplorer.png)

Right now those published messages are stored in the message broker topic doing nothing as we don't have any subscribers bound to the service broker on the topic `tasksavedtopic` which are interested in consuming and processing those messages. So let`s add a consumer to consume the message.

!!! note
    Some Service Brokers allow the creation of topics automatically when sending a message to a topic which has not been created before. That's the reason why the topic `tasksavedtopic` was created automatically here for us.

### Setting up the Backend Background Processor Project

#### 1. Create the Backend Service Project

Now we will add a new ASP.NET Core Web API project named **TasksTracker.Processor.Backend.Svc**. Open a command-line terminal and navigate to **root folder** of your project.

```powershell
dotnet new webapi -o TasksTracker.Processor.Backend.Svc
```

- Delete the boilerplate `WeatherForecast.cs` and `Controllers\WeatherForecastController.cs` files from the new `TasksTracker.Processor.Backend.Svc` project folder.

We need to containerize this application, so we can push it to Azure Container Registry as a docker image then deploy it to ACA.
To do so Open the VS Code Command Palette (++ctrl+shift+p++) and select **Docker: Add Docker Files to Workspace...**

- Use `.NET: ASP.NET Core` when prompted for application platform.
- Choose `Linux` when prompted to choose the operating system.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - Take a note of the provided **application port** as we will be using later on. You can always find it again inside the designated `DockerFile` inside the newly created project's directory.
    - `Dockerfile` and `.dockerignore` files are added to the workspace.

- Open `Dockerfile` and replace `FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:7.0 AS build` with `FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build`.

#### 2. Add Models

Now we will add the model which will be used to deserialize the published message. Add below file under new folder named **Models**.

=== "TaskModel.cs"

    ```csharp
    --8<-- "docs/aca/05-aca-dapr-pubsubapi/TaskModel.cs"
    ```

!!! tip
    For sake of simplicity we are recreating the same model `TaskModel.cs` under each project. For production purposes it is recommended to place the `TaskModel.cs` in a common project that can be referenced by all the projects and thus avoid code repetition which increases the maintenance cost.

#### 3.Install Dapr SDK Client NuGet package

Now we will install Dapr SDK to be able to subscribe to the service broker topic in a programmatic way. To do so, add the highlighted NuGet package to the file shown below:

=== ".NET 6"

    === "TasksTracker.Processor.Backend.Svc.csproj"

        ```xml
        <ItemGroup>
          <PackageReference Include="Dapr.AspNetCore" Version="{{ dapr.version }}" />
        </ItemGroup>
        ```

=== ".NET 7"

    === "TasksTracker.Processor.Backend.Svc.csproj"

        ```xml hl_lines="10"
        <Project Sdk="Microsoft.NET.Sdk.Web">
        
          <PropertyGroup>
            <TargetFramework>net7.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
      
          <ItemGroup>
            <PackageReference Include="Dapr.AspNetCore" Version="{{ dapr.version }}" />
            <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="7.0.13" />
            <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
          </ItemGroup>
        
        </Project>
        ```

#### 4. Create an API Endpoint for the Consumer to Subscribe to the Topic

Now we will add an endpoint that will be responsible to subscribe to the topic in the message broker we are interested in. This endpoint will start receiving the message published from the Backend API producer.
To do so, add a new controller under **Controllers** folder.

=== "TasksNotifierController.cs"

    ```csharp
    --8<-- "docs/aca/05-aca-dapr-pubsubapi/TasksNotifierController.cs"
    ```

??? tip "Curious about what we have done so far"

    - We have added an action method named `TaskSaved` which can be accessed on the route `api/tasksnotifier/tasksaved`
    - We have attributed this action method with the attribute `Dapr.Topic` which accepts the Dapr pub/sub component to target as the first argument, 
    and the second argument is the topic to subscribe to, which in our case is `tasksavedtopic`.
    - The action method expects to receive a `TaskModel` object.
    - Now once the message is received by this endpoint, we can start out the business logic to trigger sending an email (more about this next) and then return `200 OK` response to indicate that the consumer 
    processed the message successfully and the broker can delete this message.
    - If anything went wrong during sending the email (i.e. Email service not responding) and we want to retry processing this message at a later time, we return `400 Bad Request`, 
    which will inform the message broker that the message needs to be retired based on the configuration in the message broker.
    - If we need to drop the message as we are aware it will not be processed even after retries (i.e Email to is not formatted correctly) we return a `404 Not Found` response. 
    This will tell the message broker to drop the message and move it to dead-letter or poison queue.

Now you are probably wondering how the consumer was able to identify what are the subscriptions available and on which route they can be found at.
The answer for this is that at startup on the consumer service (more on that below after we add app.MapSubscribeHandler()), the Dapr runtime will call the application on a well-known endpoint to identify and create the required subscriptions.

The well-known endpoint can be reached on this endpoint: `http://localhost:<appPort>/dapr/subscribe`. When you invoke this endpoint, the response will contain an array of all available topics for which the applications will subscribe. Each includes a route to call when the topic receives a message. This was generated as we used the attribute `Dapr.Topic` on the action method `api/tasksnotifier/tasksaved`.

That means when a message is published on the PubSubname `taskspubsub` on the topic `tasksavedtopic`, it will be routed to the action method `/api/tasksnotifier/tasksaved` and will be consumed in this action method.

In our case, a sample response will be as follows:

```json
[
    {
    "pubsubname": "taskspubsub",
    "topic": "tasksavedtopic",
    "route": "/api/tasksnotifier/tasksaved"
    }
]
```

!!! tip
    Follow this [link](https://learn.microsoft.com/en-us/dotnet/architecture/dapr-for-net-developers/publish-subscribe#how-it-works){target=_blank} to find a detailed diagram of how the consumers will discover and subscribe to
    those endpoints.

#### 5. Register Dapr and Subscribe Handler at the Consumer Startup

Update below file in **TasksTracker.Processor.Backend.Svc** project.

=== ".NET 6"

    === "Program.cs"
    
        ```csharp hl_lines="9 13 15"
        namespace TasksTracker.Processor.Backend.Svc
        {
            public class Program
            {
                public static void Main(string[] args)
                {
                    var builder = WebApplication.CreateBuilder(args);
                    // Add services to the container.
                    builder.Services.AddControllers().AddDapr();
                    var app = builder.Build();
                    app.UseHttpsRedirection();
                    app.UseAuthorization();
                    app.UseCloudEvents();
                    app.MapControllers();
                    app.MapSubscribeHandler();
                    app.Run();
                }
            }
        }
        ```

=== ".NET 7"

    === "Program.cs"
    
        ```csharp hl_lines="4 22 26"
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers().AddDapr();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        var app = builder.Build();
        
        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        
        app.UseHttpsRedirection();
        
        app.UseAuthorization();
        
        app.UseCloudEvents();
        
        app.MapControllers();
        
        app.MapSubscribeHandler();
        
        app.Run();
        ```

- Let's verify that the Dapr dependency is restored properly and that the project compiles. From VS Code Terminal tab, open developer command prompt or PowerShell terminal and navigate to the parent directory which hosts the `.csproj` project folder and build the project.

    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Svc
    dotnet build
    ```

??? tip "Curious about the code above?"

    - On line `builder.Services.AddControllers().AddDapr();`, the extension method `AddDapr` registers the necessary services to integrate Dapr into the MVC pipeline. 
    It also registers a `DaprClient` instance into the dependency injection container, which then can be injected anywhere into your service.
    We will see how we are injecting DaprClient in the controller constructor later on.
    - On line `app.UseCloudEvents();`, the extension method `UseCloudEvents` adds CloudEvents middleware into the ASP.NET Core middleware pipeline. 
    This middleware will unwrap requests that use the CloudEvents structured format, so the receiving method can read the event payload directly. 
    You can read more about [CloudEvents](https://cloudevents.io/){target=_blank} here which includes specs for describing event data in a common and standard way.
    - On line `app.MapSubscribeHandler();`, we make the endpoint `http://localhost:<appPort>/dapr/subscribe` available for the consumer so it responds and returns available subscriptions. 
    When this endpoint is called, it will automatically find all WebAPI action methods decorated with the `Dapr.Topic` attribute and instruct Dapr to create subscriptions for them.

With all those bits in place, we are ready to run the publisher service `Backend API` and the consumer service `Backend Background Service` and test pub/sub pattern end to end.

```powershell
$BACKEND_SERVICE_APP_PORT=<backend service application https port number found under properties->launchSettings.json. e.g. 7051>
```

--8<-- "snippets/update-variables.md::5"

To do so, run the below commands in two separate PowerShell console, ensure you are on the right root folder of each respective project.

--8<-- "snippets/restore-variables.md:9:14"

=== ".NET 6 or below"

    ```powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    dotnet run 

    cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

    dapr run `
    --app-id tasksmanager-backend-processor `
    --app-port $BACKEND_SERVICE_APP_PORT `
    --dapr-http-port 3502 `
    --app-ssl `
    --resources-path "../components" `
    dotnet run
    ```

=== ".NET 7 or above"

    ```powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https

    cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

    dapr run `
    --app-id tasksmanager-backend-processor `
    --app-port $BACKEND_SERVICE_APP_PORT `
    --dapr-http-port 3502 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    ```

!!! note
    Notice that we gave the new Backend background service a Dapr App Id with the name `tasksmanager-backend-processor` and a Dapr HTTP port with the value `3502`.

Now let's try to publish a message by sending a **POST** request to [http://localhost:3500/v1.0/publish/taskspubsub/tasksavedtopic](http://localhost:3500/v1.0/publish/taskspubsub/tasksavedtopic) with the below request body, don't forget to set the `Content-Type` header to `application/json`

```json
POST /v1.0/publish/taskspubsub/tasksavedtopic HTTP/1.1
Host: localhost:3500
Content-Type: application/json
        
{
    "taskId": "fbc55b2c-d9fa-405e-aec8-22e53f4306dd",
    "taskName": "Testing Pub Sub Publisher",
    "taskCreatedBy": "user@mail.net",
    "taskCreatedOn": "2023-02-12T00:24:37.7361348Z",
    "taskDueDate": "2023-02-20T00:00:00",
    "taskAssignedTo": "user2@mail.com"
}
```

Keep an eye on the terminal logs of the Backend background processor as you will see that the message is received and consumed by the action method `api/tasksnotifier/tasksaved` and an information message is logged in the terminal to indicate the processing of the message.

??? tip "VS Code Dapr Extension"
    You can use the VS Code [Dapr Extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-dapr){target=_blank} to publish the message directly. It will be similar to the below image:

    ![dapr-pub-sub-code-extension](../../assets/images/05-aca-dapr-pubsubapi/dapr-pub-sub-code-extension.jpg)

#### 6. Optional: Update VS Code Tasks and Launch Configuration Files

If you have followed the steps in the [appendix](../13-appendix/01-run-debug-dapr-app-vscode.md) so far in order to be able to run the three services together (frontend, backend api, and backend processor) and debug them in VS Code, we need to update the files `tasks.json` and `launch.json` to include the new service we have added.

??? example "Click to expand the files to update"

    You can use the below files to update the existing ones.
    
    === "tasks.json"
    
        ```json
        --8<-- "docs/aca/05-aca-dapr-pubsubapi/tasks.json"
        ```

    === "launch.json"
    
        ```json
        --8<-- "docs/aca/05-aca-dapr-pubsubapi/launch.json"
        ```

This is a good opportunity to save intermediately:

--8<-- "snippets/update-variables.md:7:12"
--8<-- "snippets/persist-state.md:module51"

***

### Use the Dapr .NET Client SDK to Publish Messages

#### 1. Update Backend API to Publish a Message When a Task Is Saved

Now we need to update our Backend API to publish a message to the message broker when a task is saved (either due to a new task being added or an existing task assignee being updated).

To do this, update below file under the project **TasksTracker.TasksManager.Backend.Api** and update the file in the **Services** folder as highlighted below:

=== "TasksStoreManager.cs"

    ```csharp hl_lines="1-7 24 40-43"
    //Add new private method
    private async Task PublishTaskSavedEvent(TaskModel taskModel)
    {
        _logger.LogInformation("Publish Task Saved event for task with Id: '{0}' and Name: '{1}' for Assigne: '{2}'",
        taskModel.TaskId, taskModel.TaskName, taskModel.TaskAssignedTo);
        await _daprClient.PublishEventAsync("dapr-pubsub-servicebus", "tasksavedtopic", taskModel);
    }
    
    //Update the below method:
    public async Task<Guid> CreateNewTask(string taskName, string createdBy, string assignedTo, DateTime dueDate)
    {
        var taskModel = new TaskModel()
        {
            TaskId = Guid.NewGuid(),
            TaskName = taskName,
            TaskCreatedBy = createdBy,
            TaskCreatedOn = DateTime.UtcNow,
            TaskDueDate = dueDate,
            TaskAssignedTo = assignedTo,
        };
    
        _logger.LogInformation("Save a new task with name: '{0}' to state store", taskModel.TaskName);
        await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);
        await PublishTaskSavedEvent(taskModel);
        return taskModel.TaskId;
    }
        
    //Update the below method:
    public async Task<bool> UpdateTask(Guid taskId, string taskName, string assignedTo, DateTime dueDate)
        {
            _logger.LogInformation("Update task with Id: '{0}'", taskId);
            var taskModel = await _daprClient.GetStateAsync<TaskModel>(STORE_NAME, taskId.ToString());
            var currentAssignee = taskModel.TaskAssignedTo;
            if (taskModel != null)
            {
                taskModel.TaskName = taskName;
                taskModel.TaskAssignedTo = assignedTo;
                taskModel.TaskDueDate = dueDate;
                await _daprClient.SaveStateAsync<TaskModel>(STORE_NAME, taskModel.TaskId.ToString(), taskModel);
                if (!taskModel.TaskAssignedTo.Equals(currentAssignee, StringComparison.OrdinalIgnoreCase))
                {
                    await PublishTaskSavedEvent(taskModel);
                }
                return true;
            }
            return false;
        }
    ```

!!! tip
    Notice the new method `PublishTaskSavedEvent` added to the class. All we have to do is to call the method `PublishTaskSavedEvent` and pass the Pub/Sub name. In our case we named it `dapr-pubsub-servicebus` as we are going to use Azure Service Bus as a message broker in the next step.

    The second parameter `tasksavedtopic` is the topic name the publisher going to send the task model to. That's all the changes required to start publishing async messages from the Backend API.

#### 2. Update Backend Background Processor to Consume Messages and Simulate Sending Emails

Update the files below under the project **TasksTracker.Processor.Backend.Svc**. We are installing the NuGet package named `SendGrid` to the Backend processor project which will allow us to send emails.

=== "TasksNotifierController.cs"

    ```csharp
    --8<-- "docs/aca/05-aca-dapr-pubsubapi/TasksNotifierController-SendGrid.cs"
    ```

=== "TasksTracker.Processor.Backend.Svc.csproj"

    ```xml hl_lines="2"
    <ItemGroup> 
       <PackageReference Include="SendGrid" Version="9.28.1" />
    </ItemGroup>
    ```

=== "appsettings.json"

    ```json hl_lines="8-12"
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "AllowedHosts": "*",
      "SendGrid": {
        "ApiKey": "",
        "IntegrationEnabled":false
      }
    }
    ```

!!! tip
    Follow this [link](https://signup.sendgrid.com/){target=_blank} to set up a SendGrid account. Also follow [these](https://docs.sendgrid.com/ui/account-and-settings/api-keys#creating-an-api-key){target=_blank} instructions to fetch your SendGrid ApiKey.

    Please note that the SendGrid API KEY is generated and displayed to you just once. So be sure to copy and save it somewhere. After that only the subset key is displayed.

!!! tip
    In the [next module](../06-aca-dapr-bindingsapi/index.md) we will be using a new type of Dapr components to send emails using SendGrid.

    If you don't want to bother with signing up for a SendGrid account to send emails, you can just simulate sending emails by returning always `ok` from the `TaskSaved` method as shown below. 

    ```csharp hl_lines="4 7-14"
      // If you prefer not to deal with setting up a SendGrid account then simply always return OK from the TaskSaved method
      [Dapr.Topic("dapr-pubsub-servicebus", "tasksavedtopic")]
      [HttpPost("tasksaved")]
      public IActionResult TaskSaved([FromBody] TaskModel taskModel)
      {
          _logger.LogInformation("Started processing message with Task Name '{0}'", taskModel.TaskName);
          // I decided to simulate a call
          //var sendGridResponse = await SendEmail(taskModel);
    
          //if (sendGridResponse.Item1)
          //{
              return Ok($"SendGrid response status code: 200");
          //}
          //return BadRequest($"Failed to send email, SendGrid response status code: {sendGridResponse.Item1}");
      }
    ```

??? info "Curious to learn what's happening in the code above?"

    - We've updated the attribute `Dapr.Topic` to use the same Pub/Sub component name used in the publisher `dapr-pubsub-servicebus`. Then we added a new method that is responsible to consume the received message, 
    taking the assignee email and trying to send an email using SendGrid API.
    - We are returning `200 Ok` if the SendGrid was able to send the email successfully, otherwise we are returning `400 Bad Request` if the SendGrid failed to send the email.
    This will allow the consumer service to re-try processing the message again on failure.
    - We are reading the `SendGrid:ApiKey` from AppSettings and later we will read this value from environment variables once we deploy this service to Azure Container Apps. 

### Use Azure Service Bus as a Service Broker for Dapr Pub/Sub API

Now we will switch our implementation to use Azure Service Bus as a message broker. Redis worked perfectly for local development and testing, but we need to prepare ourselves for the cloud deployment. To do so we need to create Service Bus Namespace followed by a Topic. A namespace provides a scoping container for Service Bus resources within your application.

#### 1. Create Azure Service Bus Namespace and a Topic

You can do this from [Azure portal](https://portal.azure.com){target=_blank} or use the below PowerShell command to create the services. We will assume you are using the same PowerShell session from the previous module so variables still hold the right values.
You need to change the namespace variable as this one should be unique globally across all Azure subscriptions. Also, you will notice that we are opting for standard sku (default if not passed) as topics only available on the standard tier not and not on the basic tier. More details can be found [here](https://learn.microsoft.com/en-us/cli/azure/servicebus/namespace?view=azure-cli-latest#az-servicebus-namespace-create-optional-parameters){target=_blank}.

```powershell
$SERVICE_BUS_NAMESPACE_NAME="sbns-taskstracker-$RANDOM_STRING"
$SERVICE_BUS_TOPIC_NAME="tasksavedtopic"
$SERVICE_BUS_TOPIC_SUBSCRIPTION="sbts-tasks-processor"

# Create servicebus namespace
az servicebus namespace create --resource-group $RESOURCE_GROUP --name $SERVICE_BUS_NAMESPACE_NAME --location $LOCATION --sku Standard

# Create a topic under the namespace
az servicebus topic create --resource-group $RESOURCE_GROUP --namespace-name $SERVICE_BUS_NAMESPACE_NAME --name $SERVICE_BUS_TOPIC_NAME

# Create a topic subscription
az servicebus topic subscription create `
--resource-group $RESOURCE_GROUP `
--namespace-name $SERVICE_BUS_NAMESPACE_NAME `
--topic-name $SERVICE_BUS_TOPIC_NAME `
--name $SERVICE_BUS_TOPIC_SUBSCRIPTION

# List connection string
az servicebus namespace authorization-rule keys list `
--resource-group $RESOURCE_GROUP `
--namespace-name $SERVICE_BUS_NAMESPACE_NAME `
--name RootManageSharedAccessKey `
--query primaryConnectionString `
--output tsv
```

!!! note
    Primary connection string is only needed for local dev testing. We will be using Managed Identities when publishing container apps to ACA.

#### 2. Create a local Dapr Component file for Pub/Sub API Using Azure Service Bus

Add a new files **components** as shown below:

```yaml title="dapr-pubsub-svcbus.yaml"
--8<-- "docs/aca/05-aca-dapr-pubsubapi/dapr-pubsub-svcbus.yaml"
```

!!! note
    We used the name `dapr-pubsub-servicebus` which should match the name of Pub/Sub component we've used earlier in the [TasksNotifierController.cs](#4-create-an-api-endpoint-for-the-consumer-to-subscribe-to-the-topic) controller on the action method with the attribute `Topic`.

    We set the metadata (key/value) to allow us to connect to Azure Service Bus topic. The metadata `consumerID` value should match the topic subscription name `sbts-tasks-processor`. 

    We have set the scopes section to include the `tasksmanager-backend-api` and `tasksmanager-backend-processor` app ids, as those will be the Dapr apps that need access to Azure Service Bus for publishing and 
    consuming the messages.

#### 3. Create an ACA Dapr Component file for Pub/Sub API Using Azure Service Bus

Add a new files **aca-components** as shown below:

!!! note
    Remember to replace the namespace placeholder with the unique global name you chose earlier

```yaml title="containerapps-pubsub-svcbus.yaml"
# pubsub.yaml for Azure Service Bus component
--8<-- "docs/aca/05-aca-dapr-pubsubapi/containerapps-pubsub-svcbus.yaml"
```

???+ note "Things to note here"
     - We didn't specify the component name `dapr-pubsub-servicebus` when we created this component file. We are going to specify it once we add this dapr component to Azure Container Apps Environment via CLI.
     - We are not referencing any service bus connection strings as the authentication between Dapr and Azure Service Bus will be configured using Managed Identities.
     - The metadata `namespaceName` value is set to the address of the Service Bus namespace as a fully qualified domain name. The `namespaceName` key is mandatory when using Managed Identities for authentication.
     - We are setting the metadata `consumerID` value to match the topic subscription name `sbts-tasks-processor`. If you didn't set this metadata, dapr runtime will try to create a subscription using the dapr application ID.

With all those bits in place, we are ready to run the publisher service `Backend API` and the consumer service `Backend Background Service` and test pub/sub pattern end to end.

!!! note
    Ensure you are on the right root folder of each respective project. Remember to replace the placeholders with your own values.

=== ".NET 6 or below"

    ```powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    dotnet run 
    
    cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

    dapr run `
    --app-id tasksmanager-backend-processor `
    --app-port $BACKEND_SERVICE_APP_PORT `
    --dapr-http-port 3502 `
    --app-ssl `
    --resources-path "../components" `
    dotnet run
    ```

=== ".NET 7 or above"

    ````powershell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    
    cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

    dapr run `
    --app-id tasksmanager-backend-processor `
    --app-port $BACKEND_SERVICE_APP_PORT `
    --dapr-http-port 3502 `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    ````

!!! note
    We gave the new Backend background service a Dapr App Id with the name `tasksmanager-backend-processor` and a Dapr HTTP port with the value **3502**.

Now let's try to publish a message by sending a **POST** request to [http://localhost:3500/v1.0/publish/**dapr-pubsub-servicebus**/tasksavedtopic](http://localhost:3500/v1.0/publish/dapr-pubsub-servicebus/tasksavedtopic) with the below request body, don't forget to set the `Content-Type`
header to `application/json`

```json
POST /v1.0/publish/dapr-pubsub-servicebus/tasksavedtopic HTTP/1.1
Host: localhost:3500
Content-Type: application/json
{
    "taskId": "fbc55b2c-d9fa-405e-aec8-22e53f4306dd",
    "taskName": "Testing Pub Sub Publisher",
    "taskCreatedBy": "user@mail.net",
    "taskCreatedOn": "2023-02-12T00:24:37.7361348Z",
    "taskDueDate": "2023-02-20T00:00:00",
    "taskAssignedTo": "user2@mail.com"
}
```

You should see console messages from APP in the backend service console as you send requests.

### Deploy the Backend Background Processor and the Backend API Projects to Azure Container Apps

#### 1. Build the Backend Background Processor and the Backend API App Images and Push Them to ACR

As we have done previously we need to build and deploy both app images to ACR, so they are ready to be deployed to Azure Container Apps.

!!! note
    Make sure you are in root directory of the project, i.e. **TasksTracker.ContainerApps**

```powershell
$BACKEND_SERVICE_NAME="tasksmanager-backend-processor"

az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$BACKEND_API_NAME" `
--file 'TasksTracker.TasksManager.Backend.Api/Dockerfile' . 

az acr build `
--registry $AZURE_CONTAINER_REGISTRY_NAME `
--image "tasksmanager/$BACKEND_SERVICE_NAME" `
--file 'TasksTracker.Processor.Backend.Svc/Dockerfile' .
```

#### 2. Create a new Azure Container App to host the new Backend Background Processor

Now we need to create a new Azure Container App. We need to have this new container app with those capabilities in place:

- Ingress for this container app should be disabled (no access via HTTP at all as this is a background processor responsible to process published messages).
- Dapr needs to be enabled.
- **Optional** (only if you activated your SendGrid account and received an api key. Otherwise, remove the `--secrets` and `--env-var` from the powershell command below). 
Setting the value of SendGrid API in the secrets store and referencing it in the environment variables, as well setting the flag `IntegrationEnabled` to `true` so it will send actual emails.

To achieve the above, run the PowerShell script below.

!!! note
    Notice how we removed the Ingress property totally which disables the Ingress for this Container App. Remember to replace the placeholders with your own values:

=== "Using SendGrid"

    ```powershell
    az containerapp create `
    --name "$BACKEND_SERVICE_NAME"  `
    --resource-group $RESOURCE_GROUP `
    --environment $ENVIRONMENT `
    --image "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io/tasksmanager/$BACKEND_SERVICE_NAME" `
    --registry-server "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io" `
    --min-replicas 1 `
    --max-replicas 1 `
    --cpu 0.25 --memory 0.5Gi `
    --enable-dapr `
    --dapr-app-id $BACKEND_SERVICE_NAME `
    --dapr-app-port $TARGET_PORT `
    --secrets "sendgrid-apikey=<Replace with your SendGrid API Key>" `
    --env-vars "SendGrid__ApiKey=secretref:sendgrid-apikey" "SendGrid__IntegrationEnabled=true"
    ```

=== "Not Using SendGrid"

    ```powershell
    az containerapp create `
    --name "$BACKEND_SERVICE_NAME"  `
    --resource-group $RESOURCE_GROUP `
    --environment $ENVIRONMENT `
    --image "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io/tasksmanager/$BACKEND_SERVICE_NAME" `
    --registry-server "$AZURE_CONTAINER_REGISTRY_NAME.azurecr.io" `
    --min-replicas 1 `
    --max-replicas 1 `
    --cpu 0.25 --memory 0.5Gi `
    --enable-dapr `
    --dapr-app-id $BACKEND_SERVICE_NAME `
    --dapr-app-port $TARGET_PORT
    ```

#### 3. Deploy New Revisions of the Backend API to Azure Container Apps

We need to update the Azure Container App hosting the Backend API with a new revision so our code changes for publishing messages after a task is saved is available for users.

```powershell
# Update Backend API App container app and create a new revision 
az containerapp update `
--name $BACKEND_API_NAME `
--resource-group $RESOURCE_GROUP `
--revision-suffix v$TODAY-2
```

#### 4. Add Azure Service Bus Dapr Pub/Sub Component to Azure Container Apps Environment

Deploy the Dapr Pub/Sub Component to the Azure Container Apps Environment using the following command:

```powershell
az containerapp env dapr-component set `
--name $ENVIRONMENT `
--resource-group $RESOURCE_GROUP `
--dapr-component-name dapr-pubsub-servicebus `
--yaml '.\aca-components\containerapps-pubsub-svcbus.yaml'
```

!!! note
    Notice that we set the component name `dapr-pubsub-servicebus` when we added it to the Container Apps Environment.

### Configure Managed Identities for Both Container Apps

In the previous module we have [already configured](../04-aca-dapr-stateapi/index.md#configure-managed-identities-in-container-app) and used system-assigned identity for the Backend API container app. We follow the same steps here to create an association between the backend processor container app and Azure Service Bus.

#### 1. Create system-assigned identity for Backend Processor App

Run the command below to create `system-assigned` identity for our Backend Processor App:

```powershell
az containerapp identity assign `
--resource-group $RESOURCE_GROUP `
--name $BACKEND_SERVICE_NAME `
--system-assigned

$BACKEND_SVC_PRINCIPAL_ID=(az containerapp identity show `
--name $BACKEND_SERVICE_NAME `
--resource-group $RESOURCE_GROUP `
--query principalId `
--output tsv)
```

This command will create an Enterprise Application (basically a Service Principal) within Azure AD, which is linked to our container app. The output of this command will be as the below, keep a note of the property `principalId` as we are going to use it in the next step.

```json
{
    "principalId": "<your principal id will be displayed here>",
    "tenantId": "<your tenant id will be displayed here>",
    "type": "SystemAssigned"
}
```

#### 2. Grant Backend Processor App the Azure Service Bus Data Receiver Role

We will be using a `system-assigned` managed identity with a role assignments to grant our Backend Processor App the `Azure Service Bus Data Receiver` role which will allow it to receive messages from Service Bus queues and subscriptions.

You can read more about `Azure built-in roles for Azure Service Bus` [here](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-managed-service-identity#azure-built-in-roles-for-azure-service-bus){target=_blank}.

Run the command below to associate the `system-assigned` identity with the access-control role `Azure Service Bus Data Receiver`:

```powershell
$SVC_BUS_DATA_RECEIVER_ROLE = "Azure Service Bus Data Receiver" # Built in role name

az role assignment create `
--assignee $BACKEND_SVC_PRINCIPAL_ID `
--role $SVC_BUS_DATA_RECEIVER_ROLE `
--scope /subscriptions/$AZURE_SUBSCRIPTION_ID/resourcegroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/$SERVICE_BUS_NAMESPACE_NAME
```

#### 3. Grant Backend API App the Azure Service Bus Data Sender Role

We'll do the same with Backend API container app, but we will use a different Azure built-in roles for Azure Service Bus which is the role `Azure Service Bus Data Sender` as the Backend API is a publisher of the messages. Run the command below to associate the `system-assigned` with access-control role `Azure Service Bus Data Sender`:

```powershell
$SVC_BUS_DATA_SENDER_ROLE = "Azure Service Bus Data Sender" # Built in role name

az role assignment create `
--assignee $BACKEND_API_PRINCIPAL_ID `
--role $SVC_BUS_DATA_SENDER_ROLE `
--scope /subscriptions/$AZURE_SUBSCRIPTION_ID/resourcegroups/$RESOURCE_GROUP/providers/Microsoft.ServiceBus/namespaces/$SERVICE_BUS_NAMESPACE_NAME
```

#### 4. Restart Container Apps

Lastly, we need to restart both container apps revisions to pick up the role assignment.

```powershell
# Get revision name and assign it to a variable
$REVISION_NAME = (az containerapp revision list `
        --name $BACKEND_SERVICE_NAME  `
        --resource-group $RESOURCE_GROUP `
        --query [0].name)

# Restart revision by name
az containerapp revision restart `
--resource-group $RESOURCE_GROUP `
--name $BACKEND_SERVICE_NAME  `
--revision $REVISION_NAME

$REVISION_NAME = (az containerapp revision list `
        --name $BACKEND_API_NAME  `
        --resource-group $RESOURCE_GROUP `
        --query [0].name)

# Restart revision by name
az containerapp revision restart `
--resource-group $RESOURCE_GROUP `
--name $BACKEND_API_NAME  `
--revision $REVISION_NAME
```

!!! Success
    With this in place, you should be able to test the 3 services end to end and should receive a notification email to the task assignee email, the email will look like the below.

    ![email-log](../../assets/images/05-aca-dapr-pubsubapi/email-log.jpg)

    !!! note
    
        If you opted not to activate the SendGrid then you won't receive the email. In this case you can get the backend processor logs using the command below. Start by running the command below and then launch the
        application and start creating new tasks. You should start seeing logs similar to the ones shown in the image below. The command will stop executing after 60 seconds of inactivity.
    
        ```powershell
        az containerapp logs show --follow `
        -n $BACKEND_SERVICE_NAME `
        -g $RESOURCE_GROUP
        ```
    
        ![email-log](../../assets/images/05-aca-dapr-pubsubapi/az_containerapp_logs.png)

??? tip "What to do if you do not see messages?"
    Sometimes, the revision creation right after creating the managed identity results in the identity not yet being picked up properly. This becomes evident when we look at the Backend Service's Container App's `Log stream` blade in the [Azure portal](https://portal.azure.com){target=_blank}. Specifically, the `daprd` sidecar container will show HTTP 401 errors.

    Should this be the case, you can navigate to the `Revisions` blade, click on the active revision, then press `Restart`. Going back to the `daprd` sidecar in the `Log Stream` should now reveal processing of messages.

--8<-- "snippets/update-variables.md"
--8<-- "snippets/persist-state.md:module52"

The next module will delve into the implementation of Dapr bindings with ACA.
