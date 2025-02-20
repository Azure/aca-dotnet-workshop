- From the VS Code Terminal tab, open developer command prompt or PowerShell terminal in the project folder `TasksTracker.ContainerApps` (*root* of your project):

=== "PowerShell"
    ```shell
    cd ~\TasksTracker.ContainerApps
    ```
=== "Bash"
    ```shell
    # If you still have the variables set
    cd $PROJECT_ROOT
    # Or manually cd into your project's root eg cd ~/TasksTracker.ContainerApps
    ```

- Restore the previously-stored variables by executing the local script. The output informs you how many variables have been set.

=== "PowerShell"
    ```shell
    .\Variables.ps1
    ```
=== "Bash"
    ```shell
    . ./variables.sh
    ```