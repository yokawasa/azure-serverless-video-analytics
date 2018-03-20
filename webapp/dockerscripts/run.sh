#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/../../scripts/videoanalytics.conf

version=`cat $cwd/../VERSION`
tag="$version"
docker run -v $cwd/..:/var/www/html --rm \
    -e CosmosDBAccountName=$CosmosDBAccountName \
    -e CosmosDBAccountKey=$CosmosDBAccountKey \
    -e SasTokenAPIEndpoint=$SasTokenAPIEndpoint \
    -e VideoUploadEndpoint="https://$SourceStorageAccountName.blob.core.windows.net/$VideoUploadingContainerName" \
    -e AzureSearchServiceName=$AzureSearchServiceName \
    -e AzureSearchAdminKey=$AzureSearchAdminKey \
    -p 8080:8080 -p 2222:2222 -it serverless-video-analytics:$tag
