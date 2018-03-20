<?php

// Proxy Server for signature 
// this is proxy to sas generate function
// Target function - https://azure.microsoft.com/ja-jp/resources/samples/functions-dotnet-sas-token/
// Implimentaiton is based on the following guide
// https://docs.fineuploader.com/branch/master/endpoint_handlers/azure.html

require_once dirname(__FILE__) . '/../lib/config.php';

$cfg = GET_CONFIG();

$GET_SAS_TOKEN_API_ENDPOINT = $cfg['sas_token_api_endpoint'];
$sasurlarr=parse_url($GET_SAS_TOKEN_API_ENDPOINT);
$sasparams=array();
parse_str($sasurlarr['query'],$sasparams);
$GET_SAS_TOKEN_API_HOST_PATH= sprintf("%s://%s%s", $sasurlarr['scheme'], $sasurlarr['host'],$sasurlarr['path']);
$GET_SAS_TOKEN_API_KEY=$sasparams['code'];

$req=$_REQUEST;
$params = array();
if( is_array($req) ) {
    foreach( $req as $name => $value) {
        $params[$name] = $value;
    }
}
// request parameters
// _method
// bloburi
// qqtimestamp  (not need to verify)
if ( !array_key_exists('_method', $params)
      || !array_key_exists('bloburi', $params)) {
    print "Error!";
    http_response_code( 403 );
    exit;
}
$permissions = ($params['_method'] == "PUT" ) ? "Read,Write,Create" : "Read";
//$permissions = ($params['_method'] == "PUT" ) ? "Write" : "Read";
$bloburi_urldecoded=urldecode($params['bloburi']);
$uri_info = parse_url( $bloburi_urldecoded);
$container = '';
$blob = '';
$container_plus_blob = ( array_key_exists('path', $uri_info) && strlen($uri_info['path']) > 1) ? substr($uri_info['path'],1) : '';
if (!empty($container_plus_blob)) {
    $cb_arr = preg_split('/\//', $container_plus_blob);
    if (count($cb_arr) > 1 ) {
        $container = $cb_arr[0];
        $blob = substr($container_plus_blob, strlen($container) + 1);
    }
}
if (empty($container) || empty($blob)) {
    print "Invalid bloburi:" . $params['bloburi'];
    http_response_code( 403 );
    exit;
}

$url = $GET_SAS_TOKEN_API_HOST_PATH;

$body_arr = array(
    "container" => $container,
    "blobName" => $blob,
    "permissions" => $permissions
);

$header = array(
    "Content-Type: application/json; charset=UTF-8",
    "x-functions-key: $GET_SAS_TOKEN_API_KEY"
);
$context = array(
    "http" => array(
        "method"  => "POST",
        "header"  => implode("\r\n", $header),
        "content" => json_encode($body_arr)
    )
);

$res_data = file_get_contents($url, false, stream_context_create($context));

if ($res_data  === false) {
    print "Error!";
}
else
{
    $res_arr = (array)json_decode($res_data);
    $token = $res_arr['token'];
    $ret_bloburl = sprintf("%s%s", $bloburi_urldecoded, $token);
    header('Content-Length: ' + (string)strlen($ret_bloburl));
    header('Content-Type: plain/text');
    header('Access-Control-Allow-Origin: *');
    print $ret_bloburl;
}
?>
