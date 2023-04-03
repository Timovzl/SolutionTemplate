namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.UnitTests;

public class DomainObjectTests
{
	/// <summary>
	/// Our parameterized domain object constructors perform validation.
	/// It is possible for validation rules for <em>new</em> objects to change, without existing values in the database having to change.
	/// EF must always be able to reload and should not re-validate.
	/// As such, default constructors are required on domain objects.
	/// </summary>
	[Fact]
	public void DomainObjects_Always_ShouldHaveReconstitutionConstructor()
	{
		var domainObjectWithoutReconstitutionConstructor = typeof(DomainRegistrationExtensions).Assembly.GetTypes()
			.Where(type => type.GetInterface(typeof(IDomainObject).FullName!) is not null && type.GetInterface(typeof(IDomainService).FullName!) is null) // Domain objects, but not domain services
			.Where(type => !type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition) // Concrete only
			.Where(type => !type.IsValueType) // Not value types (which are cast rather than constructed)
			.Where(type => !(type.BaseType?.IsConstructedGenericType == true && type.BaseType.GetGenericTypeDefinition() == typeof(WrapperValueObject<>))) // Not wrapper value objects (which are cast rather than constructed)
			.FirstOrDefault(type => !type.HasDefaultConstructor());

		Assert.True(domainObjectWithoutReconstitutionConstructor is null, $"{domainObjectWithoutReconstitutionConstructor?.Name} must have a (private) default constructor for safe reconstitution from the ORM. See existing entities or value objects for examples.");
	}
}
