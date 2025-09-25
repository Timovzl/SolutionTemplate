namespace __ToDoAreaName__.__ToDoBoundedContextName__.Application.UnitTests;

public class UseCaseTests
{
	[Test]
	public async Task UseCaseClasses_Always_ShouldBeApplicationServices()
	{
		var useCaseClasses = typeof(ApplicationRegistrationExtensions).Assembly.GetTypes()
			.Where(type => type.Name.EndsWith("UseCase") && type.IsClass);

		foreach (var type in useCaseClasses)
			await Assert.That(type.GetInterface("IApplicationService")).IsNotNull();
	}
}
