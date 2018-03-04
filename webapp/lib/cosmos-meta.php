<?php

require_once dirname(__FILE__) . '/azure-documentdb-php-sdk/vendor/autoload.php';
require_once dirname(__FILE__) . '/config.php';

class CosmosMetadata {
    private $_captions;

    private static function get_http_code($res) {
        if( is_array($res) and array_key_exists('_curl_info', $res)) {
            $curl_info=$res['_curl_info'];
            if ( array_key_exists('http_code', $curl_info) ) {
                return $curl_info['http_code'];
            }
        }
        return 500;
    }
    
    private static function is_success($res) {
        $http_code = self::get_http_code($res);
        if ( $http_code != 200 && $http_code !=201 ){
            return false;
        }
        return true;
    }

    public function __construct() {
        $cfg = GET_CONFIG();
        $client = new \DreamFactory\DocumentDb\Client(
                        sprintf("https://%s.documents.azure.com:443", $cfg['docdb_host']),
                        $cfg['docdb_master_key']);
        $this->doc = new \DreamFactory\DocumentDb\Resources\Document($client,
                            $cfg['docdb_db_meta'], $cfg['docdb_coll_meta']);

        $headers = array(
                'x-ms-max-item-count: 1000'
            );
        $this->doc->setHeaders($headers);
    }

    public function getList() {
        $res = $this->doc->query('SELECT * FROM c');
        if (!self::is_success($res)) {
            return array();
        }
        return $res['Documents'];
    }    

    public function get($content_id) {
        $res = $this->doc->query('SELECT * FROM c WHERE c.id = @id',
                [
                    ['name' => '@id', 'value' => $content_id]
                ]);
        $http_code = self::get_http_code($res);
        return (!self::is_success($res) ) ? array() : $res['Documents'][0];
    }

    public function insert($meta) {
        $res = $this->doc->create($meta);
        return self::is_success($res);
    }    

    public function update($mid, $meta) {
        $res = $this->doc->query('SELECT * FROM c WHERE c.id = @id', [['name' => '@id', 'value' => $mid]]);
        $http_code = self::get_http_code($res);
        if ($http_code != 200 ){
            return false;
        }
        $metas_arr = $res['Documents'];
        $m = $metas_arr[0];
        foreach( $m as $k => $v) {
            if (array_key_exists($k, $meta) && $v !=$meta[$k]){
                $m[$k] = $meta[$k];
            }
        }
        $res = $this->doc->replace($m, $mid);
        return self::is_success($res);
    }

}
