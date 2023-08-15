using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases.Shared.Conventions;

internal sealed class LimitedPrecisionDecimalConvention : IModelFinalizingConvention
{
	public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
	{
		// Unless a property deliberately permits implicit rounding, throw if rounding would happen
		foreach (var monetaryAmountProperty in modelBuilder.Metadata.GetEntityTypes()
			.SelectMany(entityType => entityType.GetProperties())
			.Where(property => property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
			.Where(property => property.GetPrecision() == 19)
			.Where(property => property.PropertyInfo?.GetCustomAttribute<UnlimitedPrecisionAttribute>() is null))
		{
			monetaryAmountProperty.Builder.HasConversion(new LimitedPrecisionDecimalConverter(), fromDataAnnotation: true);
		}
	}
}

/// <summary>
/// Performs a meaningless conversion between <see cref="Decimal"/>s, but throws if too much precision is being sent to the database, according to <see cref="LimitedPrecisionDecimal"/>.
/// </summary>
file sealed class LimitedPrecisionDecimalConverter : ValueConverter<decimal, decimal>
{
	public LimitedPrecisionDecimalConverter()
		: base(codeValue => LimitedPrecisionDecimal.ValueOrTooPrecise(codeValue), dbValue => dbValue)
	{
	}
}
