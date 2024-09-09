# Creating Clients via commandline and EventGrid with MQTT support via AZ CLI 

## Prerequisites

Install this [TLS tool](https://smallstep.com/docs/step-cli/#introduction-to-step) 

Install this [MQTT client toolbox](https://mqttx.app/)

## Create client certficates

Generate all certificates needed for two devices, a publisher and a subscriber:

```
mkdir aci
cd aci

step ca init --deployment-type standalone --name AciTestCA --dns localhost --address 127.0.0.1:443 --provisioner AciTestCAProvisioner --context aci

// remember password

step context current

step certificate create client1-authnID client1-authnID.pem client1-authnID.key --ca ../.step/authorities/aci/certs/intermediate_ca.crt --ca-key ../.step/authorities/aci/secrets/intermediate_ca_key --no-password --insecure --not-after 72000h 

step certificate fingerprint client1-authnID.pem

// remember Client1 Thumbprint

step certificate create client2-authnID client2-authnID.pem client2-authnID.key --ca ../.step/authorities/aci/certs/intermediate_ca.crt --ca-key ../.step/authorities/aci/secrets/intermediate_ca_key --no-password --insecure --not-after 72000h 

step certificate fingerprint client2-authnID.pem

// remember Client2 Thumbprint
```


## Create Eventgrid with mqtt support

Create the Event Grid Name space with MQTT support in one go:

```
az eventgrid namespace create -g acsResourceGroup -n egns-aci-test-mqtt --topic-spaces-configuration "{state:Enabled}"
```



## Create Clients

Register two clients in the broker, one with a publisher attribute and one with a subscriber attribute:

```
az eventgrid namespace client create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n client1-authnID --authentication-name client1-authnID --client-certificate-authentication "{validationScheme:ThumbprintMatch,allowed-thumbprints:[Client1-Thumbprint]}" --attributes "{'ispublisher':1}"

az eventgrid namespace client create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n client2-authnID --authentication-name client2-authnID --client-certificate-authentication "{validationScheme:ThumbprintMatch,allowed-thumbprints:[Client2-Thumbprint]}" --attributes "{'issubscriber':1}"
```

*Note*: The thumbprint must be surrounded with square brackets and no space is needed between the ending square bracket and ending accolade. 



## Create Client groups

Create two client groups, one for the pubishers and one for the subscribers:

```
az eventgrid namespace client-group create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n publishersgroup --group-query "attributes.ispublisher=1"

az eventgrid namespace client-group create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n subscribersgroup --group-query "attributes.issubscriber=1"
```



## Topic space publisher

Create topic space and topic template for the publishers:

```
az eventgrid namespace topic-space create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n publishertopicspace --topic-templates 'acitest/+/alert'
```

Notice that the publisher can write messages in multiple topics where the '+' represents the client identifier.


## Topic space subscriber

Create topic space and topic template for the subscribers:

```
az eventgrid namespace topic-space create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n subscribertopicspace --topic-templates 'acitest/${client.authenticationName}/alert'
```

Notice that each subscriber can only read its own topic based on the 'client authentication name'.



## Permission binding publishers

Bring both the publisher client group and the publisher topic space together and mark it with publisher permissions:

```
az eventgrid namespace permission-binding create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n publisherpermissionbinding --client-group-name publishersgroup --permission publisher --topic-space-name publishertopicspace
```



## Permission binding subscribers

Bring both the subscriber client group and the subscriber topic space together and mark it with subscriber permissions:

```
az eventgrid namespace permission-binding create -g acsResourceGroup --namespace-name egns-aci-test-mqtt -n subscriberpermissionbinding --client-group-name subscribersgroup --permission subscriber --topic-space-name subscribertopicspace
```


## optional: Test publisher and subscriber in MQTTX

Notice that the endpoint of the MQTT broker looks like:

```
egns-aci-test-mqtt.westeurope-1.ts.eventgrid.azure.net

port: 8883
```

See also this [blog post](https://sandervandevelde.wordpress.com/2023/10/14/a-first-look-at-azure-eventgrid-mqtt-support/)