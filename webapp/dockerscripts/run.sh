#!/bin/sh

#docker run --rm \
#    -e CosmosdbServiceHost="https://<CosmosDBAccount>.documents.azure.com:443/" \
#    -e CosmosdbMasterKey="<CosmosDBMasterKey>" \
#    -e AzureSearchServiceName="<AzureSearchAccountName>" \
#    -e AzureSearchApiKey="<AzureSearchAdminApiKey>" \
#    -p 8080:8080 -p 2222:2222  -it ai-digitalmedia-portal:0.1.0

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/../../scripts/videocoganalytics.conf

version=`cat $cwd/../VERSION`
tag="$version"
docker run -v $cwd/..:/var/www/html --rm \
    -e CosmosDBAccountName=$CosmosDBAccountName \
    -e CosmosDBAccountKey=$CosmosDBAccountKey \
    -e SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    -e SasTokenAPIKey=$SasTokenAPIKey \
    -e VideoUploadEndpoint=$VideoUploadEndpoint \
    -e AzureSearchServiceName=$AzureSearchServiceName \
    -e AzureSearchAdminKey=$AzureSearchAdminKey \
    -p 8080:8080 -p 2222:2222 -it video-cognitive-analytics:$tag
