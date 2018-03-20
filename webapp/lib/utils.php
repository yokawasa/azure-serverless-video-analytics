<?php

function array_has_value_of( $key, $arr) {
    if( array_key_exists($key, $arr) ){
        $v = $arr[$key];
        if( isset($v) && !empty($v) ) {
            return true;
        }
    }
    return false;
}

function get_value($key, $arr, $default_value) {
    if (array_key_exists($key, $arr)) {
        return $arr[$key];
    }
    return $default_value;
}
 
function substr_yymmdd($s) {
    return (strlen($s)>8) ? substr($s, 0, 8) : $s;
}

function secure_protocol($url) {
    return str_replace("http://","https://", $url);
}

?>
