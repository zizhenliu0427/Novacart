using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Novacart.Api.Infrastructure.Threading;

public static class ThreadPoolTuningApplicator
{
    public static ThreadPoolTuningResult Apply(IConfiguration configuration, ILogger? logger = null)
    {
        var options = configuration.GetSection(ThreadPoolTuningOptions.SectionName)
            .Get<ThreadPoolTuningOptions>() ?? new ThreadPoolTuningOptions();

        if (!options.Enabled)
            return ThreadPoolTuningResult.Disabled;

        global::System.Threading.ThreadPool.GetMinThreads(out var currentWorkerMin, out var currentIoMin);
        global::System.Threading.ThreadPool.GetMaxThreads(out var maxWorker, out var maxIo);

        var targetWorker = options.MinWorkerThreads > 0
            ? Math.Min(options.MinWorkerThreads, maxWorker)
            : currentWorkerMin;

        var targetIo = options.MinCompletionPortThreads > 0
            ? Math.Min(options.MinCompletionPortThreads, maxIo)
            : currentIoMin;

        var changed = false;
        if (targetWorker != currentWorkerMin || targetIo != currentIoMin)
            changed = global::System.Threading.ThreadPool.SetMinThreads(targetWorker, targetIo);

        global::System.Threading.ThreadPool.GetMinThreads(out var appliedWorker, out var appliedIo);

        logger?.LogInformation(
            "Thread pool tuning Enabled={Enabled} MinWorker={Worker} MinIo={Io} Applied={Applied}",
            options.Enabled,
            appliedWorker,
            appliedIo,
            changed);

        return new ThreadPoolTuningResult(
            options.Enabled,
            appliedWorker,
            appliedIo,
            changed);
    }
}

public record ThreadPoolTuningResult(
    bool Enabled,
    int MinWorkerThreads = 0,
    int MinCompletionPortThreads = 0,
    bool Applied = false)
{
    public static ThreadPoolTuningResult Disabled { get; } = new(false);
}
