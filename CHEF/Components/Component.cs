using System.Threading.Tasks;
using Discord.WebSocket;

namespace CHEF.Components
{
    /// <summary>
    /// A Component is a class that you want to call at start
    /// for either initializing stuff or registering events to the DiscordSocketClient object.
    /// </summary>
    public abstract class Component
    {
        protected readonly DiscordSocketClient Client;

        protected Component(DiscordSocketClient client)
        {
            Client = client;
        }

        /// <summary>
        /// This method should have the async keyword when being implemented
        /// </summary>
        /// <returns></returns>
        public abstract Task SetupAsync();
    }
}
