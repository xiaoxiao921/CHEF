using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CHEF.Components.Commands.Ignore
{
    public class IgnoreContext : DbContext
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

        public async Task<Ignore> GetIgnore(ulong discordId) => 
            await UserIds.AsQueryable()
                .FirstOrDefaultAsync(i => i.DiscordId == discordId);


        public async Task<bool> IsIgnored(SocketUser user) =>
            await UserIds.AsQueryable().AnyAsync(i => i.DiscordId == user.Id);
    }

    public class Ignore
    {
        [Key]
        public ulong DiscordId { get; set; }
    }
}
