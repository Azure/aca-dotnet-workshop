- We need to containerize this application, so we can push it to the Azure Container Registry before we deploy it to Azure Container Apps:

    - Open the VS Code Command Palette (++ctrl+shift+p++) and select **Docker: Add Docker Files to Workspace...**
    - Use `.NET: ASP.NET Core` when prompted for the application platform.
    - Choose the newly-created project, if prompted.
    - Choose `Linux` when prompted to choose the operating system.
    - Set the **application port** to `8080`, which is the default non-privileged port since .NET 8.
    - You will be asked if you want to add Docker Compose files. Select `No`.
    - `Dockerfile` and `.dockerignore` files are added to the project workspace.
    - Open `Dockerfile` and remove `--platform=$BUILDPLATFORM` from the `FROM` instruction.

        !!! bug "Dockerfile Build Platform"

            Azure Container Registry does not set `$BUILDPLATFORM` presently when building containers. This consequently causes the build to fail. See [this issue](https://github.com/microsoft/vscode-docker/issues/4149){target=_blank} for details. Therefore, we remove it from the file for the time being. We expect this to be corrected in the future.
