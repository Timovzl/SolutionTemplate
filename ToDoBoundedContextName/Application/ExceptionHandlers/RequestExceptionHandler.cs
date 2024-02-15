using System.Net;
using System.Text;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;

/// <summary>
/// A global handler for uncaught exceptions during request handling.
/// Treats validation errors as user error (4xx) and other errors as developer error (5xx).
/// </summary>
public sealed class RequestExceptionHandler(
	IHostApplicationLifetime hostApplicationLifetime,
	ILogger<RequestExceptionHandler> logger)
	: IExceptionHandler
{
	public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
	{
		// Note:
		// Cancellation checks are imperfect
		// Checking OperationCanceledException.CancellationToken: If multiple tokens are combined into a new token, we would not match, and wrongfully infer a "hard" failure
		// Checking CancellationToken.IsCancellationRequested: If a slow query or HTTP request times out, and the comparison token (RequestAborted, ApplicationStopping) was cancelled in the meantime, we would match, and wrongfully infer a "soft" failure
		// We choose the former as the lesser evil and can adapt if it proves to result in false positives

		// Shutdown is an acceptable reason for cancellation
		if ((exception as OperationCanceledException)?.CancellationToken == hostApplicationLifetime.ApplicationStopping)
			logger.LogInformation(exception, "Shutdown cancelled the request");
		// An aborted request is an acceptable reason for cancellation
		else if ((exception is OperationCanceledException opCanceledException) && opCanceledException.CancellationToken == httpContext.RequestAborted)
			logger.LogInformation(exception, "The caller cancelled the request");
		else if (exception is ValidationException validationException)
			await HandleValidationExceptionAsync(validationException, httpContext, cancellationToken);
		else if (exception is not null)
			logger.LogError(exception, "The request handler has thrown an exception");

		return true;
	}

	private ValueTask HandleValidationExceptionAsync(ValidationException exception, HttpContext httpContext, CancellationToken cancellationToken)
	{
		logger.LogInformation(exception, "The request was invalid: {Message}", exception.Message);

		// Respond with the rejection if possible
		if (httpContext?.Response.HasStarted == false)
		{
			httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
			httpContext.Response.ContentType = "text/plain";
			return httpContext.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(exception.Message), cancellationToken);
		}

		return ValueTask.CompletedTask;
	}
}
