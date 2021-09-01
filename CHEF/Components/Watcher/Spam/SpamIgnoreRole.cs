using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHEF.Components.Watcher.Spam
{
    public class SpamIgnoreRolesContext : DbContext
    {
        public DbSet<SpamIgnoreRole> SpamIgnoreRoles { get; set; }

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

        public async Task<SpamIgnoreRole> GetIgnore(SocketRole role)
        {
            return await SpamIgnoreRoles.AsQueryable().FirstOrDefaultAsync(el => role.Id == el.DiscordId);
        }

        public async Task<bool> IsIgnored(IEnumerable<SocketRole> roles)
        {
            var ids = roles.Select(el => el.Id).ToArray();
            return await SpamIgnoreRoles.AsQueryable().AnyAsync(el => ids.Contains(el.DiscordId));
        }
    }

    public class SpamIgnoreRole
    {
        [Key]
        public ulong DiscordId { get; set; }
    }
}
