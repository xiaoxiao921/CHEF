#!/bin/bash

cd ..
dotnet publish CHEF.sln --configuration Debug --framework netcoreapp2.1 --output ./publish --self-contained false
cd ./Docker
docker-compose build
docker-compose up