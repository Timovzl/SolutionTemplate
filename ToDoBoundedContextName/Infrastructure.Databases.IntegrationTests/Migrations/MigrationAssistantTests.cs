using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Infrastructure.Databases.IntegrationTests.Migrations;

public class MigrationAssistantTests : IntegrationTestBase
{
	[Test]
	public async Task MigrateAsync_Regularly_ShouldHaveExpectedEffect()
	{
		this.ShouldCreateDatabase = false;

		var instance = this.Host.Services.GetRequiredService<MigrationAssistant<CoreDbContext>>();

		await instance.MigrateAsync(CancellationToken.None);

		await Assert.That(Convert.ToInt32(await this.ExecuteScalar("SELECT COUNT(*) FROM __EFMigrationsHistory;"))).IsNotEqualTo(0);
	}

	[Test]
	public async Task Migrations_Always_ShouldBeUpToDate()
	{
		this.ShouldCreateDatabase = false;

		var context = this.Host.Services.GetRequiredService<CoreDbContext>();
		var services = context.GetInfrastructure();

		var modelDiffer = services.GetRequiredService<IMigrationsModelDiffer>();
		var migrationsAssembly = services.GetRequiredService<IMigrationsAssembly>();
		var modelRuntimeInitializer = services.GetRequiredService<IModelRuntimeInitializer>();

		// Get the current design-time model
		var currentModel = services.GetRequiredService<IDesignTimeModel>().Model;

		// Before the first (non-boilerplate )migration is created, there is no snapshot to compare to
		var modelSnapshot = migrationsAssembly.ModelSnapshot;
		if (modelSnapshot is null && migrationsAssembly.Migrations.Count <= 1)
			return;

		var snapshotModel = modelSnapshot!.Model;

		// If the model is mutable, finalize it first
		if (snapshotModel is IMutableModel mutableModel)
			snapshotModel = mutableModel.FinalizeModel();

		// Initialize the snapshot model with runtime dependencies
		snapshotModel = modelRuntimeInitializer.Initialize(snapshotModel);

		// Now we can safely call GetRelationalModel()
		var differences = modelDiffer.GetDifferences(
			snapshotModel.GetRelationalModel(),
			currentModel.GetRelationalModel());

		// If there are pending model changes since the last migration, then the developer either made a mistake or forgot to add a migration
		await Assert.That(differences).IsEmpty();
	}
}
