---
title: Prerequisites  
parent: Workshop Introduction
has_children: false
nav_order: 4
---

## Prerequisites

Make sure you have your development environment set up and configured.

1. An Azure account with an active subscription - [Create an account for free](https://azure.microsoft.com/free/?ref=microsoft.com&utm_source=microsoft.com&utm_medium=docs&utm_campaign=visualstudio){target=_blank}
1. dotnet 6.0 or a higher version - [Install](https://dotnet.microsoft.com/en-us/download){target=_blank}
1. Docker Desktop - [Install](https://docs.docker.com/desktop/install/windows-install/){target=_blank}
1. Visual Studio Code - [Install](https://code.visualstudio.com/){target=_blank}
1. VS Code Docker extension - [Install](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker){target=_blank}
1. Dapr CLI - [Install](https://docs.dapr.io/getting-started/install-dapr-cli/){target=_blank} and [Initialize](https://docs.dapr.io/getting-started/install-dapr-selfhost/){target=_blank}
1. VS Code Dapr extension. Depends on Dapr CLI - [Install](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-dapr){target=_blank}
1. Azure CLI - [Install](https://docs.microsoft.com/cli/azure/install-azure-cli){target=_blank}

## Workshop Instructions

The workshop is divided into separate modules. Each module will guide you through building the solution code step-by-step. Ensure that you finish the modules in the right order as they have dependencies on each other.

If you don't want to build the solution code from scratch, you can clone the source code repository final version by utilizing below command, and you can use the modules to deploy Azure resources using the provided Azure CLI commands. 

```shell
git clone https://github.com/Azure/aca-dotnet-workshop.git
```

!!! note
    In a production setting, it is advisable to fully automate the entire workflow by adhering to the guidelines provided in [module 10](../../aca/10-aca-iac-bicep/index.md), as opposed to relying on the manual procedures showcased in various modules within this workshop, which were primarily intended for educational purposes.

