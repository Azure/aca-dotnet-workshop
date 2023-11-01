- Execute the `Set-Variables.ps1` in the root to update the `variables.ps1` file with all current variables. The output of the script will inform you how many variables are written out.

    ```shell
    .\Set-Variables.ps1
    ```

- Persist a list of all current variables.

    ```shell
    git add .\Variables.ps1
    git commit -m "Update Variables.ps1"
    ```
