using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace LoopMeet.Api.Tests.Infrastructure;

/// <summary>
/// Collects the Api's Serilog events. Program.cs uses UseSerilog without writeToProviders, so an
/// ILoggerProvider would see nothing; ReadFrom.Services picks up this sink instead.
/// </summary>
public sealed class CapturingLogSink : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _events = new();

    public IReadOnlyList<LogEvent> Events => _events.ToArray();

    public void Emit(LogEvent logEvent) => _events.Enqueue(logEvent);
}

/// <summary>
/// UseSerilog without preserveStaticLogger points every host's ILogger at the static Log.Logger,
/// which the most recently built host replaces. Classes that assert on captured events join this
/// collection so no other host is built while they run.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CapturedLogsCollection
{
    public const string Name = "Captured Serilog events";
}
