cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api

dapr run `
--app-id tasksmanager-backend-api `
--app-port $API_APP_PORT `
--dapr-http-port 3500 `
--app-ssl `
--resources-path "../components" `
-- dotnet run --launch-profile https