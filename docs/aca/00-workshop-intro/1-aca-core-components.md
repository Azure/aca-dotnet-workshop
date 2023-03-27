---
canonical_url: https://bitoftech.net/2022/08/25/tutorial-building-microservice-applications-azure-container-apps-dapr/
---

## Overview of Azure Container Apps Core Components

The main components of Azure Container Apps are:

![Azure Container Apps main components](../../assets/images/00-workshop-intro/ACA-Tutorial-ACA-Components.jpg)

**1. Environments**
The Environment is a secure boundary around several Container Apps. It contains one or more container apps. All container apps within an environment are deployed into a dedicated Azure Virtual Network, which makes it possible for these different container apps to communicate securely. In addition, all the logs produced from all container apps in the environment are sent to a dedicated Log Analytics workspace.

**2. Log Analytics Workspace**
Used to provide monitoring and observability functionality. Each environment will have its own Log Analytic workspace and will be shared among all container apps within the environment.

**3. Container Apps**
Each container App represents a single deployable unit that can contain one or more related containers. More than one container is an advanced use case. For this workshop we will deploy a single container in each container app. More about multiple containers in the same single Azure Container App can be found [here](https://docs.microsoft.com/en-us/azure/container-apps/containers#multiple-containers).

**4. Revisions**
For each container app, you can create up to 100 revisions. Revisions are a way to deploy multiple versions of an app where you have the option to send the traffic to a certain revision. You can select if revision mode will support 1 active revision or multiple active revisions at the same time to support A/B testing scenarios or canary deployments. A container app running in single revision mode will have a single revision that is backed by zero-many Pods/replicas.

**5. Containers**
Containers in the Azure Container Apps are grouped together in pods/replicas inside revision snapshots. A Pod/replica is composed of the application container and any required sidecar containers. Containers can be deployed from any public or private container registry, and they support any Linux-based x86-64 (linux/amd64) images. At the time of creating this workshop Windows based images are not supported.