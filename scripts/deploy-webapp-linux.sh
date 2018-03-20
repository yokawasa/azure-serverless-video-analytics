#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$WebUIAppName
APP_SERVICE_PLAN=$WebUIAppServicePlan
#CONTAINER_IMAGE=<REGISTRY_URL>/<CONTAINER_IMAGE:TAG>
CONTAINER_IMAGE=yoichikawasaki/serverless-video-analytics:0.0.1

## Create 
RESOURCE_GROUP_LINUX_APP="$RESOURCE_GROUP-fe"
az group create --name $RESOURCE_GROUP_LINUX_APP --location $ResourceLocation

## Create App Service Plan (If it's App Service Plan instead of Consumption Plan)
az appservice plan create \
 --name $APP_SERVICE_PLAN \
 --resource-group $RESOURCE_GROUP_LINUX_APP \
 --sku S1 --is-linux
### [NOTE] Plan with Linux worker can only be created in a group which has never contained a Windows worker, and vice versa.

# Create Web App for Container
az webapp create \
  --name $NAME \
  --resource-group $RESOURCE_GROUP_LINUX_APP \
  --plan $APP_SERVICE_PLAN \
  --deployment-container-image-name $CONTAINER_IMAGE

## Configure App Settings
az webapp config appsettings set \
  -n $NAME \
  -g $RESOURCE_GROUP_LINUX_APP \
  --settings \
    CosmosDBAccountName=$CosmosDBAccountName \
    CosmosDBAccountKey=$CosmosDBAccountKey \
    AzureSearchServiceName=$AzureSearchServiceName \
    AzureSearchAdminKey=$AzureSearchAdminKey \
    SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    VideoUploadEndpoint="https://$SourceStorageAccountName.blob.core.windows.net/$VideoUploadingContainerName"
