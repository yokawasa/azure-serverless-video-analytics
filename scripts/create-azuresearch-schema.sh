#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`
. $cwd/videoanalytics.conf

AZURE_SEARCH_SERVICE_NAME=$AzureSearchServiceName
AZURE_SEARCH_ADMIN_KEY=$AzureSearchAdminKey
AZURE_SEARCH_INDEX_PREFIX="caption"
AZURE_SEARCH_API_VER="2016-09-01"

APIURI="https://$AZURE_SEARCH_SERVICE_NAME.search.windows.net/indexes"
LANGS="en ja"

for LANG in $LANGS
do
    LOWERED_LANG=`echo $LANG | tr '[A-Z]' '[a-z]'`
    INDEX_NAME="$AZURE_SEARCH_INDEX_PREFIX-$LOWERED_LANG"
    echo "Start Creating Index: $INDEX_NAME ...."
    curl -s\
    -H "Content-Type: application/json"\
    -H "api-key: $AZURE_SEARCH_ADMIN_KEY"\
    -XPOST "$APIURI?api-version=$AZURE_SEARCH_API_VER" \
    -d"{
    \"name\": \"$INDEX_NAME\",
    \"fields\": [
        {\"name\":\"id\", \"type\":\"Edm.String\", \"key\":true, \"retrievable\":true, \"searchable\":false, \"filterable\":false, \"sortable\":false, \"facetable\":false},
        {\"name\":\"content_id\", \"type\":\"Edm.String\", \"retrievable\":true, \"searchable\":false, \"filterable\":true, \"sortable\":false, \"facetable\":false},
        {\"name\":\"begin_s\", \"type\":\"Edm.String\", \"retrievable\":true, \"searchable\":false, \"filterable\":false, \"sortable\":true, \"facetable\":false},
        {\"name\":\"end_s\", \"type\":\"Edm.String\", \"retrievable\":true, \"searchable\":false, \"filterable\":false, \"sortable\":true, \"facetable\":false},
        {\"name\":\"caption\", \"type\":\"Edm.String\", \"retrievable\":true, \"searchable\":true, \"filterable\":false, \"sortable\":false, \"facetable\":false, \"analyzer\":\"$LANG.lucene\"}
       ],
         \"corsOptions\": {
            \"allowedOrigins\": [\"*\"],
            \"maxAgeInSeconds\": 300
        }
    }"
done
