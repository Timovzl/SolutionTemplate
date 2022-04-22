using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;

public static class DatabaseInfrastructureRegistrationExtensions
{
	public static IServiceCollection AddDatabaseInfrastructureLayer(this IServiceCollection services, IConfiguration _)
	{
		services.AddMemoryCache();

		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(DatabaseInfrastructureRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => !type.IsNested), publicOnly: false)
			.AsSelfWithInterfaces().WithSingletonLifetime());

		return services;
	}
}
