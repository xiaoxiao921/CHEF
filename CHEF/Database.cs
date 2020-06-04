using System;
using Npgsql;

namespace CHEF
{
    internal static class Database
    {
        internal static string Connection;

        internal static void Init()
        {
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");

            Connection = $"Host={host};Username={username};Password={password};Database={dbName}";

            // Debug code for dropping table, careful with that
            //
            /*var conn = new NpgsqlConnection(Connection);
            conn.Open();
            using (var cmd = new NpgsqlCommand("DROP TABLE recipes", conn))
            {
                cmd.ExecuteNonQuery();
            }*/
        }
    }
}