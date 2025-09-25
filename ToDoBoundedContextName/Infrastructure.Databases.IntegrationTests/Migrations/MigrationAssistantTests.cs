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
}
