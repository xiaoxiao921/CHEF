FROM mcr.microsoft.com/dotnet/core/runtime:2.1

COPY / App/
WORKDIR /App

RUN ./setup_puppeteer.sh

USER pptruser
ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-stable"

ENTRYPOINT ["dotnet", "CHEF.dll"]