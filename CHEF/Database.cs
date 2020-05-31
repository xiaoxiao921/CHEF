using System;
using Npgsql;

namespace CHEF
{
    internal static class Database
    {
        internal static void Init()
        {
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");

            var connectionString = $"Host={host};Username={username};Password={password};Database={dbName}";

            using (var con = new NpgsqlConnection(connectionString))
            {
                con.Open();
                var sql = "SELECT version()";

                using (var cmd = new NpgsqlCommand(sql, con))
                {
                    var version = cmd.ExecuteScalar().ToString();
                    Console.WriteLine($"PostgreSQL version: {version}");
                }
            }
        }
    }
}