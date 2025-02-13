---
title: Variables
parent: Appendix
has_children: false
nav_order: 3
canonical_url: 'https://azure.github.io/aca-dotnet-workshop'
---

# Variables

We declare numerous variables throughout this workshop. As these modules are lengthy, you will likely complete them over multiple sessions. However, as sessions are additive, they require previously-set variables.


## Setting Variables

Execute this script to persist all variables in the current session at any time. We recommend you do this after you complete each module or any other time you are taking a break from the workshop. 


=== "PowerShell"
    Execute this script in the root (e.g. `~\TasksTracker.ContainerApps`) to keep setting and updating the same `Variables.ps1` file.
    ```powershell
    --8<-- "docs/aca/30-appendix/Set-Variables.ps1"
    ```
=== "Bash"
    Copy this script in the project's root (e.g. `$PROJECT_ROOT$`).
    ```shell
    -8<-- "docs/aca/30-appendix/set_variables.sh"
    ```
    Make the script executable and execute it to keep setting and updating the same `variables.sh` file.
    ```shell
    chmod +x ./set_variables.sh
    ./set_variables.sh
    ```

## Restoring Variables

=== "PowerShell"
    The output of the `Set-Variables.ps1` script is stored in the same directory where you execute that script. You can quickly apply those variables and get back to a working state by executing `.\Variables.ps1` in your PowerShell console. This is useful after having taken a break from the workshop and losing the session or when you are asked to open a second session such as when you are running multiple microservices locally with dapr.
=== "Bash"
    The output of the `set_variables.sh` script is stored in the same directory where you execute that script. You can quickly apply those variables and get back to a working state by executing `. ./variables.sh` in your shell console. This is useful after having taken a break from the workshop and losing the session or when you are asked to open a second session such as when you are running multiple microservices locally with dapr.
