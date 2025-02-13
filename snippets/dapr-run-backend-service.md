<!-- Dapr Components snippet -->
--8<-- [start:dapr-components]

=== "PowerShell"
    ```shell
    cd ~\TasksTracker.ContainerApps\TasksTracker.Processor.Backend.Svc

    dapr run `
    --app-id tasksmanager-backend-processor `
    --app-port $BACKEND_SERVICE_APP_PORT `
    --dapr-http-port 3502 `
    --scheduler-host-address "" `
    --app-ssl `
    --resources-path "../components" `
    -- dotnet run --launch-profile https
    ```
=== "Bash"
    ```shell
    cd $PROJECT_ROOT/TasksTracker.Processor.Backend.Svc

    dapr run \
    --app-id tasksmanager-backend-processor \
    --app-port $BACKEND_SERVICE_APP_PORT \
    --dapr-http-port 3502 \
    --scheduler-host-address "" \
    --app-ssl \
    --resources-path "../components" \
    -- dotnet run --launch-profile https
    ```

!!! note
    An [issue with dapr-scheduler](https://github.com/Azure/aca-dotnet-workshop/issues/168){target=_blank} presently exists with Dapr 1.4. However, this should not affect the labs negatively.
--8<-- [end:dapr-components]
