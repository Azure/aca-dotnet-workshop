# Variables

We declare numerous variables throughout this workshop. As these modules are lengthy, you will likely complete them over multiple sessions. However, as sessions are additive, they require previously-set variables.

## Setting Variables

Execute this script to persist all variables in the current session at any time. We recommend you do this after you complete each module or any other time you are taking a break from the workshop.

=== "Set-Variables.ps1"
    ```powershell
    --8<-- "docs/aca/13-appendix/Set-Variables.ps1"
    ```

## Restoring Variables

The output of the `Set-Variables.ps1` script is stored in the same directory where you execute that script. You can quickly apply those variables and get back to a working state by executing `.\Variables.ps1` in your PowerShell console. This is useful after having taken a break from the workshop and losing the session or when you are asked to open a second session such as when you are running multiple microservices locally with dapr.
