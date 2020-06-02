# CHEF

A Discord bot made for the Risk of Rain 2 modding server.

Main feature of this bot is the possibility to have user commands created at runtime. Such commands are stored in a PostgreSQL Database.

The bot requires some bot permissions, here is the current list:

- Send Messages

- Embed Links

- Attach Files

- Add Reactions

The bot can print the availables commands with the `!help` command in a channel where the bot is available. 

It can also print the names of the recipes (the custom commands) that the users have written with `!c ls`

The bot was also made with Docker deployment in mind for easy setup on VPS and such.

## Install Instructions

Clone the repository:

```bash
$ git clone --recursive https://github.com/xiaoxiao921/CHEF.git
```

Insert your Discord token and PostgreSQL credentials from a fresh .env file.

```bash
$ cd CHEF/Docker
$ cp .env.template .env
```

## Building for production (publishing):

```bash
# Current directory should be where the CHEF.csproj file is sitting
$ dotnet publish -c Release
```

This will put all the necessary dependencies on the same folder as the bot dll and make it ready for a Docker container.

### Docker

Once the app is published, you can `cd` to that `publish/` folder and use the docker-composer file.

You may want to change the docker-compose file depending on your server setup, notably on your database and its used volume.

```bash
$ cd bin/x64/Release/netcoreapp2.1/publish/
$ docker-compose up
```
