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
$ git clone https://github.com/xiaoxiao921/CHEF.git
```

Create a fresh .env file:

```bash
$ cd CHEF/Docker
$ cp .env.template .env
```

Insert your Discord token and PostgreSQL credentials from this fresh .env file.

If you have followed the links above for setting up your Google Cloud Service, you now have with you a .json file containing your credentials.

The next step is to encode the content of this file with base64. 

The content will be decoded and used to connect to your service when doing OCR.

Insert the base64 string for the `GOOGLE_SERVICE_CREDENTIALS_B64` environment variable in the .env file.

Finally, `docker-compose up` to launch the chef container.

## Building the image yourself for production (publishing):

You can setup a github action and let all the building and publishing get handled by this workflow [github action](https://github.com/xiaoxiao921/CHEF/blob/master/.github/workflows/docker-image.yml)

You'll need to have two secrets in your repo settings, called `DOCKERHUB_USERNAME` and `DOCKERHUB_PASSWORD`.

A build and a docker image will be made and published to your Dockerhub account each time you create a new release in the Github repo.