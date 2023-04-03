namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

/// <summary>
/// <para>
/// Marks a <see cref="Decimal"/> property as storing a monetary amount, such as 1.12.
/// </para>
/// <para>
/// With <see cref="MonetaryAmount.ValueOrTooPrecise"/>, this type helps verify that we have properly truncated or rounded any values before we send them to the database.
/// We would much rather throw than let the database silently evaporate partial assets.
/// </para>
/// <para>
/// Permits exactly 2 decimal places. Support for currencies with other numbers of decimal places is not implemented.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class MonetaryAmountAttribute : Attribute
{
}
