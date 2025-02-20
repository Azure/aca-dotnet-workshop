---
title: ACA Core Components Overview
parent: Workshop Introduction
has_children: false
nav_order: 1
canonical_url: https://bitoftech.net/2022/08/25/tutorial-building-microservice-applications-azure-container-apps-dapr/
---

## Overview of Azure Container Apps Core Components

The main components of Azure Container Apps are:

![Azure Container Apps main components](../../assets/images/00-workshop-intro/ACA-Tutorial-ACA-Components.jpg)

1. **Environments**
The Container App Environment is a secure boundary around several Container Apps. It contains one or more container apps. All container apps within an environment are deployed into a dedicated Azure Virtual Network, which makes it possible for these different container apps to communicate securely. In addition, all the logs produced from all container apps in the environment are sent to a dedicated Log Analytics workspace.

1. **Log Analytics workspace**
Used to provide monitoring and observability functionality. Each environment will have its own Log Analytics workspace and will be shared among all container apps within the environment.

1. **Container Apps**
Each container App represents a single deployable unit that can contain one or more related containers. Using more than one container in a container app is an advanced use case. For this workshop we will deploy a single container in each container app. More about multiple containers in the same single Azure Container App can be found [here](https://docs.microsoft.com/azure/container-apps/containers#multiple-containers){target=_blank}.

1. **Revisions**
For each container app, you can create up to 100 revisions. Revisions are a way to deploy multiple versions of an app where you have the option to send the traffic to a certain revision. You can select if revision mode will support one or multiple active revisions at the same time to support A/B testing scenarios or canary deployments. A container app running in single revision mode will have a single revision that is backed by zero-many Pods/replicas.

1. **Containers**
Containers in the Azure Container Apps are grouped together in pods/replicas inside revision snapshots. A pod/replica is composed of the application container and any required sidecar containers. Containers can be deployed from any public or private container registry, and they support any Linux-based x86-64 (linux/amd64) images. At the time of creating this workshop Windows based images are not supported.
