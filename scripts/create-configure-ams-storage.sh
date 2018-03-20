#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$AMSStorageAccountName

# Create Azure Storage Account for Video Processing Pipeline and Blob Container in the account
az storage account create \
    --name $NAME \
    --resource-group $RESOURCE_GROUP \
    --sku Standard_LRS \
    --kind Storage

# Get Key
ACCESS_KEY=$(az storage account keys list --account-name $NAME --resource-group $RESOURCE_GROUP --output tsv |head -1 | awk '{print $3}')

echo "ACCESS_KEY => $ACCESS_KEY"
