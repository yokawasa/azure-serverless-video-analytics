#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`

RESOURCE_GROUP="RG-amstt-poc"
NAME="serverlessvideoanalytics"
TEMPLATE_FILE=$cwd/../logicapp/LogicApp.json
PARAM_FILE=$cwd/../logicapp/LogicApp.parameters.json

az group deployment create --name $NAME \
     --resource-group $RESOURCE_GROUP \
     --template-file $TEMPLATE_FILE  \
     --parameters @$PARAM_FILE
