<!-- Basic snippet -->
--8<-- [start:basic]
```shell
cd ~\TasksTracker.ContainerApps\TasksTracker.WebPortal.Frontend.Ui

dapr run `
--app-id tasksmanager-frontend-webapp `
--app-port $UI_APP_PORT `
--dapr-http-port 3501 `
--scheduler-host-address "" `
--app-ssl `
-- dotnet run --launch-profile https
```

!!! note
    An [issue with dapr-scheduler](https://github.com/Azure/aca-dotnet-workshop/issues/168){target=_blank} presently exists with Dapr 1.4. However, this should not affect the labs negatively.
--8<-- [end:basic]
