FROM mcr.microsoft.com/dotnet/core/runtime:2.1

COPY /CHEF/publish/ App/
WORKDIR /App

ENTRYPOINT ["dotnet", "CHEF.dll"]