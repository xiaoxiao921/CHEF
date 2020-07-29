using Discord.WebSocket;

namespace CHEF.Components.Commands.Ignore
{
    class Ignore
    {
        public ulong discordId;

        public override bool Equals(object obj)
        {
            if(obj is string @string)
            {
                return discordId.ToString() == @string;
            }if(obj is SocketUser user)
            {
                return discordId == user.Id;
            }
            return base.Equals(obj);
        }
    }
}
