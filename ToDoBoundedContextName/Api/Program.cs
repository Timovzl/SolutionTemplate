using __ToDoAreaName__.__ToDoBoundedContextName__.Application;
using __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;
using __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Api;

public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

		builder.Services.AddApplicationLayer(builder.Configuration);
		builder.Services.AddDatabaseInfrastructureLayer(builder.Configuration);
		builder.Services.AddDatabaseMigrations();

		// Register the mock dependencies
		builder.Services.Scan(scanner => scanner.FromAssemblies(typeof(Program).Assembly)
			.AddClasses(c => c.Where(type => type.Name.StartsWith("Mock")))
			.AsSelfWithInterfaces().WithSingletonLifetime());

		builder.Services.AddApplicationControllers();

		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(swagger =>
		{
			swagger.CustomSchemaIds(type => type.FullName!["__ToDoAreaName__.__ToDoBoundedContextName__.Contracts.".Length..]);

			swagger.SupportNonNullableReferenceTypes();
			swagger.SwaggerDoc("V1", new OpenApiInfo()
			{
				Title = "__ToDoBoundedContextName__ API",
				Description = """
				<p>This page documents the __ToDoBoundedContextName__ API.</p>
				""",
			});

			var apiDocumentationFilePath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
			swagger.IncludeXmlComments(apiDocumentationFilePath);
			var contractsDocumentationFilePath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Contracts.Optional<object>).Assembly.GetName().Name}.xml");
			swagger.IncludeXmlComments(contractsDocumentationFilePath);
		});

		builder.Services.AddHealthChecks();

		services.AddExceptionHandler<RequestExceptionHandler>();

		var app = builder.Build();
		
		if (builder.Environment.IsDevelopment())
			app.UseDeveloperExceptionPage();

        app.UseExceptionHandler("/Error");

		app.UseRouting();

		// Expose a health check endpoint
		app.UseHealthChecks("/health");

		// Expose Prometheus metrics
		app.UseMetricServer();
		app.UseHttpMetrics();

		app.UseSwagger(swagger =>
		{
			swagger.RouteTemplate = "api/{DocumentName}/swagger.json";
		});
		app.UseSwaggerUI(swagger =>
		{
			swagger.RoutePrefix = "api";
			swagger.SwaggerEndpoint("V1/swagger.json", "__ToDoBoundedContextName__ API");
		});

		app.UseApplicationControllers();

		await app.RunAsync();
	}
}
