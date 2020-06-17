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

## Database ORM

The app is made with ORM in mind, to make it easy, EntityFrameworkCore is being used with Npgsql.

To make migrations (and init your database/table the first time), you'll need to either :

- Use the `Add-Migration` command in your Package Manager Console in VS.

- Do it through dotnet ef.

Note that if all you get is an exception about the connectionString, make sure to setup a dummy one like i'm doing in the RecipeContext class when setting-up migrations.

What happens when doing migrations is that the Entity Framework except you to have a local database setup, 

which could not be the case, or could be stored in a docker container, that you don't have access to right now.

If everything goes correctly, this will create the first migration, you'll be able to see the new .cs files in the folder called Migrations.

For applying it in your database, you can do it at runtime like the Cook Component does :

```cs
using (var context = new RecipeContext())
{
    await context.Database.MigrateAsync();
}
```

The first time, this will effectively create the database and the table called recipes.

This will also ensure the database is in sync with what you have in code.

If any further changes are made to the table, you should simply do it by editing the fields of your class directly, and then simply create a new migration.

## Image Recognition through Yandex and Google Cloud Vision API for OCR

The bot requires a Google Cloud Account with the Vision API activated (you will need to have your billing information filled)

You will need to have a `json` file containing your credentials for the service account.

Please check those links for more information on how to setup this :

https://cloud.google.com/vision/docs/before-you-begin

https://cloud.google.com/vision/docs/libraries

Note that when first created, your account should be setup as a free account, the first 1k OCR API requests will be free of charge.

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

Make sure that a file called `ChefBot-ocr.json` also exists in the `Docker` folder.

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
