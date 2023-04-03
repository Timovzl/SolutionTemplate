using Hangfire;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.JobRunner.Jobs;

internal interface IJob
{
	string CronSchedule { get; }

	[DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
	Task Execute(CancellationToken cancellationToken);
}
