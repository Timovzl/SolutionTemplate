using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Architect.DomainModeling;
using Architect.Identities.EntityFramework;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using __ToDoAreaName__.__ToDoBoundedContextName__.Application;
using __ToDoAreaName__.__ToDoBoundedContextName__.Domain;
using __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases;

/// <summary>
/// The DbContext for the bounded context's core database.
/// </summary>
internal sealed class CoreDbContext : DbContext, ICoreDatabase
{
	// UTF-8 (e.g. Latin1_General_100_BIN2_UTF8/Latin1_General_100_CI_AS_SC_UTF8) is avoided because its lengths are in bytes (easy to mismatch with validations in .NET) and it requires IsUnicode() and UseColumnType()

	/// <summary>
	/// Our preferred binary collation: a binary, case-sensitive collation that matches .NET's <see cref="StringComparison.Ordinal"/>.
	/// </summary>
	public const string BinaryCollation = "Latin1_General_100_BIN2";
	/// <summary>
	/// <para>
	/// Our preferred culture-sensitive collation: a culture-sensitive, ignore-case, accent-sensitive collation.
	/// </para>
	/// <para>
	/// Use this collation only for non-indexed (or at the very least non-FK) columns, such as titles and descriptions.
	/// </para>
	/// </summary>
	public const string CulturalCollation = "Latin1_General_100_CI_AS";
	/// <summary>
	/// Our default collation, used for textual columns that do not specify one.
	/// </summary>
	public const string DefaultCollation = BinaryCollation;

	/// <summary>
	/// Fired after a <see cref="CoreDbContext"/> instance is disposed.
	/// Note that the instance may have already been returned to the pool if <see cref="DbContext"/> pooling is enabled.
	/// </summary>
	internal static event Action<CoreDbContext>? DbContextDisposed;

	public CoreDbContext(DbContextOptions<CoreDbContext> options)
		: base(options)
	{
	}

	public override void Dispose()
	{
		base.Dispose();
		DbContextDisposed?.Invoke(this);
	}

	public override ValueTask DisposeAsync()
	{
		var result = base.DisposeAsync();
		DbContextDisposed?.Invoke(this);
		return result;
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		modelBuilder.UseCollation(DefaultCollation);

		modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoreDbContext).Assembly);

		this.PluralizeTableNames(modelBuilder);

		Seeder.AddSeedData(modelBuilder);
	}

	/// <summary>
	/// Ensures that table names are in plural.
	/// Although EF does this automatically where our <see cref="DbSet{TEntity}"/>s are named this way, entities without one (i.e. non-roots) require manual intervention.
	/// </summary>
	private void PluralizeTableNames(ModelBuilder modelBuilder)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes().Where(entityType => !entityType.IsOwned() && entityType.ClrType is not null))
		{
			var clrTypeName = entityType.ClrType!.Name;

			entityType.SetTableName(clrTypeName.EndsWith('y')
				? $"{clrTypeName[..^1]}ies"
				: clrTypeName.EndsWith('s')
				? $"{clrTypeName}es"
				: $"{clrTypeName}s");
		}
	}

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);

		// We map things explicitly
		configurationBuilder.Conventions.Remove(typeof(ConstructorBindingConvention));
		configurationBuilder.Conventions.Remove(typeof(RelationshipDiscoveryConvention));
		configurationBuilder.Conventions.Remove(typeof(PropertyDiscoveryConvention));

		configurationBuilder.Conventions.Add(_ => new UninitializedInstantiationConvention());
		configurationBuilder.Conventions.Add(_ => new LimitedPrecisionDecimalConvention());
		configurationBuilder.Conventions.Add(_ => new MonetaryAmountConvention());

		configurationBuilder.ConfigureDecimalIdTypes(typeof(DomainRegistrationExtensions).Assembly);

		configurationBuilder.Properties<DateTime>()
			.HaveConversion<UtcDateTimeConverter>()
			.HavePrecision(3);

		configurationBuilder.Properties<DateOnly>()
			.HaveConversion<DateOnlyConverter>()
			.HaveColumnType("date");

		// Configure default precision for (non-ID) decimals outside of properties (e.g. in CAST(), SUM(), AVG(), etc.)
		configurationBuilder.DefaultTypeMapping<decimal>()
			.HasPrecision(19, 9);

		// Configure default precision for (non-ID) decimal properties
		configurationBuilder.Properties<decimal>()
			.HavePrecision(19, 9);

		configurationBuilder.Properties<ExternalId>()
			.HaveConversion<WrapperConverter<ExternalId, string>>()
			.HaveMaxLength(ExternalId.MaxLength)
			.UseCollation(BinaryCollation);
	}

	/// <summary>
	/// A convention that instantiates objects using <see cref="FormatterServices.GetUninitializedObject"/>, bypassing constructors.
	/// This avoids the need to add default constructors and the risk that parameterized constructors are inadvertently used.
	/// </summary>
	private sealed class UninitializedInstantiationConvention : IModelFinalizingConvention
	{
		public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
		{
			foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
			{
				if (entityType.ClrType.IsAbstract)
					continue;

#pragma warning disable EF1001 // Internal EF Core API usage -- EF demands usable constructors, even if we would use an interceptor that prevents their usage entirely
				var underlyingEntityType = entityType as EntityType ?? throw new NotImplementedException("Internal changes to the EF Core API have broken this code. Are public methods now available to configure instantiation?");
				underlyingEntityType.ConstructorBinding = new UninitializedBinding(entityType.ClrType);
#pragma warning restore EF1001 // Internal EF Core API usage
			}
		}

		/// <summary>
		/// An <see cref="InstantiationBinding"/> that produces uninitialized objects.
		/// </summary>
		private sealed class UninitializedBinding : InstantiationBinding
		{
			private static readonly MethodInfo GetUninitializedObjectMethod = typeof(FormatterServices).GetMethod(nameof(FormatterServices.GetUninitializedObject))!;

			public override Type RuntimeType { get; }

			public UninitializedBinding(Type runtimeType)
				: base(Array.Empty<ParameterBinding>())
			{
				this.RuntimeType = runtimeType;
			}

			public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo)
			{
				return Expression.Convert(
					Expression.Call(method: GetUninitializedObjectMethod, arguments: Expression.Constant(this.RuntimeType)),
					this.RuntimeType);
			}

			public override InstantiationBinding With(IReadOnlyList<ParameterBinding> parameterBindings)
			{
				return this;
			}
		}
	}

	private sealed class LimitedPrecisionDecimalConvention : IModelFinalizingConvention
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

	private sealed class MonetaryAmountConvention : IModelFinalizingConvention
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
	/// Converts <see cref="DateTime"/> values so that values from the database are interpreted as <see cref="DateTimeKind.Utc"/>.
	/// </summary>
	private sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
	{
		public UtcDateTimeConverter()
			: base(codeValue => codeValue, dbValue => DateTime.SpecifyKind(dbValue, DateTimeKind.Utc))
		{
		}
	}

	/// <summary>
	/// Converts <see cref="DateOnly"/> values so that the database provider understands them.
	/// </summary>
	private sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
	{
		public DateOnlyConverter()
			: base(codeValue => codeValue.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), dbValue => DateOnly.FromDateTime(dbValue))
		{
		}
	}

	/// <summary>
	/// Performs a meaningless conversion between <see cref="Decimal"/>s, but throws if too much precision is being sent to the database, according to <see cref="MonetaryAmount"/>.
	/// </summary>
	private sealed class MonetaryAmountConverter : ValueConverter<decimal, decimal>
	{
		public MonetaryAmountConverter()
			: base(codeValue => MonetaryAmount.ValueOrTooPrecise(codeValue), dbValue => dbValue)
		{
		}
	}

	/// <summary>
	/// Performs a meaningless conversion between <see cref="Decimal"/>s, but throws if too much precision is being sent to the database, according to <see cref="LimitedPrecisionDecimal"/>.
	/// </summary>
	private sealed class LimitedPrecisionDecimalConverter : ValueConverter<decimal, decimal>
	{
		public LimitedPrecisionDecimalConverter()
			: base(codeValue => LimitedPrecisionDecimal.ValueOrTooPrecise(codeValue), dbValue => dbValue)
		{
		}
	}

	/// <summary>
	/// Used by the manual migration tool.
	/// </summary>
	internal sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
	{
		public CoreDbContext CreateDbContext(string[] args)
		{
			// Pooling should be disabled in design-time migrations, to avoid connection issues caused by ALTER DATABASE queries
			var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
			optionsBuilder.UseSqlServer(@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Initial Catalog=__ToDoAreaName__.__ToDoBoundedContextName__;Connect Timeout=5;Pooling=False;");

			return new CoreDbContext(optionsBuilder.Options);
		}
	}
}
