namespace __ToDoAreaName__.__ToDoBoundedContextName__.Domain.Shared;

/// <summary>
/// <para>
/// Provides operations for working with decimal values intended to store monetary amounts, such as 1.12.
/// </para>
/// <para>
/// With <see cref="ValueOrTooPrecise"/>, this type helps verify that we have properly truncated or rounded any values before we send them to the database.
/// We would much rather throw than let the database silently evaporate partial assets.
/// </para>
/// <para>
/// Permits exactly 2 decimal places. Support for currencies with other numbers of decimal places is not implemented.
/// </para>
/// </summary>
public static class MonetaryAmount
{
	public const int DecimalPlaces = 2; // This could be removed if currencies with different decimal place counts are introduced, but at least we can see what code relies on this
	private static readonly decimal RoundingMultiplier = (decimal)Math.Pow(10, DecimalPlaces);

	/// <summary>
	/// Returns the given value, or throws if it has too much precision.
	/// </summary>
	public static decimal ValueOrTooPrecise(decimal value)
	{
		if (IsTooPrecise(value))
			throw new OverflowException($"Developer error: Forgot to truncate/distribute subcent precision in the appropriate place before converting a decimal value to an actual, storable {nameof(MonetaryAmount)}: {value}.");

		return value;
	}

	/// <summary>
	/// <para>
	/// Determines whether the precision of the given <see cref="MonetaryAmount"/> exceeds a real monetary amount, such as when subcent precision is included.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	public static bool IsTooPrecise(decimal value)
	{
		var hasAdditionalPrecision = Truncate(value) != value;
		return hasAdditionalPrecision;
	}

	/// <summary>
	/// <para>
	/// Returns the given <paramref name="value"/> by truncating superfluous decimal places for a <see cref="MonetaryAmount"/>, effectively rounding towards zero.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	/// <returns>A value suitable as a <see cref="MonetaryAmount"/>.</returns>
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
	/// Returns the given <paramref name="value"/> by truncating superfluous decimal places for a <see cref="MonetaryAmount"/>, effectively rounding towards zero.
	/// Also outputs the truncated portion.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	/// <param name="subtractedPortion">The portion considered superfluous precision. Positive for positive inputs and negative for negative inputs.</param>
	/// <returns>A value suitable as a <see cref="MonetaryAmount"/>.</returns>
	public static decimal Truncate(decimal value, out decimal subtractedPortion)
	{
		var result = Truncate(value);
		subtractedPortion = value - result;
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the portion of the given <paramref name="value"/> that is considered superfluous precision for a <see cref="MonetaryAmount"/>.
	/// </para>
	/// <para>
	/// The result is positive for positive inputs and negative for negative inputs.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	public static decimal GetSuperfluousPrecision(decimal value)
	{
		Truncate(value, out var result);
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the given <paramref name="value"/> by rounding to the appropriate number of decimal places for a <see cref="MonetaryAmount"/>.
	/// </para>
	/// <para>
	/// Midpoint values (e.g. 1.5, -20.5) are rounded away from zero.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	/// <returns>A value suitable as a <see cref="MonetaryAmount"/>.</returns>
	public static decimal Round(decimal value)
	{
		var result = Math.Round(value, 2, MidpointRounding.AwayFromZero) * 1.00m; // The multiplication enforces 2 decimal places
		return result;
	}

	/// <summary>
	/// <para>
	/// Returns the given <paramref name="value"/> by rounding to the appropriate number of decimal places for a <see cref="MonetaryAmount"/>.
	/// Also outputs the "lost" portion (positive or negative).
	/// </para>
	/// <para>
	/// Midpoint values (e.g. 1.5, -20.5) are rounded away from zero.
	/// </para>
	/// <para>
	/// Support for currencies with a precision of anything other than 2 decimals (e.g. JPY(0), IQD(3)) is not implemented.
	/// </para>
	/// </summary>
	/// <param name="subtractedPortion">The subtracted portion. Positive when rounding down or negative when rounding up.</param>
	/// <returns>A value suitable as a <see cref="MonetaryAmount"/>.</returns>
	public static decimal Round(decimal value, out decimal subtractedPortion)
	{
		var result = Round(value);
		subtractedPortion = value - result;
		return result;
	}
}
