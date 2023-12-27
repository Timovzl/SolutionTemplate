using System.Linq.Expressions;
using System.Reflection;
using Hangfire;
using __ToDoAreaName__.__ToDoBoundedContextName__.Application.Shared;
using __ToDoAreaName__.__ToDoBoundedContextName__.JobRunner.Jobs;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.JobRunner.Shared;

/// <inheritdoc />
internal sealed class HangfireJobEnqueuer(
	IBackgroundJobClient backgroundJobClient)
	: IJobEnqueuer
{
	private static readonly MethodInfo JobExecuteMethod = typeof(IJob).GetMethod(nameof(IJob.Execute)) ?? throw new InvalidProgramException($"Method {nameof(IJob)}.{nameof(IJob.Execute)} was not found.");

	private static readonly MethodInfo EnqueueMethod = typeof(BackgroundJobClientExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public).Single(method =>
		method.Name == nameof(BackgroundJobClientExtensions.Enqueue) &&
		method.IsGenericMethodDefinition &&
		method.GetParameters()[^1].ParameterType.GenericTypeArguments.All(arg => arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(Func<,>)));

	private static readonly MethodInfo ScheduleMethod = typeof(BackgroundJobClientExtensions).GetMethods(BindingFlags.Static | BindingFlags.Public).Single(method =>
		method.Name == nameof(BackgroundJobClientExtensions.Schedule) &&
		method.IsGenericMethodDefinition &&
		method.GetParameters()[^1].ParameterType == typeof(DateTimeOffset) &&
		method.GetParameters()[^2].ParameterType.GenericTypeArguments.All(arg => arg.IsGenericType && arg.GetGenericTypeDefinition() == typeof(Func<,>)));

	public Task EnqueueJob(string jobNamePrefix)
	{
		return this.EnqueueOrScheduleJobCore(jobNamePrefix, EnqueueMethod, new object?[] { null, null, });
	}

	public Task ScheduleJob(string jobNamePrefix, DateTimeOffset instant)
	{
		return this.EnqueueOrScheduleJobCore(jobNamePrefix, ScheduleMethod, new object?[] { null, null, instant, });
	}

	/// <param name="parameters">Must start with two null objects.</param>
	private Task EnqueueOrScheduleJobCore(string jobNamePrefix, MethodInfo genericEnqueueOrScheduleMethod, object?[] parameters)
	{
		System.Diagnostics.Debug.Assert(genericEnqueueOrScheduleMethod.IsGenericMethodDefinition);

		var jobType = typeof(HangfireJobEnqueuer).Assembly.GetTypes()
			.SingleOrDefault(type => type.BaseType == typeof(Job) && type.Name.StartsWith(jobNamePrefix)) ?? throw new ArgumentException($"No job named {jobNamePrefix}* was found.");

		var param = Expression.Parameter(jobType, "job");
		var call = Expression.Call(param, JobExecuteMethod, [Expression.Constant(default(CancellationToken))]);

		var lambdaType = typeof(Func<,>).MakeGenericType(jobType, typeof(Task));

		var lambda = Expression.Lambda(lambdaType, call, [param]);

		var enqueueOrScheduleMethod = genericEnqueueOrScheduleMethod.MakeGenericMethod(jobType);
		parameters[0] = backgroundJobClient;
		parameters[1] = lambda;
		enqueueOrScheduleMethod.Invoke(obj: null, parameters: parameters);

		return Task.CompletedTask;
	}
}
