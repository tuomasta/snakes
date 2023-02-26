# Snakes
This is my simple playground project.

It implements distributed snakes game. Snakes was chosen because implementing the game rules are simple, allowing me to focus on the tech.

The aim is to test out following technologies:
- F#
- SignalR
- Kubernetis (AKS), hosting
- Redis, signalr backplane
- Blazor, client (todo)
- Azure devops pipelines

## BUILD & RUN WITH DOCKER

### BUILD
````
docker build -f [client|server|executor]/Dockerfile -t talustuo/snakes-[client|server|executor] .
````

### NETWORK
````
docker network create snakes
````

### RUN
##### Redis
````
docker run --rm --name redis -p 6379:6379 --network snakes redislabs/redismod
````

##### Executor
````
docker run --rm --name snakes-executor -e "REDIS_URI=redis:6379" --network snakes talustuo/snakes-executor
````

##### SERVER
````
docker run --rm --name snakes-server -e "REDIS_URI=redis:6379" -p 5000:80 --network snakes talustuo/snakes-server
````

##### CLIENT
````
docker run --rm --name snakes-client -it --network snakes -e "SERVER_URI=http://snakes-server:80" talustuo/snakes-client
````

## K8S

#### DEPLOY
````
helm install snakes ./infra/chart
````

#### ATTACH TO CLIENT RUNNING IN K8S
````
kubectl get pods
kubectl attach -it client-dpt-<xxx>
````

## AKS INFRA

### AZ CLI
#### CREATE RG
````
RG_NAME=snakes
RG_ID=$(az group create --location northeurope --name $RG_NAME -o tsv --query id)
````

#### CREATE CLUSTER
````
az aks create -n aks-snakes -g $RG_NAME
--node-count 1
--node-vm-size Standard_A2_v2
--load-balancer-sku standard
--ssh-key-value ./snakes.pub
--enable-msi-auth-for-monitoring
--enable-addons monitoring
--enable-managed-identity
--output none

az aks get-credentials -n aks-snakes -g $RG_NAME --admin
````

#### INSTALL INGRESS
````
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm install -n ingress ingress-nginx ingress-nginx/ingress-nginx  --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz
````

#### SET DNS NAME FOR INGRESS
````bash
DNS_LABEL=snakes

helm upgrade -n ingress ingress-nginx ingress-nginx/ingress-nginx --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=$DNS_LABEL

curl http://snakes.northeurope.cloudapp.azure.com/api
````

### TERRAFORM

#### SETUP VARIABLES
Note that using ARM prefix makes them visible for terraform, so we don't need to pass them explicitely.

```bash
ARM_TENANT_ID=$(az account show --query tenantId -o tsv)
ARM_SUBSCRIPTION_ID=$(az account show --query id -o tsv)

## NOTE THIS CREATES NEW SP PASSWORD EVERYTIME!!
SERVICE_PRINCIPAL=$(az ad sp create-for-rbac --name terraform-ci -o json)
ARM_CLIENT_ID=$(echo $SERVICE_PRINCIPAL | jq -r '.appId')
ARM_CLIENT_SECRET=$(echo $SERVICE_PRINCIPAL | jq -r '.password')

TF_RG_NAME=tuomas-general
TF_STORAGE_NAME=tuomascommonstorage
```

#### UPDATING PROVIDERS
If you update providers then you need to update the terraform lock file used in CI pipelines. This can be done with following command. Read more about the from [here](https://developer.hashicorp.com/terraform/tutorials/automation/automate-terraform#plan-and-apply-on-different-machines) why this is needed.
```
terraform init -upgrade -backend-config="resource_group_name=$TF_RG_NAME" -backend-config="storage_account_name=$TF_STORAGE_NAME"
```

#### CREATE BLOB STORE FOR REMOTE TF STATE
```
az storage account create -g $TF_RG_NAME -n $TF_STORAGE_NAME --sku Standard_LRS
az storage container create --account-name $TF_STORAGE_NAME -n terraform-state
```

#### CREATE PRINCIPAL TO AUTH TERRAFORM
Make service principal a contributor for the subscription. This needs to be in subscription scope in order to create resource groups

````
az role assignment create --assignee $ARM_CLIENT_ID --scope "/subscriptions/$ARM_SUBSCRIPTION_ID" --role Contributor
````

#### CREATE AND EXECUTE THE PLAN
````
terraform init -backend-config="resource_group_name=$TF_RG_NAME" -backend-config="storage_account_name=$TF_STORAGE_NAME"

terraform plan

terraform apply
````

#### CLEAN UP
````
terraform plan -destroy
terraform destroy
````
<!-- 
##### ASSIGN ROLE IF IT DOES NOT EXITS
````</br>
``test $HAS_ROLE -lt 1 && az role assignment create --assignee $AKS_APP_ID --scope $RG_ID --role Contributor``
<!-- -->
<!-- 
## DEPLOY TO AKS
````
az aks get-credentials -n snakes-cluster -g snakes

kubectl apply -f .\k8s\redis.yaml
kubectl apply -f .\k8s\snakes-deployment.yaml
curl http://snakes.northeurope.cloudapp.azure.com/api
```` --> -->