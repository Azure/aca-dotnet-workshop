- From the root folder of your project, execute the script you created in the Prerequisites section to persist a list of all current variables.

=== "PowerShell"
    Execute the `Set-Variables.ps1` in the root to update the `variables.ps1` file with all current variables. The output of the script will inform you how many variables are written out.
    ```shell
    .\Set-Variables.ps1
    ```
=== "Bash"
    Execute the `set_variables.sh` in the root to update the `variables.sh` file with all current variables. The output of the script will inform you how many variables are written out.
    ```shell
    ./set_variables.sh
    ```

- From the root folder of your project, persist a list of all current variables.

=== "PowerShell"
    ```shell
    git add .\Variables.ps1
    git commit -m "Update Variables.ps1"
    ```
=== "Bash"
    ```shell
    git add ./variables.sh
    git commit -m "Update variables.sh"
    ```