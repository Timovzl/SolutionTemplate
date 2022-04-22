using System.Globalization;
using __ToDoAreaName__.__ToDoBoundedContextName__.Domain;
using __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;
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
		services.AddDatabaseInfrastructureLayer(configuration);

		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(ApplicationRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => !type.IsNested), publicOnly: false)
			.AsSelfWithInterfaces().WithSingletonLifetime());
		
		services.AddHttpContextAccessor();

		return services;
	}
}
