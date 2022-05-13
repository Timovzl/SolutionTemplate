using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;

/// <summary>
/// A global handler for uncaught exceptions during request handling.
/// </summary>
public class RequestExceptionHandler
{
	private IHostApplicationLifetime HostApplicationLifetime { get; }
	private IHttpContextAccessor HttpContextAccessor { get; }
	private ILogger<RequestExceptionHandler> Logger { get; }

	public RequestExceptionHandler(IHostApplicationLifetime hostApplicationLifetime, IHttpContextAccessor httpContextAccessor, ILogger<RequestExceptionHandler> logger)
	{
		this.HostApplicationLifetime = hostApplicationLifetime;
		this.HttpContextAccessor = httpContextAccessor;
		this.Logger = logger;
	}

	public Task HandleExceptionAsync()
	{
		var exceptionHandlerFeature = this.HttpContextAccessor.HttpContext?.Features.Get<IExceptionHandlerFeature>();
		var exception = exceptionHandlerFeature?.Error;

		// Shutdown is an acceptable reason for cancellation
		if (exception is OperationCanceledException && this.HostApplicationLifetime.ApplicationStopping.IsCancellationRequested)
		{
			this.Logger.LogInformation(exception, "Shutdown cancelled the request.");
		}
		// An aborted request is an acceptable reason for cancellation
		else if (exception is OperationCanceledException && this.HttpContextAccessor.HttpContext?.RequestAborted.IsCancellationRequested == true)
		{
			this.Logger.LogInformation(exception, "The caller cancelled the request.");
		}
		// TODO Enhancement: Log validation exceptions as Warning or Informational
		else if (exception is not null)
		{
			this.Logger.LogError(exception, "The request handler has thrown an exception.");
		}
		else
		{
			this.Logger.LogError("The request exception handler was invoked, but no exception was available.");
		}

		return Task.CompletedTask;
	}
}
