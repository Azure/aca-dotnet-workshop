# Variables

We declare numerous variables throughout this workshop. As these modules are lengthy, you will likely complete them over multiple sessions. However, as sessions are additive, they require previously-set variables.


!!! info "Shell Support"
    Presently, this supports PowerShell only, and we would like to see community contributions for shell scripts, please. Please see [GitHub Issue #111](https://github.com/Azure/aca-dotnet-workshop/issues/111){target=_blank}.

## Setting Variables

### PowerShell

Execute this script to persist all variables in the current session at any time. We recommend you do this after you complete each module or any other time you are taking a break from the workshop. Execute it in the root (e.g. `~\TasksTracker.ContainerApps`) to keep setting and updating the same `Variables.ps1` file.

=== "Set-Variables.ps1"
    ```powershell
    --8<-- "docs/aca/13-appendix/Set-Variables.ps1"
    ```

## Restoring Variables

### PowerShell

The output of the `Set-Variables.ps1` script is stored in the same directory where you execute that script. You can quickly apply those variables and get back to a working state by executing `.\Variables.ps1` in your PowerShell console. This is useful after having taken a break from the workshop and losing the session or when you are asked to open a second session such as when you are running multiple microservices locally with dapr.
