#!/bin/sh

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

az webapp config appsettings set \
  -n $WebUIAppName \
  -g $ResourceGroup \
  --settings \
    CosmosDBAccountName=$CosmosDBAccountName \
    CosmosDBAccountKey=$CosmosDBAccountKey \
    AzureSearchServiceName=$AzureSearchServiceName \
    AzureSearchAdminKey=$AzureSearchAdminKey \
    SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    VideoUploadEndpoint="https://$SourceStorageAccountName.blob.core.windows.net/$VideoUploadingContainerName"
