using System;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace CHEF.Components;
internal static class InteractionHandler
{
    /// <summary>
    /// Get next SocketMessageComponent (interaction) which matches the filter, if no predicate filter is provided return the next SocketMessageComponent
    /// </summary>
    /// <param name="client"></param>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<SocketMessageComponent> GetNextSocketMessageComponent(DiscordSocketClient client,
            Predicate<SocketMessageComponent> filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= m => true;

        var cancelSource = new TaskCompletionSource<bool>();
        var componentSource = new TaskCompletionSource<SocketMessageComponent>();
        var cancellationRegistration = cancellationToken.Register(() => cancelSource.SetResult(true));

        var componentTask = componentSource.Task;
        var cancelTask = cancelSource.Task;

        Task CheckComponent(SocketMessageComponent comp)
        {
            if (filter.Invoke(comp))
            {
                componentSource.SetResult(comp);
            }

            return Task.CompletedTask;
        }

        Task HandleInteraction(SocketInteraction arg)
        {
            if (arg is SocketMessageComponent comp)
            {
                return CheckComponent(comp);
            }

            return Task.CompletedTask;
        }

        try
        {
            client.InteractionCreated += HandleInteraction;

            var result = await Task.WhenAny(componentTask, cancelTask).ConfigureAwait(false);

            return result == componentTask
                ? await componentTask.ConfigureAwait(false)
                : null;
        }
        finally
        {
            client.InteractionCreated -= HandleInteraction;
            cancellationRegistration.Dispose();
        }
    }
}
