using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;

/// <summary>
/// <para>
/// Middleware that estimates if cancellation was caused by shutdown, request aborted, or something else.
/// </para>
/// <para>
/// This type exists because cancellation checks are imperfect.
/// </para>
/// <para>
/// Checking <see cref="OperationCanceledException.CancellationToken"/>: If multiple tokens are combined into a new token, we would not match, and wrongfully infer a "hard" failure.
/// </para>
/// <para>
/// Checking <see cref="CancellationToken.IsCancellationRequested"/>: If ApplicationStopping or RequestAborted was not included in a slow operation, that operation times out, and the unused token (RequestAborted, ApplicationStopping) was cancelled in the meantime, we would wrongfully infer a "soft" failure.
/// </para>
/// </summary>
public sealed class CancellationReasonMiddleware : IMiddleware
{
	public enum WellKnownCancellationReason : byte
	{
		Shutdown,
		RequestAborted,
	}

	public static readonly object HttpContextItemKey = new object();

	/// <summary>
	/// Any cancellation token that was cancelled more than this long before we observed the cancellation was <em>not</em> the reason.
	/// Apparently, we were ignoring that cancellation token.
	/// </summary>
	private const int MaxMillisecondsDelayBeforeObservingCancellation = 3000;

	private static long ShutdownTimestamp;
	private long RequestAbortedTimestamp { get; set; }

	/// <summary>
	/// <para>
	/// Estimates the reason for cancellation, usually in case of an <see cref="OperationCanceledException"/>.
	/// </para>
	/// <para>
	/// Null if no well-known cancellation reason was observed.
	/// </para>
	/// </summary>
	public WellKnownCancellationReason? Reason => (GetMillisecondsSince(ShutdownTimestamp), GetMillisecondsSince(this.RequestAbortedTimestamp)) switch
	{
		( >= 0 and <= MaxMillisecondsDelayBeforeObservingCancellation, > 0 and <= MaxMillisecondsDelayBeforeObservingCancellation) => ShutdownTimestamp < this.RequestAbortedTimestamp
			? WellKnownCancellationReason.Shutdown
			: WellKnownCancellationReason.RequestAborted,
		( >= 0 and <= MaxMillisecondsDelayBeforeObservingCancellation, _) => WellKnownCancellationReason.Shutdown,
		(_, >= 0 and <= MaxMillisecondsDelayBeforeObservingCancellation) => WellKnownCancellationReason.RequestAborted,
		_ => null,
	};

	public CancellationReasonMiddleware(IHostApplicationLifetime hostApplicationLifetime)
	{
		// Only the first time, ensure that shutdown causes ShutdownTimestamp to be set
		if (ShutdownTimestamp == default && Interlocked.CompareExchange(ref ShutdownTimestamp, -1, default) == default)
			hostApplicationLifetime.ApplicationStopping.Register(static () => ShutdownTimestamp = Stopwatch.GetTimestamp());
	}

	public async Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		context.Items[HttpContextItemKey] = this;

		using var cancellationTokenRegistration = context.RequestAborted.Register(
			self => ((CancellationReasonMiddleware)self!).RequestAbortedTimestamp = Stopwatch.GetTimestamp(),
			state: this,
			useSynchronizationContext: false);

		await next(context);
	}

	private static int GetMillisecondsSince(long timestamp)
	{
		return timestamp <= 0
			? -1
			: (int)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
	}
}
