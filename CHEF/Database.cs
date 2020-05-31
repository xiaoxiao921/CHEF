using System;
using Npgsql;

namespace CHEF
{
    internal static class Database
    {
        internal static void Init()
        {
            /*var host = Config.Get<string>("DB_HOST");
            var username = Config.Get<string>("DB_USERNAME");
            var password = Config.Get<string>("DB_PASSWORD");
            var dbName = Config.Get<string>("DB_NAME");

            var connectionString = $"Host={host};Username={username};Password={password};Database={dbName}";

            using (var con = new NpgsqlConnection(connectionString))
            {
                var sql = "SELECT version()";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    var version = cmd.ExecuteScalar().ToString();
                    Console.WriteLine($"PostgreSQL version: {version}");
                }
            }*/
        }
    }
}