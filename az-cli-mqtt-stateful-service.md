# AZ CLI infrastructure setup for the MQTT stateful service

## Prerequisites

From the previous [blog post](https://sandervandevelde.wordpress.com/2024/08/24/getting-started-with-azure-container-instances/), we should have inherited a consumer group, Azure container repository and a UserID. 

The userID can pull containers from the registry.



## Build and push the Event Processor Service module 

Construct and push the demo container to the repository:

```
docker build --rm -f "Dockerfile.amd64" -t mycontainerregistrysvdv.azurecr.io/event-processor-service:0.0.1-amd64 "."

docker push mycontainerregistrysvdv.azurecr.io/event-processor-service:0.0.1-amd64
```



## Create an EventHub Namespace 

Create an Event Hub Namespace:

```
az eventhubs namespace create --name acs-eventprocessor-service-ehns --resource-group acsResourceGroup -l westeurope --sku Standard 
```

## Create an EventHub named 'messages'

Add an EventHub:

```
az eventhubs eventhub create --name messages --resource-group acsResourceGroup --namespace-name acs-eventprocessor-service-ehns --partition-count 4
```

## add a consumer group 'aci' to the eventhub

Add a consumer group:

```
az eventhubs eventhub consumer-group create --consumer-group-name aci --eventhub-name messages --namespace-name acs-eventprocessor-service-ehns --resource-group acsResourceGroup
```

### Get the id of the eventhub Namespace

Get the ID:

```
EHREGID=$(az eventhubs namespace show --resource-group acsResourceGroup --name acs-eventprocessor-service-ehns --query id --output tsv)

echo $EHREGID
```



## Create a Storage account for the EventHub checkpoints

Create an Azure storage:

```
az storage account create --name acseventhubchckpntstor --resource-group acsResourceGroup -l westeurope --sku Standard_LRS --kind StorageV2 --enable-hierarchical-namespace false
```

### Create a private blob container for checkpoints named 'messagesacicheckpoints'

Create a private blob container:

```
az storage container create -n messagesacicheckpoints --account-name acseventhubchckpntstor --fail-on-exist
```

### Get the id of the storage account

Get the ID:

```
STORREGID=$(az storage account show --name acseventhubchckpntstor --resource-group acsResourceGroup --query id --output tsv)

echo $STORREGID
```


## Give the container user instance roles 'Storage Blob Data Contributor' and 'Azure Event Hubs Data Contributor'

### Get the user id

Get the User id:

```
USERID=$(az identity show --resource-group acsResourceGroup --name myACRIdsvdv --query id --output tsv)

echo $USERID
```

### get the user principalid

Get the principal id of the user:

```
SPID=$(az identity show --resource-group acsResourceGroup --name myACRIdsvdv --query principalId --output tsv)

echo $SPID
```

### Assign the user the role 'Storage Blob Data Contributor' role for the storage account

Assign the user the 'Storage Blob Data Contributor' role:

```
az role assignment create --assignee $SPID --scope $STORREGID --role "Storage Blob Data Contributor"
```

### Assign the user the role 'Azure Event Hubs Data Owner' for the EventHub 

Assign the user the 'Azure Event Hubs Data Owner' role (see [source](https://github.com/Azure/azure-sdk-for-net/tree/Azure.Messaging.EventHubs.Processor_5.11.5/sdk/eventhub/Azure.Messaging.EventHubs.Processor))

```
az role assignment create --assignee $SPID --scope $EHREGID --role 'Azure Event Hubs Data Owner'
```



## Get the values for the environment variables

The following (sample) environment variables are used for our container instance:

```
eventHubNamespaceUri = 'acs-eventprocessor-service-ehns.servicebus.windows.net' 
consumerGroupName = 'aci'
eventHubName = 'messages'
blobStorageUri='https://acseventhubchckpntstor.blob.core.windows.net/messagesacicheckpoints'
brokerHostName='egns-aci-test-mqtt.westeurope-1.ts.eventgrid.azure.net'
brokerPort='8883'
deviceId='client1-authnID'
publishTopic='acitest/client2-authnID/alert'
```

Notice that the client certificates are part of the module and not configurable via the environment variables.


## Create ACI

Create the Azure Container Instance running the EventProcessor module:

```
az container create --name aci-test-eventprocessor --resource-group acsResourceGroup --image mycontainerregistrysvdv.azurecr.io/mqtt-stateful-service:0.0.1-amd64 --acr-identity $USERID --assign-identity $USERID --cpu 1 --memory 1 --os-type Linux --environment-variables eventHubNamespaceUri='acs-eventprocessor-service-ehns.servicebus.windows.net' consumerGroupName='aci' eventHubName='messages' blobStorageUri='https://acseventhubchckpntstor.blob.core.windows.net/messagesacicheckpoints' brokerHostName='egns-aci-test-mqtt.westeurope-1.ts.eventgrid.azure.net' brokerPort='8883' deviceId='client1-authnID' publishTopic='acitest/client2-authnID/alert' --sku Standard --run-as-group $USERID
```



## Remove current Azure Container Instance  

Remove the Azure Container Instance:

```
az container delete --resource-group acsResourceGroup --name aci-test-eventprocessor --yes
```
