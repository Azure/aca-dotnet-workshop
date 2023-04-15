---
canonical_url: https://bitoftech.net/2022/09/16/use-bicep-to-deploy-dapr-microservices-apps-to-azure-container-apps-part-10/
---

# Module 10 - Deployment Via Bicep

Throughout the various modules, we have utilized various Azure CLI commands to provision different resources. While this approach is suitable for this workshop, in a production environment, you will likely require a more automated process to deploy the same resources. In this module, we will be working on defining the proper process to automate the infrastructure provisioning by creating the scripts/templates to provision the resources. This process is known as IaC (Infrastructure as Code).

Once we have this in place, IaC deployments will benefit us in key ways such as:

1. By ensuring consistency and reducing human errors in resource provisioning, deployments can be made with greater confidence and consistency.
2. Avoid configuration drifts as IaC is an idempotent operation, which means it provides the same result each time it’s run.
3. With Infrastructure as Code (IaC) in place, recreating an identical environment to the production one becomes a simple task of executing the scripts. This can be particularly useful during the application's lifecycle when short-term isolation is needed for tasks such as penetration testing or load testing.
4. The Azure Portal abstracts several processes when you provision resources. For instance, when you create an Azure Container Apps Environment from the portal, it automatically creates a log analytics workspace and associates it with the environment without your direct involvement. However, using Infrastructure as Code (IaC) can provide you with a deeper understanding of Azure and help you troubleshoot any issues that may arise more effectively.

### ARM Templates in Azure

ARM templates are files that define the infrastructure and configuration for your deployment. The templates use declarative syntax, which lets you state what you intend to deploy without having to write the sequence of programming commands to create it.

Within Azure there are two ways to create IaC. We can either use the [JSON ARM templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) or [Bicep](https://docs.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview?tabs=bicep) (domain-specific language). As a project grows and the number of components and dependencies increases, working with JSON ARM templates in real-world scenarios can become increasingly complex and difficult to manage and maintain. Bicep provides a more user-friendly and straightforward experience when compared to ARM templates, resulting in increased productivity. However, it's worth noting that Bicep code is eventually compiled into ARM templates through a process called "transpilation."

![aca-arm-bicep](../../assets/images/10-aca-iac-bicep/aca-bicep-l.jpg)

!!! tip
    For those interested in learning more about Bicep, it is recommended to visit the Microsoft Learn website [Fundamentals of Bicep](https://docs.microsoft.com/en-us/training/paths/fundamentals-bicep/).
