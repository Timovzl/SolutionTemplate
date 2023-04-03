namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

/// <summary>
/// <para>
/// Marks a <see cref="Decimal"/> property as not following our standard of requiring rounding or truncation before being persisted.
/// </para>
/// <para>
/// A property <em>without</em> this attribute will cause an exception if it is attempted to be stored with a value with too much precision.
/// </para>
/// <para>
/// Beware that, as a side-effect of using this attribute, property values are implicitly rounded or truncated when they are persisted, but they still retain a very high precision.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class UnlimitedPrecisionAttribute : Attribute
{
}
