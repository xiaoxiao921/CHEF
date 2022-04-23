#!/bin/bash

cd ..
dotnet publish CHEF.sln --configuration Debug --output ./publish --self-contained false
cd ./Docker
docker-compose build
docker-compose up