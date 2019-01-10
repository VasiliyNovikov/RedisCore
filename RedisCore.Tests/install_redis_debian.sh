#!/usr/bin/env bash
sudo service unattended-upgrades stop
sudo apt update
sudo apt install -y redis-server
sudo service unattended-upgrades start
sudo cp -rf ./RedisCore.Tests/redis.conf /etc/redis/redis.conf
sudo service redis-server restart
