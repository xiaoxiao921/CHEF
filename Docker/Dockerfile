FROM mcr.microsoft.com/dotnet/core/runtime:2.1

RUN ls -la /
COPY publish/ App/
WORKDIR /App

ENTRYPOINT ["dotnet", "CHEF.dll"]
