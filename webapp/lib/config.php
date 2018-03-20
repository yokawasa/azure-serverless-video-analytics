<?php
function GET_CONFIG() {
    $c = array();
    $c['docdb_host']           = getenv('CosmosDBAccountName');
    $c['docdb_master_key']     = getenv('CosmosDBAccountKey');
    $c['docdb_db_meta']     = 'asset';
    $c['docdb_coll_meta']   = 'meta';
    $c['sas_token_api_endpoint']     = getenv('SasTokenAPIEndpoint');
    $c['video_upload_blob_endpoint']  = getenv('VideoUploadEndpoint');
    $c['azsearch_service_name'] = getenv('AzureSearchServiceName');
    $c['azsearch_api_key'] = getenv('AzureSearchAdminKey');
    return $c;
}
?>
