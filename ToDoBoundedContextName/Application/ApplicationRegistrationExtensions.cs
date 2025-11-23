using System.Globalization;
using System.Text.Json.Serialization;
using __ToDoAreaName__.__ToDoBoundedContextName__.Domain;
using __ToDoAreaName__.__ToDoBoundedContextName__.Application.ExceptionHandlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application;

public static class ApplicationRegistrationExtensions
{
	public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
	{
		// Use the invariant culture throughout the application
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

		// Register the layers that we depend on
		services.AddDomainLayer(configuration);

		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(ApplicationRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => type.Name.EndsWith("er") || type.Name.EndsWith("or") || type.Name.EndsWith("UseCase") || type.Name.EndsWith("Client")), publicOnly: false) // Services only
			.AsSelfWithInterfaces().WithSingletonLifetime());

		services.AddScoped<CancellationReasonMiddleware>();

		return services;
	}

	public static IMvcBuilder AddApplicationControllers(this IServiceCollection services)
	{
		// Consistently use our own exception handling, irrespective of whether ASP.NET Core considers the model valid
		services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

		var result = services.AddControllers()
			.AddJsonOptions(options =>
			{
				options.JsonSerializerOptions.AllowDuplicateProperties = false;
				options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
			});

		// AddAuthentication() could be added here if authentication is required

		return result;
	}

	public static IApplicationBuilder UseApplicationControllers(this IApplicationBuilder applicationBuilder)
	{
		// UseAuthentication() and UseAuthorization() could be added here if authentication and/or authorization is required (both required even to just have authentication)

		applicationBuilder.UseEndpoints(endpoints => endpoints
			.MapControllers()); // RequireAuthorization() could be added here if authentication and/or authorization is required

		return applicationBuilder;
	}
}
