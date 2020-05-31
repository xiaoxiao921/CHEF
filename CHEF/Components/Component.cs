using System.Threading.Tasks;
using Discord.WebSocket;

namespace CHEF.Components
{
    public abstract class Component
    {
        protected readonly DiscordSocketClient Client;

        protected Component(DiscordSocketClient client)
        {
            Client = client;
        }

        public abstract Task SetupAsync();
    }
}
