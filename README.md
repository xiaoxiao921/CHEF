# CHEF

A Discord bot made for the Risk of Rain 2 modding server.

## Install Instructions

Clone the repository:

```bash
$ git clone --recursive https://github.com/xiaoxiao921/CHEF.git
$ cd CHEF/CHEF
```

Edit `config.json` and insert your Discord token and PostgreSQL credentials:

```bash
$ cp config.json.template config.json
```

## Building for production (publishing):

```bash
# Current directory should be where the CHEF.csproj file is sitting
$ dotnet publish -c Release
```

This will put all the necessary dependencies on the same folder as the bot dll and make it ready for a Docker container.

### Docker

Once the app published, you can `cd` to that `publish/` folder , build an image from the dockerfile and run it.

```bash
$ cd bin/x64/Release/netcoreapp2.1/publish/
$ docker build -t chefbot -f Dockerfile .
$ docker run --detach chefbot
```