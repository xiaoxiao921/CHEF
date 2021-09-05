using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CHEF.Components.Watcher.Spam
{
    public class SpamIgnoreRole
    {
        [Key]
        public ulong DiscordId { get; set; }
    }
}
