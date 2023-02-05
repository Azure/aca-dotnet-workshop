---
title: ACA Core Components Overview 
parent: Workshop Introduction
has_children: false
nav_order: 1
---

## Overview of Azure Container Apps Core Components

The main components of Azure Container Apps are:

![Azure Container Apps main components](/assets/images/00-workshop-intro/ACA-Tutorial-ACA-Components.jpg)

**1. Environments in Azure Container Apps**
The Environment is a secure boundary around several Container Apps, It contains at least one single container app or many, all container apps within an environment are deployed into a dedicated Azure Virtual Network, which makes it possible for these different container apps to communicate securely. As well all the logs produced from all container apps in the environment are sent to a dedicated Log Analytics workspace. and makes it possible for these different container apps to communicate, much like an App Service Environment when using Azure App Services

**2. Log Analytics workspace in Azure Container Apps**
Used to provide monitoring and observability functionality, each environment will have its own Log Analytic workspace and will be shared among all container apps within the environment.

**3. Container Apps in Azure Container Apps**
Each container App represents a single deployable unit that can contain one or more related containers (_More than one container is an advanced use case, for this workshop we will deploy a single container in each container app, more about multiple containers in the same single Azure Container App can be found [here](https://docs.microsoft.com/en-us/azure/container-apps/containers#multiple-containers)_

**4. Revisions in Azure Container Apps**
For each container app, you can create up to 100 revisions. Revisions are a way to deploy multiple versions of an app where you have the option to send the traffic to a certain revision. You can select if revision mode will support 1 active revision or multiple active revisions at the same time to support A/B testing scenarios or canary deployments.

**5. Containers in Azure Container Apps**
Containers in the Azure Container Apps are grouped together in pods inside revision snapshots, containers can be deployed from any public or private container registry, and they support any Linux-based x86-64 (linux/amd64) container image (Windows containers are not supported)