using System;
using System.Threading;
using System.Threading.Tasks;

namespace CHEF.Extensions
{
    internal static class TaskExtensions
    {
        /// <summary>
        /// Returns true if the task finished in time, false if the task timed out.
        /// </summary>
        public static Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.WhenAny(task, Task.Delay(timeout, cancellationToken)).ContinueWith(resultTask =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (resultTask != task)
                {
                    return default;
                }

                return task.Result;
            }, cancellationToken);
        }
    }
}
