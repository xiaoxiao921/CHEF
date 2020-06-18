using System;
using System.Text;
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
            byte[] data = Convert.FromBase64String(Environment.GetEnvironmentVariable("GOOGLE_SERVICE_CREDENTIALS_B64",
                EnvironmentVariableTarget.Process));

            var builder = new ImageAnnotatorClientBuilder
            {
                CredentialsPath = null,
                JsonCredentials = Encoding.UTF8.GetString(data)
            };
            AnnotatorClient = await builder.BuildAsync();
        }
    }
}