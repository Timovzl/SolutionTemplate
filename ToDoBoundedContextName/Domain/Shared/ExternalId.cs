using Architect.DomainModeling.Comparisons;

namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

/// <summary>
/// <para>
/// An ID value originating outside of the bounded context.
/// </para>
/// </summary>
[WrapperValueObject<string>]
public readonly partial record struct ExternalId : IIdentity<string>, IComparable<ExternalId>
{
	private StringComparison StringComparison => StringComparison.Ordinal;

	public const ushort MaxLength = 50;

	public string Value { get; private init; }

	public ExternalId(string value)
	{
		this.Value = value ?? throw new NullValidationException("ExternalId_ValueNull", nameof(value));

		if (this.Value.Length == 0)
			throw new ValidationException("ExternalId_ValueEmpty", "An external ID value must not be empty.");
		if (this.Value.Length > MaxLength)
			throw new ValidationException("ExternalId_ValueToolong", $"An external ID value must not be over {MaxLength} characters long.");
		if (ValueObjectStringValidator.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(this.Value) || this.Value.AsSpan().IndexOfAny('\'', '"') >= 0)
			throw new ValidationException("ExternalId_ValueInvalid", "An external ID value must consist of printable, non-whitespace, non-quote ASCII characters.");
	}
}
