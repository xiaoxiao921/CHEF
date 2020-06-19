using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public class PermissionSystem : Component
    {
        public static Dictionary<SocketGuild, RolesPermissionLevel> GuildRolePermissions;

        public PermissionSystem(DiscordSocketClient client) : base(client)
        {
            GuildRolePermissions = new Dictionary<SocketGuild, RolesPermissionLevel>();
        }

        public override Task SetupAsync()
        {
            return Task.CompletedTask;
        }

        public static void UpdateCache(SocketGuild guild)
        {
            GuildRolePermissions.TryGetValue(guild, out var rolePermissions);

            if (rolePermissions == null)
            {
                rolePermissions = new RolesPermissionLevel(guild);

                GuildRolePermissions.Add(guild, rolePermissions);
            }
            else
            {
                var now = DateTime.Now;
                if ((now - rolePermissions.Timestamp).Hours >= 1)
                {
                    GuildRolePermissions[guild] = new RolesPermissionLevel(guild);
                }
            }
        }

        public static bool HasRequiredPermission(SocketRole role, PermissionLevel requiredLevel)
        {
            var roleLevel = PermissionLevel.None;

            GuildRolePermissions.TryGetValue(role.Guild, out var rolesPermissionLevel);
            rolesPermissionLevel?.Roles.TryGetValue(role, out roleLevel);

            return roleLevel >= requiredLevel;
        }

        public static SocketRole GetRefRole(SocketGuild guild, PermissionLevel level) =>
            GuildRolePermissions[guild].RefRoleToPermissionLevel[(int)level];

        public class RolesPermissionLevel
        {
            private static readonly int PermissionLevelCount = Enum.GetNames(typeof(PermissionLevel)).Length;

            public SocketRole[] RefRoleToPermissionLevel;
            public Dictionary<SocketRole, PermissionLevel> Roles { get; }
            public DateTime Timestamp { get; }

            public RolesPermissionLevel(SocketGuild guild)
            {
                Roles = new Dictionary<SocketRole, PermissionLevel>();
                Timestamp = DateTime.Now;

                DefineRolesPositionsToPermLevels(guild);

                foreach (var role in guild.Roles)
                {
                    Roles.Add(role, GetPermissionLevelFromRole(role));
                }
            }

            private void DefineRolesPositionsToPermLevels(SocketGuild guild)
            {
                var refRoles = new SocketRole[PermissionLevelCount];
                refRoles[0] = guild.EveryoneRole;
                foreach (var role in guild.Roles)
                {
                    //todo: remove DefinedRoles and make this configurable at runtime instead

                    if (role.Name.Equals(DefinedRoles.ModDeveloper))
                    {
                        refRoles[1] = role;
                        continue;
                    }

                    if (role.Name.Equals(DefinedRoles.CoreDeveloper))
                    {
                        refRoles[2] = role;
                    }
                }
                RefRoleToPermissionLevel = refRoles;
            }

            private PermissionLevel GetPermissionLevelFromRole(SocketRole role)
            {
                var level = PermissionLevel.None;

                for (var i = 0; i < RefRoleToPermissionLevel.Length; i++)
                {
                    // Higher position value = Higher on the role hierarchy

                    var refRole = RefRoleToPermissionLevel[i];
                    level = role.Position >= refRole.Position ? (PermissionLevel)i : level;
                }

                return level;
            }
        }
    }

    public enum PermissionLevel
    {
        None,
        ModDev,
        Elevated
    }

    public class RequireRoleAttribute : PreconditionAttribute
    {
        private readonly PermissionLevel _requiredLevel;

        public RequireRoleAttribute(PermissionLevel level) => _requiredLevel = level;

        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            if (context.User is SocketGuildUser gUser)
            {
                var guild = context.Guild as SocketGuild;
                PermissionSystem.UpdateCache(guild);

                return Task.FromResult(gUser.Roles.Any(r => PermissionSystem.HasRequiredPermission(r, _requiredLevel))
                    ? PreconditionResult.FromSuccess()
                    : PreconditionResult.FromError($"You must be atleast a {PermissionSystem.GetRefRole(guild, _requiredLevel).Name} to run this command."));
            }

            return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
        }
    }

    public static class DefinedRoles
    {
        public const string ModDeveloper = "mod developer";
        public const string CoreDeveloper = "core developer";
        public const string Moderator = "moderator";
    }
}
