FROM mcr.microsoft.com/dotnet/core/runtime:2.1

RUN ls -lR /
COPY /CHEF/publish/ App/
WORKDIR /App

ENTRYPOINT ["dotnet", "CHEF.dll"]
