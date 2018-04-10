#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$WebUIAppName
APP_SERVICE_PLAN=$WebUIAppServicePlan
ZIPFILE=$cwd/../webapp/webapp.zip

## Setup PHP Library Package 
$cwd/setup-webapp-dependencies-packages.sh

## Create App Service Plan (If it's App Service Plan instead of Consumption Plan)
az appservice plan create \
 --name $APP_SERVICE_PLAN \
 --resource-group $RESOURCE_GROUP \
 --sku F1

# Create Web App for Container
az webapp create \
  --name $NAME \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN

## Zipping Webapp sources
cd $cwd/../webapp
zip -r $ZIPFILE .

az webapp deployment source config-zip \
    --name $NAME \
    --resource-group $RESOURCE_GROUP \
    --src $ZIPFILE

## Configure App Settings
az webapp config appsettings set \
  -n $NAME \
  -g $RESOURCE_GROUP \
  --settings \
    CosmosDBAccountName=$CosmosDBAccountName \
    CosmosDBAccountKey=$CosmosDBAccountKey \
    AzureSearchServiceName=$AzureSearchServiceName \
    AzureSearchAdminKey=$AzureSearchAdminKey \
    SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    VideoUploadEndpoint="https://$SourceStorageAccountName.blob.core.windows.net/$VideoUploadingContainerName"
