using System.Net;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;

/// <summary>
/// A global handler for uncaught exceptions during request handling.
/// Treats validation errors as user error (4xx) and other errors as developer error (5xx).
/// </summary>
public sealed class RequestExceptionHandler(
	ILogger<RequestExceptionHandler> logger)
	: IExceptionHandler
{
	public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		var cancellationReasonMiddleware = (CancellationReasonMiddleware?)httpContext.Items[CancellationReasonMiddleware.HttpContextItemKey] ??
			throw new NotImplementedException($"The {nameof(CancellationReasonMiddleware)} is missing.");

		var wellKnownCancellationReason = cancellationReasonMiddleware.Reason;

		if (wellKnownCancellationReason is CancellationReasonMiddleware.WellKnownCancellationReason.Shutdown)
		{
			// Shutdown is an acceptable reason for cancellation
			logger.LogInformation(exception, "Shutdown cancelled the request");
			if (!httpContext.Response.HasStarted)
				httpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
		}
		else if (wellKnownCancellationReason is CancellationReasonMiddleware.WellKnownCancellationReason.RequestAborted)
		{
			// An aborted request is an acceptable reason for cancellation
			logger.LogInformation(exception, "The caller aborted the request");
			if (!httpContext.Response.HasStarted)
				httpContext.Response.StatusCode = 499; // "Client Closed Request"
		}
		else if (exception is ValidationException validationException)
		{
			// A validation exception is the caller's responsibility
			logger.LogInformation(exception, "The request was rejected: {Message}", exception.Message);
			if (!httpContext.Response.HasStarted)
			{
				httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
				httpContext.Response.ContentType = "text/plain";
				return WriteAsync(httpContext, exception.Message, cancellationToken);
			}
		}
		else
		{
			// Other exceptions warrant investigation
			logger.LogError(exception, "The request handler has thrown an exception");
			if (!httpContext.Response.HasStarted)
				httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
		}

		return new ValueTask<bool>(true);
	}

	private static ValueTask<bool> WriteAsync(HttpContext httpContext, string message, CancellationToken cancellationToken)
	{
		var result = httpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message), cancellationToken);
		if (result.IsCompleted)
			return new ValueTask<bool>(true);
		return new ValueTask<bool>(result.AsTask().ContinueWith(_ => true, TaskContinuationOptions.ExecuteSynchronously));
	}
}
