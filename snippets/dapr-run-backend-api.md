<!-- Basic snippet -->
--8<-- [start:basic]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

dapr run `
--app-id tasksmanager-backend-api `
--app-port $API_APP_PORT `
--dapr-http-port 3500 `
--app-ssl `
-- dotnet run --launch-profile https
```
--8<-- [end:basic]

<!-- Dapr Components snippet -->
--8<-- [start:dapr-components]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

dapr run `
--app-id tasksmanager-backend-api `
--app-port $API_APP_PORT `
--dapr-http-port 3500 `
--app-ssl `
--resources-path "../components" `
-- dotnet run --launch-profile https
```
--8<-- [end:dapr-components]

<!-- .NET 6 -->

<!-- Basic .NET 6 snippet -->
--8<-- [start:basic-dotnet6]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

dapr run `
--app-id tasksmanager-backend-api `
--app-port $API_APP_PORT `
--dapr-http-port 3500 `
--app-ssl `
-- dotnet run
```
--8<-- [end:basic-dotnet6]

<!-- Dapr Components .NET 6 snippet -->
--8<-- [start:dapr-components-dotnet6]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

dapr run `
--app-id tasksmanager-backend-api `
--app-port $API_APP_PORT `
--dapr-http-port 3500 `
--app-ssl `
--resources-path "../components" `
-- dotnet run
```
--8<-- [end:dapr-components-dotnet6]
