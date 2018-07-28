sudo apt install -y redis-server
sudo cp -rf ./RedisCore.Tests/redis.conf /etc/redis/redis.conf
sudo service redis-server restart