#!/bin/bash

mode=$1

echo "Running app in ${mode} mode"
docker compose -f ./docker-compose.${mode}.yml up -d
