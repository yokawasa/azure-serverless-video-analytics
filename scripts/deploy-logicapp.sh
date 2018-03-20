#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
NAME=$LogicAppName
TEMPLATE_FILE=$cwd/../logicapp/LogicApp.json
PARAM_TEMPLATE_FILE=$cwd/../logicapp/LogicApp.parameters.json
PARAM_FILE=$cwd/../logicapp/_LogicApp.parameters.json

cp $PARAM_TEMPLATE_FILE $PARAM_FILE

# Set variables
perl -p -i -e "s,{logicAppName},$LogicAppName,g" $PARAM_FILE
perl -p -i -e "s,{functionsDeploymentResourceGroup},$RESOURCE_GROUP,g" $PARAM_FILE
perl -p -i -e "s,{functionsDeploymentName},$FunctionsAppName,g" $PARAM_FILE
perl -p -i -e "s,{sourceAssetsStorageAccountName},$SourceStorageAccountName,g" $PARAM_FILE
perl -p -i -e "s,{webhookSubscribeEndpoint},$WebhookSubscribeAPIEndpoint,g" $PARAM_FILE

## Comment these. These lines won't work as expected as built-in 'sed' in MacOS isn't GNU sed
#sed -i "s,{logicAppName},$LogicAppName,g" $PARAM_FILE
#sed -i "s,{functionsDeploymentResourceGroup},$RESOURCE_GROUP,g" $PARAM_FILE
#sed -i "s,{functionsDeploymentName},$FunctionsAppName,g" $PARAM_FILE
#sed -i "s,{sourceAssetsStorageAccountName},$SourceStorageAccountName,g" $PARAM_FILE
#sed -i "s,{uploadVideoWatchContainer},$VideoUploadingContainerName,g" $PARAM_FILE
#sed -i "s,{webhookSubscribeEndpoint},$WebhookSubscribeAPIEndpoint,g" $PARAM_FILE

az group deployment create --name $NAME \
     --resource-group $RESOURCE_GROUP \
     --template-file $TEMPLATE_FILE  \
     --parameters @$PARAM_FILE
