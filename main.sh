#!/bin/bash

$mode="$1"

echo "Running app in $1 mode"
docker compose up -f ./docker-compose.$1.yml -d
