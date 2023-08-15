using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases.Shared.Conventions;

internal sealed class MonetaryAmountConvention : IModelFinalizingConvention
{
	public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
	{
		foreach (var monetaryAmountProperty in modelBuilder.Metadata.GetEntityTypes()
			.SelectMany(entityType => entityType.GetProperties())
			.Where(property => property.PropertyInfo?.GetCustomAttribute<MonetaryAmountAttribute>() is not null))
		{
			monetaryAmountProperty.Builder.HasConversion(new MonetaryAmountConverter(), fromDataAnnotation: true);
		}
	}
}

/// <summary>
/// Performs a meaningless conversion between <see cref="Decimal"/>s, but throws if too much precision is being sent to the database, according to <see cref="MonetaryAmount"/>.
/// </summary>
file sealed class MonetaryAmountConverter : ValueConverter<decimal, decimal>
{
	public MonetaryAmountConverter()
		: base(codeValue => MonetaryAmount.ValueOrTooPrecise(codeValue), dbValue => dbValue)
	{
	}
}
