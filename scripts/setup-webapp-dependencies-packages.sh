#!/bin/sh
set -x -e

cwd=`dirname "$0"`
expr "$0" : "/.*" > /dev/null || cwd=`(cd "$cwd" && pwd)`

cd $cwd/../webapp/lib
if [ -d azure-documentdb-php-sdk ]
then
    echo "azure-documentdb-php-sdk exist, so skip package setup!!"
    exit 0
fi

git clone https://github.com/dreamfactorysoftware/azure-documentdb-php-sdk.git
cd azure-documentdb-php-sdk

curl http://getcomposer.org/composer.phar -s -o composer.phar
chmod +x composer.phar
php composer.phar install
rm composer.lock composer.phar
