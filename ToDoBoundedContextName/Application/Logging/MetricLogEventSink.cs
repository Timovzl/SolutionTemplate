using Prometheus;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.Logging;

/// <summary>
/// A Serilog <see cref="ILogEventSink"/> that exposes a metric based on the logged <see cref="LogLevel"/>.
/// </summary>
internal sealed class MetricLogEventSink : ILogEventSink
{
	private static readonly Counter LogCounter = Metrics.CreateCounter(name: "log_count", help: "Counts log entries per severity.", labelNames: "severity");

	static MetricLogEventSink()
	{
		// For each label, initialize the count to 0 to have the metric present, which helps prevent the first increase from being ignored
		LogCounter.WithLabels([nameof(LogLevel.Trace)]).Inc(0);
		LogCounter.WithLabels([nameof(LogLevel.Debug)]).Inc(0);
		LogCounter.WithLabels([nameof(LogLevel.Information)]).Inc(0);
		LogCounter.WithLabels([nameof(LogLevel.Warning)]).Inc(0);
		LogCounter.WithLabels([nameof(LogLevel.Error)]).Inc(0);
		LogCounter.WithLabels([nameof(LogLevel.Critical)]).Inc(0);
	}

	public void Emit(LogEvent logEvent)
	{
		LogCounter.WithLabels([logEvent.Level.ToString()]).Inc();
	}
}

public static class MetricLogEventSinkExtensions
{
	/// <summary>
	/// <para>
	/// Registers the <see cref="MetricLogEventSink"/>.
	/// </para>
	/// <para>
	/// Can be invoked by specifying a sink with the same name as this method in the configuration.
	/// </para>
	/// </summary>
	public static LoggerConfiguration Metrics(this LoggerSinkConfiguration sinkConfiguration)
	{
		return sinkConfiguration.Sink(new MetricLogEventSink(), LevelAlias.Minimum);
	}
}
