using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.Jobs;

/// <summary>
/// <para>
/// This class annotates job runs with certain ambient information.
/// </para>
/// <para>
/// First, it adds an ambient "JobRunId" ILogger property for the duration of the job run, exposing a unique ID for the run.
/// Any entries logged by the job run have the ability to include the property.
/// </para>
/// <para>
/// Second, it exposes a "JobRuns" counter metric (scrapable via Prometheus), keeping track of when each job was run.
/// </para>
/// </summary>
internal class JobRunAnnotator
{
	/// <summary>
	/// For each running job, this stores the disposable object that adds the job run's ID to the LogContext.
	/// The values are indexed by the job run instance ID, i.e. the identifier of the firing of the job.
	/// </summary>
	private static ConditionalWeakTable<object, IDisposable> LogContextDisposablesByJobRunInstanceId { get; }
		= new ConditionalWeakTable<object, IDisposable>();

	private static Counter JobStartCounter { get; } = Metrics.CreateCounter("JobRuns", "Tracks the starting of jobs over time.", labelNames: new[] { "JobName" });

	public string Name => nameof(JobRunAnnotator);

	public ILogger<JobRunAnnotator>? Logger { get; }

	public JobRunAnnotator(ILogger<JobRunAnnotator>? logger)
	{
		this.Logger = logger;
	}

	public Task JobWillRun(string jobName, object jobRunInstanceId)
	{
		JobStartCounter.WithLabels(jobName).Inc();

		if (this.Logger is not null)
		{
			var jobRunId = $"{jobName}_{Guid.NewGuid().ToString("N")[16..]}";
			var logContextDisposable = this.Logger.BeginScope(new[] { KeyValuePair.Create("JobRunId", (object)jobRunId) });
			LogContextDisposablesByJobRunInstanceId.Add(jobRunInstanceId, logContextDisposable);
		}

		return Task.CompletedTask;
	}

	public Task JobDidRun(object jobRunInstanceId)
	{
		if (LogContextDisposablesByJobRunInstanceId.TryGetValue(jobRunInstanceId, out var logContextDisposable))
		{
			logContextDisposable.Dispose();
			LogContextDisposablesByJobRunInstanceId.Remove(jobRunInstanceId);
		}

		return Task.CompletedTask;
	}
}
