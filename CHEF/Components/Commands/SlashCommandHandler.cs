using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace CHEF.Components.Commands
{
    public abstract class SlashCommand
    {
        protected readonly DiscordSocketClient Client;

        protected SlashCommand(DiscordSocketClient client)
        {
            Client = client;
        }

        public abstract bool IsGlobal { get; }

        public abstract SlashCommandBuilder Builder { get; }

        public abstract Task Handle(SocketSlashCommand interaction);
    }

    public class SlashCommandHandler : Component
    {
        internal readonly Dictionary<string, SlashCommand> GlobalSlashCommands = new ();

        public SlashCommandHandler(DiscordSocketClient client) : base(client)
        {
            client.InteractionCreated += OnSlashCommand;
        }

        private async Task OnSlashCommand(SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand slashCommandInteraction)
            {
                if (GlobalSlashCommands.TryGetValue(slashCommandInteraction.Data.Name, out var slashCommand))
                {
                    await slashCommand.Handle(slashCommandInteraction);
                }
            }
        }

        public override async Task SetupAsync()
        {
            await GatherAndRegisterSlashCommands();
        }

        private async Task GatherAndRegisterSlashCommands()
        {
            GlobalSlashCommands.Clear();

            var slashCommands = Assembly.GetExecutingAssembly().GetTypes().Where(SlashCommandsFilter).ToList();

            Logger.Log($"{slashCommands.Count} Slash Commands Total.");

            foreach (var slashCommandDataType in slashCommands)
            {
                try
                {
                    var slashCommandData = (SlashCommand)Activator.CreateInstance(slashCommandDataType, Client);

                    if (slashCommandData.IsGlobal)
                    {
                        GlobalSlashCommands.Add(slashCommandData.Builder.Name, slashCommandData);
                        Logger.Log("Adding slash command : " + slashCommandData.Builder.Name);
                        try
                        {
                            await Client.Rest.CreateGlobalCommand(slashCommandData.Builder.Build());
                        }
                        catch (ApplicationCommandException exception)
                        {
                            var json = JsonConvert.SerializeObject(exception.Error, Formatting.Indented);
                            Logger.Log(json);
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Log($"Exception while trying to get the slash command data of {slashCommandDataType.Name} {Environment.NewLine} {e}");
                }
            }
        }

        private static bool SlashCommandsFilter(Type type) =>
            typeof(SlashCommand).IsAssignableFrom(type) &&
            type != typeof(SlashCommand);
    }
}
