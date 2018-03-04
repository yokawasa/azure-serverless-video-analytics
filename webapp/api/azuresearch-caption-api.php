<?php
require_once dirname(__FILE__) . '/../lib/config.php';

$cfg = GET_CONFIG();
$azsearch_service_name=$cfg['azsearch_service_name'];
$azsearch_api_key=$cfg['azsearch_api_key'];
$azsearch_api_version = "2016-09-01";

$req=$_REQUEST;
$params = array();
if( is_array($req) ) {
    foreach( $req as $name => $value) {
        $params[$name] = $value;
    }
}
if (!array_key_exists('content_id', $params)) {
    print "Error!";
    exit(1);
}

$lang='en';
if (array_key_exists('lang', $params)) {
    $lang=$params['lang'];
} 

$AZURESEARCH_URL_BASE= sprintf( "https://%s.search.windows.net/indexes/caption-%s/docs",
                    $azsearch_service_name, strtolower($lang));

$url = $AZURESEARCH_URL_BASE . '?'
            . '$top=1000&$select=id,content_id,begin_s,end_s,caption'
            . '&$count=true&$orderby=begin_s&highlight=caption&api-version=' . $azsearch_api_version 
            . '&$filter=content_id%20eq%20%27' . $params['content_id'] . '%27';
if (array_key_exists('search', $params)) {
    $url = $url . '&search=' . urlencode($params['search']);
}

$opts = array(
   'http'=>array(
       'method'=>"GET",
       'header'=>"Accept: application/json\r\n" .
           "api-key: $azsearch_api_key\r\n",
       'timeout' =>10
   )
);

$context = stream_context_create($opts);
$data = file_get_contents($url, false, $context);

if ($data  === false) {
    print "Error!";
    exit(1);
}
else 
{
    header('Content-Length: '.strlen($data));
    header('Content-Type: application/json; odata.metadata=minimal');
    header('Access-Control-Allow-Origin: *');
    print $data;
}
?>
