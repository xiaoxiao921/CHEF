using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Google.Cloud.Vision.V1;
using Npgsql;

namespace CHEF
{
    internal static class Database
    {
        internal static string Connection;

        internal static void Init()
        {
            var host = Environment.GetEnvironmentVariable("POSTGRES_HOST");
            var port = Environment.GetEnvironmentVariable("POSTGRES_PORT");
            var username = Environment.GetEnvironmentVariable("POSTGRES_USER");
            var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
            var dbName = Environment.GetEnvironmentVariable("POSTGRES_DB");

            Connection = $"Host={host};Port={port};Username={username};Password={password};Database={dbName};SSL Mode=Prefer";

            // Debug code for dropping table, careful with that
            //
            /*var conn = new NpgsqlConnection(Connection);
            conn.Open();
            using (var cmd = new NpgsqlCommand("DROP TABLE recipes", conn))
            {
                cmd.ExecuteNonQuery();
            }*/
        }

        // callback for validating the server certificate against a CA certificate file (here its base64 raw data equivalent).
        internal static bool RemoteCertificateValidationCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            Logger.Log("Validating server certificate against POSTGRES_SERVER_CA env var.");
            byte[] certData = Convert.FromBase64String(Environment.GetEnvironmentVariable("POSTGRES_SERVER_CA",
                EnvironmentVariableTarget.Process));

            var caCert = new X509Certificate2(certData);
            var caCertChain = new X509Chain
            {
                ChainPolicy = new X509ChainPolicy
                {
                    RevocationMode = X509RevocationMode.NoCheck,
                    RevocationFlag = X509RevocationFlag.EntireChain
                }
            };
            caCertChain.ChainPolicy.ExtraStore.Add(caCert);

            var serverCert = new X509Certificate2(certificate);

            caCertChain.Build(serverCert);
            if (caCertChain.ChainStatus.Length == 0)
            {
                Logger.Log("Validated.");
                return true;
            }

            foreach (X509ChainStatus status in caCertChain.ChainStatus)
            {
                // Check if we got any errors other than UntrustedRoot (which we will always get if we don't install the CA cert to the system store)
                if (status.Status != X509ChainStatusFlags.UntrustedRoot)
                {
                    Logger.Log("Error X509Chain: " + status.Status);
                    return false;
                }
            }

            return true;
        }
    }
}