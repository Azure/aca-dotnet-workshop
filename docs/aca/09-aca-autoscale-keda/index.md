---
title: Module 9 - ACA Auto Scaling with KEDA
has_children: false
nav_order: 9
canonical_url: 'https://bitoftech.net/2022/09/22/azure-container-apps-auto-scaling-with-keda-part-11/'
---
# Module 9 - ACA Auto Scaling with KEDA
In this module, we will explore how we can configure Auto Scaling rules in Container Apps. In my opinion, the **Auto Scaling** feature is one of the key features of any **Serverless** hosting platform, you want your application to respond dynamically based on the increased demand on workloads to maintain your system availability and performance.
Container Apps support Horizontal Scaling (**Scaling Out**) by adding more replicas (new instances of the Container App) and splitting the workload across multiple replicas to process the work in parallel. When the demand decrease, Container Apps will (**Scale In**) by removing the unutilized replicas according to your configured scaling rule. With this approach, you pay only for the replicas provisioned during the increased demand period, and you can as well configure the scaling rule to scale to **Zero** replicas, which means that no charges are incurred when your Container App scales to zero.

Azure Container Apps supports different scaling triggers as the below:

* [HTTP traffic](https://learn.microsoft.com/en-us/azure/container-apps/scale-app#http): Scaling based on the number of concurrent HTTP requests to your revision.
* [CPU](https://learn.microsoft.com/en-us/azure/container-apps/scale-app#cpu) or [Memory](https://learn.microsoft.com/en-us/azure/container-apps/scale-app#memory) usage: Scaling based on the amount of CPU utilized or memory consumed by a replica.
* Azure Storage Queues: Scaling based on the number of messages in Azure Storage Queue.
* Event-driven using [KEDA](https://keda.sh/): Scaling based on events triggers, such as the number of messages in Azure Service Bus Topic or the number of blobs in Azure Blob Storage container.

As we covered in the intro module, Azure Container Apps utilize different open source technologies, KEDA is one of them to enable event-driven autoscaling, which means that KEDA is installed by default when you provision your Container App, we should not worry about installing it. All we need to focus on is enabling and configuring our Container App scaling rules.

In this module, we will be focusing on event-driven autoscaling using KEDA.

### An overview of KEDA
KEDA stands for Kubernetes Event-Driven Autoscaler. It is an open-source project initially started by [Microsoft and Red Hat](https://cloudblogs.microsoft.com/opensource/2019/05/06/announcing-keda-kubernetes-event-driven-autoscaling-containers/) to allow any Kubernetes workload to benefit from the event-driven architecture model. Prior to KEDA, horizontally scaling Kubernetes deployment was achieved through the Horizontal Pod Autoscaler ([HPA](https://kubernetes.io/docs/tasks/run-application/horizontal-pod-autoscale/)). The HPA relies on resource metrics such as Memory and CPU to determine when additional replicas should be deployed. Within any enterprise application, there will be other external metrics we want to scale out our application based on, think of Kafka topic log, length of an Azure Service Bus Queue, or metrics obtained from a Prometheus query. KEDA offers more than [50 scalers](https://keda.sh/docs/2.8/scalers/) to pick from based on your business need. KEDA exists to fill this gap and provides a framework for scaling based on events in conjunction with HPA scaling based on CPU and Memory.

### Configure Scaling Rule in Backend Background Processor Project
We need to configure our Backend Background Processor named `tasksmanager-backend-processor` service to scale out and increase the number of replicas based on the number of messages in the Topic named `tasksavedtopic`. If our service is under a huge workload and our single replica is not able to cope with the number of messages on the topic, we need the Container App to spin up more replicas to parallelize the processing of messages on this topic.

So our requirements for scaling the backend processor are as follows:

* For every 10 messages on the Azure Service Bus Topic, scale-out by one replica.
* When there are no messages on the topic, scale-in to a Zero replica.
* The maximum number of replicas should not exceed 5.

To achieve this, we will start looking into KEDA Azure Service Bus scaler, This specification describes the `azure-servicebus` trigger for Azure Service Bus Queue or Topic, let's take a look at the below yaml file which contains a template for the KEDA specification:
```yaml
triggers:
- type: azure-servicebus
  metadata:
    # Required: queueName OR topicName and subscriptionName
    queueName: functions-sbqueue
    # or
    topicName: functions-sbtopic
    subscriptionName: sbtopic-sub1
    # Optional, required when pod identity is used
    namespace: service-bus-namespace
    # Optional, can use TriggerAuthentication as well
    connectionFromEnv: SERVICEBUS_CONNECTIONSTRING_ENV_NAME # This must be a connection string for a queue itself, and not a namespace level (e.g. RootAccessPolicy) connection string 
    # Optional
    messageCount: "5" # Optional. Count of messages to trigger scaling on. Default: 5 messages
    cloud: Private # Optional. Default: AzurePublicCloud
    endpointSuffix: servicebus.airgap.example # Required when cloud=Private
```

Let's review the parameters:

* The property `type` is set to `azure-servicebus`, each KEDA scaler specification file has a unique type.
* One of the properties `queueName` or `topicName` should be provided, in our case, it will be `topicName` and we will use the value `tasksavedtopic`.
* The property `subscriptionName` will be set to use `tasksmanager-backend-processor` This represents the subscription associated with the topic. Not needed if we are using queues.
* The property `connectionFromEnv` will be set to reference a secret stored in our Container App, we will not use the Azure Service Bus shared access policy (connection string) directly, the shared access policy will be stored in the Container App secrets, and the secret will be referenced here. Please note that the Service Bus Shared Access Policy needs to be of type `Manage`. It is required for KEDA to be able to get metrics from Service Bus and read the length of messages in the queue or topic.
* The property `messageCount` is used to decide when scaling out should be triggered, in our case, it will be set to `10`.
* The property `cloud` represents the name of the cloud environment that the service bus belongs to.

{: .note }
Note about authentication: KEDA scaler for Azure Service Bus supports different authentication mechanisms such as [Pod Managed Identity](https://learn.microsoft.com/en-us/azure/aks/use-azure-ad-pod-identity), [Azure AD Workload Identity](https://azure.github.io/azure-workload-identity/docs/), and shared access policy (connection string). When using KEDA with Azure Container Apps, at the time of writing this module, the only supported authentication mechanism is Connection Strings. on ACA roadmap, there is a work item on ACA product backlog to enable [KEDA Scale with Managed Identity.](https://github.com/microsoft/azure-container-apps/issues/592)

Azure Container Apps has its own proprietary schema to map KEDA Scaler template to its own when defining a custom scale rule, you can define this scaling rule via Container Apps [ARM templates](https://learn.microsoft.com/en-us/azure/container-apps/azure-resource-manager-api-spec?tabs=arm-template#container-app-examples), [yaml manifest](https://learn.microsoft.com/en-us/azure/container-apps/azure-resource-manager-api-spec?tabs=arm-template#container-app-examples), Azure CLI, or from Azure Portal. In this module, we will cover how to do it from the Azure CLI.

##### 1. Create a new secret in Container App

Let's now create a secret named `svcbus-connstring` in our Container App named `tasksmanager-backend-processor`, this secret will contain the value of Azure Service Bus shared access policy (connection string) with `Manage` policy. To do so, run the below commands in Azure CLI to get the connection string, and then add this secret using the second command:

```powershell
##List Service Bus Access Policy RootManageSharedAccessKey
$NamespaceName = "<Use your Azure Service Bus name space>"
az servicebus namespace authorization-rule keys list `
--resource-group $RESOURCE_GROUP `
--namespace-name $NamespaceName `
--name RootManageSharedAccessKey `
--query primaryConnectionString `
--output tsv

##Create a new secret named 'svcbus-connstring' in backend processer container app
az containerapp secret set `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--secrets "svcbus-connstring=<Connection String from Service Bus>"
```

##### 2. Create a Custom Scaling Rule from Azure CLI
Now we are ready to add a new custom scaling rule to match the business requirements, to do so we need to run the below Azure CLI command:

{: .note }
I had to update `az containerapp`  extension in order to create a scaling rule from CLI, to update it you can run the following command `az extension update --name containerapp`

```powershell
az containerapp update `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--min-replicas 0 `
--max-replicas 5 `
--revision-suffix v20230227-3 `
--set-env-vars "SendGrid__IntegrationEnabled=false" `
--scale-rule-name "topic-msgs-length" `
--scale-rule-type "azure-servicebus" `
--scale-rule-auth "connection=svcbus-connstring" `
--scale-rule-metadata "topicName=<Your topic name>" `
                        "subscriptionName=<Your topic subscription name>" `
                        "namespace=<Your service bus namespace>" `
                        "messageCount=10" `
                        "connectionFromEnv=svcbus-connstring"
```

What we have done is the following:

* Setting the minimum number of replicas to Zero, means that this Container App could be scaled-in to Zero replicas if there are no new messages on the topic.
* Setting the maximum number of replicas to 5, means that this Container App will not exceed more than 5 replicas regardless of the number of messages on the topic.
* Setting a friendly name for the scale rule `topic-msgs-length` which will be visible in Azure Portal.
* Setting the scale rule type to `azure-servicebus`, this is important to tell KEDA which type of scalers our Container App is configuring.
* Setting the authentication mechanism to type `connection` and indicating which secret reference will be used, in our case `svcbus-connstring`.
* Setting the `metadata` dictionary of the scale rule, those matching the metadata properties in KEDA template we discussed earlier.
* Disabled the integration with SendGrid as we are going to send load of messages now to test the scale out rule.

Once you run this command the custom scale rule will be created, we can navigate to the Azure Portal and see the details.

##### 3. Run an end-to-end test and generate a load of messages
Now we are ready to test out our Azure Service Bus Scaling Rule, to generate a load of messages we can do this from Service Bus Explorer under our Azure Service Bus namespace, so navigate to Azure Service Bus, select your topic/subscription, and then select `Service Bus Explorer`.

To get the number of current replicas of service `tasksmanager-backend-processor ` we could run the command below, this should run single replica as we didn't load the service bus topic yet.

```powershell
az containerapp replica list `
--name $BACKEND_SVC_NAME `
--resource-group $RESOURCE_GROUP `
--query [].name
```
The message structure our backend processor expects is as JSON below, so copy this message and click on Send messages button, paste the message content, set the content type to `application/json`, check the `Repeat Send` check box, select `500` messages and put an interval of `5ms` between them, click `Send` when you are ready.

```json
{
    "data": {
        "isCompleted": false,
        "isOverDue": true,
        "taskAssignedTo": "temp@mail.com",
        "taskCreatedBy": "someone@mail.com",
        "taskCreatedOn": "2022-08-18T12:45:22.0984036Z",
        "taskDueDate": "2023-02-24T12:45:22.0983978Z",
        "taskId": "6a051aeb-f567-40dd-a434-39927f2b93c5",
        "taskName": "Auto scale Task"
    }
}
```
![svcbus-send](../../assets/images/09-aca-autoscale-keda/svs-bus-send.jpg)

##### 4. Verify that multiple replicas are created

If all is setup correctly, 5 replicas will be created based on the number of messages we generated into the topic, there are various ways to verify this:
- You can run the Azure CLI command used in [previous step](#3-run-an-end-to-end-test-and-generate-a-load-of-messages) to list the names of replicas.
- You can verify this from Container Apps “Console” tab you will see those replicas in the drop-down list
![replica-console](../../assets/images/09-aca-autoscale-keda/replica-console.png)

**Note about KEDA Scale In:**
Container Apps implements the [KEDA ScaledObject](https://keda.sh/docs/2.8/concepts/scaling-deployments/#scaledobject-spec) with the following default settings:

* pollingInterval: 30 seconds. This is the interval to check each trigger on. By default, KEDA will check each trigger source on every ScaledObject every 30 seconds.
* cooldownPeriod: 300 seconds. The period to wait after the last trigger is reported active before scaling in the resource back to 0. By default, it’s 5 minutes (300 seconds).
Currently, there is no way to override this value, yet there is an [open issue](https://github.com/microsoft/azure-container-apps/issues/388) on the Container Apps repo and the PG is tracking it, 5 minutes might be a long period to wait for instances to be scaled in after they finish processing messages.