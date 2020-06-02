using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace CHEF
{
    internal static class Database
    {
        private static string _connection;

        private const int Retry = 3;
        private const int TimeoutSec = 5;
        private const int TimeoutMs = TimeoutSec * 1000;

        internal static void Init()
        {
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");

            _connection = $"Host={host};Username={username};Password={password};Database={dbName}";

            HealthCheck();
        }

        private static void HealthCheck()
        {
            Logger.Log($"Trying to connect to the postgresql db. Retry timeout is set to {TimeoutSec} seconds.");
            //PrintResultSet(AsyncQuery("SELECT version()", null, true).Result);
            Logger.Log("Successfully queried the db.");
        }

        internal static async Task<int> AsyncNonQuery(string sql, Dictionary<string, string> values = null, bool exitOnFail = false)
        {
            int retry = Retry;
            do
            {
                try
                {
                    using (var con = new NpgsqlConnection(_connection))
                    {
                        con.Open();
                        using (var cmd = new NpgsqlCommand(sql, con))
                        {
                            if (values != null)
                            {
                                foreach (var (key, value) in values)
                                {
                                    cmd.Parameters.AddWithValue(key, value);
                                }
                            }

                            await cmd.PrepareAsync();
                            var reader = await cmd.ExecuteNonQueryAsync();
                            return reader;
                        }
                    }
                }
                catch (Exception e)
                {
                    retry -= 1;
                    if (retry > 0)
                    {
                        Logger.Log(e.ToString());
                        Logger.Log($"Caught Exception, remaining tries: {retry}. Retrying in {TimeoutSec} seconds.");
                        await Task.Delay(TimeoutMs);
                    }
                    else
                    {
                        if (exitOnFail)
                        {
                            Logger.Log("Exiting on fail.");
                            Environment.Exit(1);
                        }
                        else
                        {
                            Logger.Log("Failed to query the db, returning null and continuing code execution.");
                        }
                    }
                }
            } while (retry > 0);

            return 0;
        }

        internal static async Task<DataRowCollection> AsyncQuery(string sql, Dictionary<string, string> values = null, bool exitOnFail = false)
        {
            int retry = Retry;
            do
            {
                try
                {
                    using (var con = new NpgsqlConnection(_connection))
                    {
                        con.Open();
                        using (var cmd = new NpgsqlCommand(sql, con))
                        {
                            if (values != null)
                            {
                                foreach (var (key, value) in values)
                                {
                                    cmd.Parameters.AddWithValue(key, value);
                                }
                            }

                            await cmd.PrepareAsync();
                            var reader = await cmd.ExecuteReaderAsync();
                            var dt = new DataTable();
                            dt.Load(reader);
                            return dt.Rows;
                        }
                    }
                }
                catch (Exception e)
                {
                    retry -= 1;
                    if (retry > 0)
                    {
                        Logger.Log(e.ToString());
                        Logger.Log($"Caught Exception, remaining tries: {retry}. Retrying in {TimeoutSec} seconds.");
                        await Task.Delay(TimeoutMs);
                    }
                    else
                    {
                        if (exitOnFail)
                        {
                            Logger.Log("Exiting on fail.");
                            Environment.Exit(1);
                        }
                        else
                        {
                            Logger.Log("Failed to query the db, returning null and continuing code execution.");
                        }
                    }
                }
            } while (retry > 0);

            return null;
        }

        internal static void PrintResultSet(DataRowCollection rows)
        {
            Logger.Log(ResultSetToString(rows));
        }

        internal static string ResultSetToString(DataRowCollection rows)
        {
            var sb = new StringBuilder();
            foreach (DataRow row in rows)
            {
                foreach (var item in row.ItemArray)
                {
                    sb.AppendLine(item.ToString());
                }
            }

            return sb.ToString();
        }
    }
}