using __ToDoAreaName__.__ToDoBoundedContextName__.Application;
using __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;
using __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;
using Prometheus;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Api;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddApplicationLayer(builder.Configuration);
		builder.Services.AddDatabaseInfrastructureLayer(builder.Configuration);

		builder.Services.AddHealthChecks();

		var app = builder.Build();
		
		if (builder.Environment.IsDevelopment())
			app.UseDeveloperExceptionPage();

		// TODO: Consider if !builder.Environment.IsDevelopment() app.UseHsts(), and other possible security settings

		app.UseExceptionHandler(app => app.Run(async context =>
			await context.RequestServices.GetRequiredService<RequestExceptionHandler>().HandleExceptionAsync()));

		app.UseRouting();

		// Expose a health check endpoint
		app.UseHealthChecks("/health");

		// Expose Prometheus metrics
		app.UseEndpoints(endpoints => endpoints.MapMetrics());

		await app.RunAsync();
	}
}
