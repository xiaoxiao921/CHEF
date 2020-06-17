using System.Threading.Tasks;
using Discord.WebSocket;
using Google.Cloud.Vision.V1;

namespace CHEF.Components
{
    public class CloudVisionOcr : Component
    {
        public static ImageAnnotatorClient AnnotatorClient { get; private set; }

        public CloudVisionOcr(DiscordSocketClient client) : base(client)
        {
        }

        public override async Task SetupAsync()
        {
            AnnotatorClient = await ImageAnnotatorClient.CreateAsync();
        }
    }
}