using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace CHEF.Components.Commands.Cooking
{
    public class RecipeContext : DbContext
    {
        public DbSet<Recipe> Recipes { get; set; }
        public const int NumberPerPage = 5;

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

        public async Task<Recipe> GetRecipe(string recipeName) =>
            await Recipes.AsQueryable()
                .FirstOrDefaultAsync(r => r.Name.Equals(recipeName, System.StringComparison.InvariantCultureIgnoreCase));

        public async Task<(List<Recipe>, int)> GetRecipes(string nameFilter = null, int page = 0, SocketUser owner = null)
        {
            var query = Recipes.AsQueryable();
            if (nameFilter != null)
            {
                query = query.Where(r => r.Name.Contains(nameFilter, System.StringComparison.InvariantCultureIgnoreCase));
            }
            if (owner != null)
            {
                query = query.Where(r => r.OwnerId.Equals(owner.Id));
            }
            var totalNumberOfRecipes = await query.CountAsync();

            var recipes = await query.
                Skip(NumberPerPage * page).
                Take(NumberPerPage).
                OrderBy(r => r.Name).
                ToListAsync();

            return (recipes, totalNumberOfRecipes);
        }

        public Task<int> CountAll() => Recipes.AsQueryable().CountAsync();
    }

    public class Recipe
    {
        [Key]
        public int Id { get; set; }
        public ulong OwnerId { get; set; }
        public string OwnerName { get; set; }

        public string Name { get; set; }
        public string Text { get; set; }

        public string RealOwnerName(SocketGuild guild)
        {
            var owner = guild?.GetUser(OwnerId);
            return owner != null ? owner.ToString() : OwnerName;
        }

        public bool IsOwner(SocketGuildUser user) => OwnerId == user.Id;

        public bool CanEdit(SocketGuildUser user) =>
            IsOwner(user) ||
            user.Roles.Any(role => PermissionSystem.HasRequiredPermission(role, PermissionLevel.Elevated));
    }
}
