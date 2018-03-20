#!/bin/sh

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

az webapp config appsettings set \
  -n $FunctionsAppName \
  -g $ResourceGroup \
  --settings \
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
    TextAnalyticsAPISubscriptionKey=$TextAnalyticsAPISubscriptionKey

