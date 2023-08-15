using Architect.Identities.EntityFramework;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
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

	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		base.ConfigureConventions(configurationBuilder);

		// We map things explicitly
		configurationBuilder.Conventions.Remove(typeof(ConstructorBindingConvention));
		configurationBuilder.Conventions.Remove(typeof(RelationshipDiscoveryConvention));
		configurationBuilder.Conventions.Remove(typeof(PropertyDiscoveryConvention));

		configurationBuilder.Conventions.Add(_ => new UninitializedInstantiationConvention());
		configurationBuilder.Conventions.Add(_ => new StringCasingConvention());
		configurationBuilder.Conventions.Add(_ => new WrapperValueObjectConversionConvention());
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
}
