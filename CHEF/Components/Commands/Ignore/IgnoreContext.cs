using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System.Linq;


namespace CHEF.Components.Commands.Ignore
{
    class IgnoreContext : DbContext 
    {
        public DbSet<Ignore> UserIds { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Dummy connection string for creating migration
            // through the Package Manager Console with Add-Migration or dotnet ef
            //optionsBuilder.UseNpgsql("Host=dummy;Username=dummy;Password=dummy;Database=dummy");

            optionsBuilder.UseNpgsql(global::CHEF.Database.Connection, builder =>
            {
#if RELEASE
                // callback for validating the server certificate against a CA certificate file.
                builder.RemoteCertificateValidationCallback(global::CHEF.Database.RemoteCertificateValidationCallback);
#endif
            });
            optionsBuilder.UseSnakeCaseNamingConvention();
        }

        public bool IsIgnored(SocketUser user)
        {
            return UserIds.AsQueryable().Any(i => i.discordId == user.Id);
        }
    }
}
