#!/usr/bin/env bash
set -e

if [ "$EUID" -ne 0 ]; then
  exec sudo bash "$0" "$@"
fi

apt update
apt install -y redis-server
cp -rf ./RedisCore.Tests/redis.conf /etc/redis/redis.conf
service redis-server restart
