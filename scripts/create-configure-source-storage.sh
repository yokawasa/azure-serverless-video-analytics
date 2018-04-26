#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$SourceStorageAccountName

# Create Azure Storage Account for Video Processing Pipeline and Blob Container in the account
az storage account create \
    --name $NAME \
    --resource-group $RESOURCE_GROUP \
    --sku Standard_LRS \
    --kind Storage

# Get Key
ACCESS_KEY=$(az storage account keys list --account-name $NAME --resource-group $RESOURCE_GROUP --output tsv |head -1 | awk '{print $3}')
 
# Create Container
az storage container create  \
    --name "uploads" \
    --account-name $NAME \
    --account-key $ACCESS_KEY

# Set CORS for Blob
az storage cors add --services b \
    --methods PUT DELETE \
    --origins "*" \
    --allowed-headers "x-ms-meta-qqfilename","x-ms-blob-type","x-ms-blob-content-type","Content-Type" \
    --exposed-headers "x-ms-meta-*" \
    --max-age 200 \
    --account-name $NAME \
    --account-key $ACCESS_KEY
