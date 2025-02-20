<!-- Basic snippet -->
--8<-- [start:basic]
=== "PowerShell"
    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    
    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --scheduler-host-address "" `
    --app-ssl `
    -- dotnet run --launch-profile https
    ```
=== "Bash"
    ```shell
    cd $PROJECT_ROOT/TasksTracker.TasksManager.Backend.Api
    
    dapr run \
    --app-id tasksmanager-backend-api \
    --app-port $API_APP_PORT \
    --dapr-http-port 3500 \
    --scheduler-host-address "" \
    --app-ssl \
    -- dotnet run --launch-profile https
    ```

!!! note
    An [issue with dapr-scheduler](https://github.com/Azure/aca-dotnet-workshop/issues/168){target=_blank} presently exists with Dapr 1.4. However, this should not affect the labs negatively.
--8<-- [end:basic]

<!-- Dapr Components snippet -->
--8<-- [start:dapr-components]
=== "PowerShell"
    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.TasksManager.Backend.Api
    
    dapr run `
    --app-id tasksmanager-backend-api `
    --app-port $API_APP_PORT `
    --dapr-http-port 3500 `
    --app-ssl `
    --scheduler-host-address "" `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    ```
=== "Bash"
    ```shell
    cd $PROJECT_ROOT/TasksTracker.TasksManager.Backend.Api
    
    dapr run \
    --app-id tasksmanager-backend-api \
    --app-port $API_APP_PORT \
    --dapr-http-port 3500 \
    --app-ssl \
    --scheduler-host-address "" \
    --resources-path "../components" \
    -- dotnet run --launch-profile https
    ```

!!! note
    An [issue with dapr-scheduler](https://github.com/Azure/aca-dotnet-workshop/issues/168){target=_blank} presently exists with Dapr 1.4. However, this should not affect the labs negatively.
--8<-- [end:dapr-components]
