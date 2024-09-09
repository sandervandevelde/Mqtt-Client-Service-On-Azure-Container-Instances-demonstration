# Stateful MQTT client service on Azure Container Instances demonstration

## Introduction

Demonstration of running a stateful MQTT client service within the Azure Container Instances.



## CLI commands and flow

If you want to play along, check the [az-cli-mqtt-stateful-service.md](az-cli-mqtt-stateful-service.md) file for building and deploying resouces and container instance.

Regarding the Azure Event Grid MQTT support, check the [az-cli-eventgrid-mqtt.md](az-cli-eventgrid-mqtt.md) for building all MQTT related resources. 



## Environment variables

The following (sample) environment variables are used:

```
eventHubNamespaceUri = 'acs-eventprocessor-service-ehns.servicebus.windows.net' 
consumerGroupName = 'aci'
eventHubName = 'messages'
blobStorageUri='https://acseventhubchckpntstor.blob.core.windows.net/messagesacicheckpoints'
```



## Credits

This demonstration is based on:

* the [Event Hub processor](https://learn.microsoft.com/en-us/azure/event-hubs/event-hubs-dotnet-standard-getstarted-send?tabs=passwordless%2Croles-azure-portal&WT.mc_id=AZ-MVP-5002324#update-the-code)
* the [first blog post introducing stateful procesing using the ACI](https://sandervandevelde.wordpress.com/2024/08/24/getting-started-with-azure-container-instances/)
* the [blog post introducing the EventHub Eventprocessor running in the ACI](https://sandervandevelde.wordpress.com/)


## MIT License

This application is made available under the MIT license. 
