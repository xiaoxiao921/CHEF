using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public class CommandHandler : Component
    {
        public static CommandService Service;

        public CommandHandler(DiscordSocketClient client) : base(client)
        {
            var config = new CommandServiceConfig { DefaultRunMode = RunMode.Async };
            Service = new CommandService(config);
        }

        public override async Task SetupAsync()
        {
            Service.CommandExecuted += OnCommandExecutedAsync;
            Service.Log += LogAsync;
            Client.MessageReceived += HandleCommandAsync;

            await Service.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private static async Task OnCommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason) && result.Error != CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync(result.ErrorReason);
            }
            
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            await LogAsync(new LogMessage(LogSeverity.Info,
                "CommandExecution",
                $"{commandName} was executed at {DateTime.UtcNow}."));
        }

        private async Task HandleCommandAsync(SocketMessage msg)
        {
            if (!(msg is SocketUserMessage message))
                return;

            var argPos = 0;
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(Client.CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            var context = new SocketCommandContext(Client, message);

            await Service.ExecuteAsync(context, argPos, null);
        }

        private static async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Exception is CommandException cmdException)
            {
                await cmdException.Context.Channel.SendMessageAsync("Something went catastrophically wrong! I'm a chef that don't know how to cook.");

                // We can also log this incident
                Logger.Log($"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in {cmdException.Context.Channel}.");
                Logger.Log(cmdException.ToString());
            }
        }
    }
}
