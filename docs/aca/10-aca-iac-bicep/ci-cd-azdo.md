
# Deploy Infrastructure Using Azure DevOps

!!! info "Module Duration"
    30 minutes

In the [previous section](../../aca/10-aca-iac-bicep/iac-bicep.md), we demonstrated how Bicep scripts can be used to automate the deployment of infrastructure components. However, creating the container registry and deploying the Bicep scripts using the Azure CLI still required manual effort. For a more efficient and streamlined process, it's preferable to use automation. Azure DevOps is a great solution for automating workflows, and in this section, we'll explain how to create a Azure DevOps pipeline for deploying the infrastructure components of our application.

The workshop repository contains a Azure Devops Pipeline yaml file that will be used to deploy the infrastructure components of our application. Follow the steps below to create a devops pipeline to deploy the infrastructure components of our application.

!!! note
        The following instructions assume that you will utilize the forked Github repository both as the host for your YAML pipeline and the source code. However, it is possible to host the same assets in your Azure DevOps repository instead, if that is your preference. It is important to remember that if you choose to store your assets in your Azure DevOps repository, you will have to direct your Azure DevOps pipeline towards the Azure DevOps repository instead of the Github repository.

### Fork the GitHub repository

Start by forking the workshop repository to your GitHub account. Follow the steps below to fork the workshop:

1. Navigate to the workshop repository at [:material-github: Azure/aca-dotnet-workshop](https://github.com/Azure/aca-dotnet-workshop){target=_blank}
2. Click the **Fork** button in the top-right corner of the page.
3. Select your GitHub account to fork the repository to.
4. Wait for the repository to be forked.

### Configure a Service Connection for GitHub and Azure Subscription

Before we start with creating pipeline, we need to configure service connection for GitHub and Azure Subscription. You can do this in either existing or new project.

#### Create a Service Connection for GitHub

Provide access to the repository forked above by creating a service connection to GitHub. You create a new pipeline by first selecting a GitHub repository and then a YAML file in repository at path [.ado/infra-deploy.yml](https://raw.githubusercontent.com/Azure/aca-dotnet-workshop/main/.ado/infra-deploy.yml){target=_blank}.

The repository in which the YAML file is present is called self repository. By default, this is the repository that your pipeline builds.

There are three authentication types for granting Azure Pipelines access to your GitHub repositories while creating a pipeline. Follow guide at this [link](https://learn.microsoft.com/azure/devops/pipelines/repos/github?view=azure-devops&tabs=yaml#access-to-github-repositories){target=_blank}
to create service connection for GitHub.

![AZDO GitHub Connection](../../assets/gifs/azdo-github-connection.gif)

#### Create Service Connection for Azure Subscription

Create a new service connection to your Azure subscription by following the steps at this [link](https://docs.microsoft.com/azure/devops/pipelines/library/service-endpoints?view=azure-devops&tabs=yaml#create-a-service-connection){target=_blank}.

!!! note
    Update the created service connection role to have **[User Access Administrator](https://learn.microsoft.com/azure/role-based-access-control/built-in-roles#user-access-administrator){target=_blank}** role. This is required for pipeline to be able to perform role assignments in the infrastructure components deployed. To update the role of a service connection in Azure DevOps to have the User Access Administrator role, you can follow these steps:

    - Navigate to the [Azure portal](https://portal.azure.com){target=_blank} and select the subscription where the service connection is created.

    - Click on **Access control (IAM)** in the left-hand menu.

    - Click on **Add role assignment**.

    - For the **Assignment type** choose **Privileged administrator roles**.

    - In the **Role** section choose **User Access Administrator**.

    - In the **Members** section, search for the name of the service connection that you want to update and select it.

    - Click **Save** to apply the changes.

### Configure Variable Group under Azure DevOps Library Section

Create a variable group named **AcaApp** under Library in your Azure Devops project. Make sure the pipeline has permissions to access the created variable group under **Pipeline permissions**.

This variable group will be used to store below details:

```bash
# AZURE_SUBSCRIPTION: Name of the service connection created for Azure Subscription
AZURE_SUBSCRIPTION=<service connection name>

# LOCATION: Azure region where resources will be deployed
LOCATION=<location>

# RESOURCE_GROUP: Name of the resource group which will be created and where the resources will be deployed
RESOURCE_GROUP=<resource group name>

# (OPTIONAL)CONTAINER_REGISTRY_NAME: Unique name of the container registry which will be created and where images will be imported
CONTAINER_REGISTRY_NAME=<container registry name>
```

!!! note

    Repository variable `CONTAINER_REGISTRY_NAME` is only needed by pipeline if you intend to deploy images from a private Azure Container Registry (ACR). You may chose to skip defining this variable and the pipeline will use the [public github container registry images](https://github.com/orgs/Azure/packages?repo_name=aca-dotnet-workshop){target=_blank} to deploy the images.

### Trigger Azure Devops Pipeline

With these steps completed, you are now ready to trigger the Pipeline.

!!! success

    Your Pipeline should be triggered and the infrastructure components of our application should be deployed successfully.

    ![GitHub Actions Workflow](../../assets/gifs/azdo-trigger.gif)

??? info "Want to delete the resources deployed by the pipeline?"

    Trigger the pipeline again select **checkbox** option named **Should teardown infrastructure?**.

    ![GitHub Actions Workflow](../../assets/gifs/azdo-delete.gif)
