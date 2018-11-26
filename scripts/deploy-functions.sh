#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$FunctionsAppName
ZIPFILE=$cwd/../functions/functions.zip
FUNCTIONS_APP_SERVICE_PLAN=$FunctionsAppName

## Create App Service Plan (If it's App Service Plan instead of Consumption Plan)
# az appservice plan create \
#  --name $FUNCTIONS_APP_SERVICE_PLAN \
#  --resource-group $RESOURCE_GROUP

## Create Storage Account (If not yet created)
#az storage account create \
#  --name $SourceStorageAccountName \
#  --resource-group $RESOURCE_GROUP \
#  --sku Standard_LRS

## Create Functions App
#az functionapp create --name $NAME \
#    --resource-group $RESOURCE_GROUP \
#    --plan $FUNCTIONS_APP_SERVICE_PLAN \
#    --storage-account $SourceStorageAccountName

## Create Functions App (Consumption Plan)
az functionapp create --name $NAME \
    --resource-group $RESOURCE_GROUP \
    --consumption-plan-location $FunctionsAppConsumptionPlanLocation \
    --storage-account $SourceStorageAccountName
### [NOTE] Use 'az functionapp list-consumption-locations' to view available locations


## Configure App Settings
az webapp config appsettings set \
  -n $NAME \
  -g $RESOURCE_GROUP \
  --settings \
    FUNCTIONS_EXTENSION_VERSION="~1" \
    AMSAADTenantDomain=$AMSAADTenantDomain \
    AMSRESTAPIEndpoint=$AMSRESTAPIEndpoint \
    AMSClientId=$AMSClientId \
    AMSClientSecret=$AMSClientSecret \
    AMSStorageAccountName=$AMSStorageAccountName \
    AMSStorageAccountKey=$AMSStorageAccountKey \
    SourceStorageConnection="DefaultEndpointsProtocol=https;AccountName=$SourceStorageAccountName;AccountKey=$SourceStorageAccountKey;EndpointSuffix=core.windows.net" \
    CosmosDB_Connection="AccountEndpoint=https://$CosmosDBAccountName.documents.azure.com:443/;AccountKey=$CosmosDBAccountKey;" \
    CosmosDBAccountName=$CosmosDBAccountName \
    CosmosDBAccountKey=$CosmosDBAccountKey \
    SourceStorageAccountName=$SourceStorageAccountName \
    SourceStorageAccountKey=$SourceStorageAccountKey \
    SasTokenTTLHours=1 \
    AMSSkipMBREncoding=0 \
    TranslatorAPIKey=$TranslatorAPIKey \
    AzureSearchServiceName=$AzureSearchServiceName \
    AzureSearchAdminKey=$AzureSearchAdminKey \
    TextAnalyticsAPISubscriptionKey=$TextAnalyticsAPISubscriptionKey \
    TextAnalyticsAPILocation=$TextAnalyticsAPILocation

## Zipping functions
cd $cwd/../functions
zip -r $ZIPFILE .

## Deploying functions
az functionapp deployment source config-zip  --name $NAME \
    --resource-group $RESOURCE_GROUP \
    --src $ZIPFILE
