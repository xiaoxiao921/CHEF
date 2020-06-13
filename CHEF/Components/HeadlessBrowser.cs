using System.Threading.Tasks;
using Discord.WebSocket;
using PuppeteerSharp;

namespace CHEF.Components
{
    class HeadlessBrowser : Component
    {
        public static Browser Chromium { get; private set; }

        public HeadlessBrowser(DiscordSocketClient client) : base(client)
        {
        }

        public override async Task SetupAsync()
        {
            Chromium = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        }
    }
}
