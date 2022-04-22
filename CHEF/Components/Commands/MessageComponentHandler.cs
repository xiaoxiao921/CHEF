using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CHEF.Components.Commands
{
    public abstract class MessageComponentHandler
    {
        protected readonly DiscordSocketClient Client;

        public abstract string CustomId { get; }

        protected MessageComponentHandler(DiscordSocketClient client)
        {
            Client = client;
        }

        public abstract Task Handle(SocketMessageComponent component);
    }

    public class MessageComponentDispatcher : Component
    {
        internal readonly Dictionary<string, MessageComponentHandler> Handlers = new();

        public MessageComponentDispatcher(DiscordSocketClient client) : base(client)
        {
            client.ButtonExecuted += OnButtonExecuted;
        }

        private async Task OnButtonExecuted(SocketMessageComponent component)
        {
            if (Handlers.TryGetValue(component.Data.CustomId, out var handler))
            {
                await handler.Handle(component);
            }
        }

        public override async Task SetupAsync()
        {
            await GatherAndRegisterHandlers();
        }

        private async Task GatherAndRegisterHandlers()
        {
            Handlers.Clear();

            var handlers = Assembly.GetExecutingAssembly().GetTypes().Where(IsHandler).ToList();

            Logger.Log($"{handlers.Count} message component handlers total.");

            foreach (var handler in handlers)
            {
                try
                {
                    var handlerInstance = (MessageComponentHandler)Activator.CreateInstance(handler, Client);

                    Handlers.Add(handlerInstance.CustomId, handlerInstance);
                    Logger.Log("Adding MessageComponentHandler : " + handlerInstance.CustomId);
                }
                catch (Exception e)
                {
                    Logger.Log($"Exception while trying to get the MessageComponentHandler data of {handler?.Name} {Environment.NewLine} {e}");
                }
            }
        }

        private static bool IsHandler(Type type) =>
            typeof(MessageComponentHandler).IsAssignableFrom(type) &&
            type != typeof(MessageComponentHandler);
    }
}
