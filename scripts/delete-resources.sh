#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

RESOURCE_GROUP=$ResourceGroup
RESOURCE_GROUP_LINUX_APP="$RESOURCE_GROUP-fe"

az group delete --name $RESOURCE_GROUP
az group delete --name $RESOURCE_GROUP_LINUX_APP
