<?php

$req=$_REQUEST;
$params = array();
if( is_array($req) ) {
    foreach( $req as $name => $value) {
        $params[$name] = $value;
    }
}

/////////////////////// 
// The endpoint is called when oupload operation has completed
///////////////////////

header('Content-Length: '.strlen($data));
header('Content-Type: application/json; odata.metadata=minimal');
header('Access-Control-Allow-Origin: *');
print "OK";


?>
