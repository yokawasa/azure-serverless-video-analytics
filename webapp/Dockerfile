FROM php:7.0-apache
MAINTAINER Yoichi Kawasaki <yoichi.kawasaki@microsoft.com>

RUN apt-get update && \
    apt-get install -y vim ctags tmux curl && \
    apt-get clean

COPY apache2.conf /bin/
COPY init_container.sh /bin/
COPY . /var/www/html/

RUN a2enmod rewrite expires include

# install the PHP extensions we need
RUN apt update \
   && apt install -y --no-install-recommends \
       openssh-server \
   && rm -f /var/log/apache2/* \
   && rmdir /var/lock/apache2 \
   && rmdir /var/run/apache2 \
   && rmdir /var/log/apache2 \
   && chmod 777 /var/log \
   && chmod 777 /var/run \
   && chmod 777 /var/lock \
   && chmod 777 /bin/init_container.sh \
   && echo "root:Docker!" | chpasswd \
   && cp /bin/apache2.conf /etc/apache2/apache2.conf

COPY sshd_config /etc/ssh/

EXPOSE 8080 2222

ENV PORT 8080
ENV WEBSITE_ROLE_INSTANCE_ID localRoleInstance
ENV WEBSITE_INSTANCE_ID localInstance

WORKDIR /var/www/html
ENTRYPOINT ["/bin/init_container.sh"]
