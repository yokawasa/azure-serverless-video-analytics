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

# Set variables
#perl -p -i -e "s,{logicAppName},$LogicAppName,g" $PARAM_FILE
#perl -p -i -e "s,{functionsDeploymentResourceGroup},$RESOURCE_GROUP,g" $PARAM_FILE
#perl -p -i -e "s,{functionsDeploymentName},$FunctionsAppName,g" $PARAM_FILE
#perl -p -i -e "s,{sourceAssetsStorageAccountName},$SourceStorageAccountName,g" $PARAM_FILE
#perl -p -i -e "s,{webhookSubscribeEndpoint},$WebhookSubscribeAPIEndpoint,g" $PARAM_FILE

cat $PARAM_TEMPLATE_FILE | \
    perl -p -e "s,{logicAppName},$LogicAppName,g" | \
    perl -p -e "s,{functionsDeploymentResourceGroup},$RESOURCE_GROUP,g" | \
    perl -p -e "s,{functionsDeploymentName},$FunctionsAppName,g" | \
    perl -p -e "s,{sourceAssetsStorageAccountName},$SourceStorageAccountName,g" | \
    perl -p -e "s,{webhookSubscribeEndpoint},$WebhookSubscribeAPIEndpoint,g"  > $PARAM_FILE

az group deployment create --name $NAME \
     --resource-group $RESOURCE_GROUP \
     --template-file $TEMPLATE_FILE  \
     --parameters @$PARAM_FILE
