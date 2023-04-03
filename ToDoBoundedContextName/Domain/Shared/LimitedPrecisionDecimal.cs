namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

/// <summary>
/// <para>
/// Provides operations for working with decimal values intended to have a notable but limited precision.
/// </para>
/// <para>
/// For example, this can be used to persist security sizes, or monetary values not yet rounded to cents.
/// </para>
/// <para>
/// With <see cref="ValueOrTooPrecise"/>, this type helps verify that we have properly truncated or rounded any values before we send them to the database.
/// We would much rather throw than let the database silently evaporate partial assets.
/// </para>
/// </summary>
public static class LimitedPrecisionDecimal
{
	public const int DecimalPlaces = 4;
	private static readonly decimal RoundingMultiplier = (decimal)Math.Pow(10, DecimalPlaces);

	/// <summary>
	/// Returns the given value, or throws if it has too much precision.
	/// </summary>
	public static decimal ValueOrTooPrecise(decimal value)
	{
		if (IsTooPrecise(value))
			throw new OverflowException($"Developer error: Forgot to truncate/distribute superfluous precision in the appropriate place before converting a decimal value to an actual, storable {nameof(LimitedPrecisionDecimal)}: {value}.");

		return value;
	}

	/// <summary>
	/// <para>
	/// Determines whether the precision of the given <see cref="LimitedPrecisionDecimal"/> exceeds the desired and storable representation.
	/// </para>
	/// </summary>
	public static bool IsTooPrecise(decimal value)
	{
		var hasAdditionalPrecision = Truncate(value) != value;
		return hasAdditionalPrecision;
	}

	/// <summary>
	/// <para>
	/// Returns the given <paramref name="value"/> by truncating superfluous decimal places for a <see cref="LimitedPrecisionDecimal"/>, effectively rounding towards zero.
	/// </para>
	/// </summary>
	/// <returns>A value suitable as a <see cref="LimitedPrecisionDecimal"/>.</returns>
	public static decimal Truncate(decimal value)
	{
		var maximumPrecisionWithoutDecimalPlaces = value * RoundingMultiplier;
		var multipliedResult = Decimal.Truncate(maximumPrecisionWithoutDecimalPlaces); // The result, but with the decimal places still multiplied to the left of the decimal separator, i.e. needs to be divided again

		// Create a resulting decimal with the intended scale
		Span<int> multipliedResultComponents = stackalloc int[4];
		_ = Decimal.GetBits(multipliedResult, multipliedResultComponents);
		var result = new decimal(multipliedResultComponents[0], multipliedResultComponents[1], multipliedResultComponents[2], isNegative: value < 0m, scale: DecimalPlaces);
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the given <paramref name="value"/> by truncating superfluous decimal places for a <see cref="LimitedPrecisionDecimal"/>, effectively rounding towards zero.
	/// Also outputs the truncated portion.
	/// </para>
	/// </summary>
	/// <param name="subtractedPortion">The portion considered superfluous precision. Positive for positive inputs and negative for negative inputs.</param>
	/// <returns>A value suitable as a <see cref="LimitedPrecisionDecimal"/>.</returns>
	public static decimal Truncate(decimal value, out decimal subtractedPortion)
	{
		var result = Truncate(value);
		subtractedPortion = value - result;
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the portion of the given <paramref name="value"/> that is considered superfluous precision for a <see cref="LimitedPrecisionDecimal"/>.
	/// </para>
	/// <para>
	/// The result is positive for positive inputs and negative for negative inputs.
	/// </para>
	/// </summary>
	public static decimal GetSuperfluousPrecision(decimal value)
	{
		Truncate(value, out var result);
		return result;
	}
}
