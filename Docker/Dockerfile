FROM mcr.microsoft.com/dotnet/runtime:6.0

COPY publish/ App/
WORKDIR /App

ENTRYPOINT ["dotnet", "CHEF.dll"]
