<!-- Dapr Components snippet -->
--8<-- [start:dapr-components]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

dapr run `
--app-id tasksmanager-backend-processor `
--app-port $BACKEND_SERVICE_APP_PORT `
--dapr-http-port 3502 `
--app-ssl `
--resources-path "../components" `
-- dotnet run --launch-profile https
```
--8<-- [end:dapr-components]
