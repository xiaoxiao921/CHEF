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
    public interface ISlashCommand
    {
        public bool IsGlobal { get; }

        public SlashCommandBuilder Builder { get; }

        public Task Handle(SocketSlashCommand interaction);
    }

    public class SlashCommandHandler : Component
    {
        internal readonly Dictionary<string, ISlashCommand> GlobalSlashCommands = new ();

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

            foreach (var slashCommandData in slashCommands)
            {
                try
                {
                    var instance = (ISlashCommand)Activator.CreateInstance(slashCommandData, Client);

                    if (instance.IsGlobal)
                    {
                        GlobalSlashCommands.Add(instance.Builder.Name, instance);
                        Logger.Log("Adding slash command : " + instance.Builder.Name);
                        try
                        {
                            await Client.Rest.CreateGlobalCommand(instance.Builder.Build());
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
                    Logger.Log($"Exception while trying to get the slash command data of {slashCommandData.Name} {Environment.NewLine} {e}");
                }
            }
        }

        private static bool SlashCommandsFilter(Type type)
        {
            return type.IsSubclassOf(typeof(ISlashCommand));
        }
    }
}
