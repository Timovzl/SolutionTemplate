using Hangfire;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.JobRunner.Jobs;

/// <summary>
/// Do not inject this type directly. Instead, inject <see cref="IJob"/>.
/// </summary>
internal abstract class Job : IJob
{
	public abstract string CronSchedule { get; }

	[DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
	public abstract Task Execute(CancellationToken cancellationToken);
}
