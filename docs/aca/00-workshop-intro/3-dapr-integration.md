---
title: Dapr Integration in ACA  
parent: Workshop Introduction
has_children: false
nav_order: 3
canonical_url: 'https://bitoftech.net/2022/08/25/tutorial-building-microservice-applications-azure-container-apps-dapr/'
---

## Dapr Overview

As developers, we are often tasked with creating scalable, resilient, and distributed applications using microservices. But more often than not we face the same challenges:

- Recovering state after failures
- Services discovery and calling other microservices
- Integration with external resources
- Asynchronous communications between different services
- Distributed tracing
- Measuring message calls and performance across components and networked services

**Dapr (Distributed Application Runtime)** offers a solution for the common challenges that are faced in any distributed microservice application. Dapr can be used with any language (Go, .NET python, Node, Java, C++) and can run anywhere (On-premise, Kubernetes, and any public cloud (e.g. Azure)).

Dapr's core component is the concept of a [Building Block](https://docs.dapr.io/concepts/building-blocks-concept){target=_blank}. So far, Dapr supports nine Building Blocks. Simply put, a Building Block is a modular component which encapsulates best practices and can be accessed over standard HTTP or gRPC APIs.

Building Blocks address common challenges faced in building resilient microservices applications and implement best practices and patterns. Building Blocks provide consistent APIs and abstract the implementation details to keep your code simple and portable.

The diagram below shows the nine Building Blocks which expose public APIs that can be called from your code and can be configured using [components](https://docs.dapr.io/concepts/components-concept){target=_blank} to implement the building block's capability. Remember that you can pick whatever building block suites your distributed microservice application, and you can incorporate other building blocks as needed.

![Dapr Building Blocks](../../assets/images/00-workshop-intro/DaprBuildingBlocks.jpg)

## Dapr & Microservices

Dapr exposes its Building Blocks and components through a **sidecar architecture**. A sidecar enables Dapr to run in a separate memory process or separate container alongside your service. Sidecars provide isolation and encapsulation as they aren't part of the service, but connected to it. This separation enables each service to have its own runtime environment and be built upon different programming platforms.

![Dapr SideCar](../../assets/images/00-workshop-intro/ACA-Tutorial-DaprSidecar-s.jpg)

This pattern is named Sidecar because it resembles a sidecar attached to a motorcycle. In the previous figure, note how the Dapr sidecar is attached to your service to provide distributed application capabilities.

## Dapr usage in the workshop

We are going to enable Dapr for all Azure Container Apps in the solution. The Dapr APIs/Building Blocks used in this workshop are:

- **Service to Service invocation**: "ACA Web App-Frontend" microservice invokes the "ACA WebAPI-Backend" microservice using Dapr sidecar via the Service-to-service invocation building block
- **State Management**: "ACA WebAPI-Backend" stores data on Azure Cosmos DB and stores email logs on Azure Table Storage using Dapr State Management building blocks.
- **Pub/Sub**: "ACA WebAPI-Backend" publishes messages to Azure Service Bus when a task is saved and the "ACA Processor-Backend" microservices consumes those messages and sends emails using SendGrid.
- **Bindings**: "ACA Processor-Backend" is triggered based on an incoming event such as a Cron job.
