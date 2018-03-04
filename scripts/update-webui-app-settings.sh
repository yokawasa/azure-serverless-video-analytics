#!/bin/sh

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videocoganalytics.conf

# App Settings
# az webapp config appsettings set -g <resource_group> -n <app_name> \
#   --settings KEY1=VALUE1 KEY2=VALUE2 KEY3=VALUE4

az webapp config appsettings set \
  -n $WebUIAppName \
  -g $ResourceGroup \
  --settings \
    CosmosDBAccountName=$CosmosDBAccountName \
    CosmosDBAccountKey=$CosmosDBAccountKey \
    AzureSearchServiceName=$AzureSearchServiceName \
    AzureSearchAdminKey=$AzureSearchAdminKey \
    SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    SasTokenAPIKey=$SasTokenAPIKey \
    VideoUploadEndpoint=$VideoUploadEndpoint
