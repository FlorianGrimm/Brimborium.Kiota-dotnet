namespace Brimborium.UriTemplate;

/// <summary>
/// Provides methods for formatting values for uris but not encoded.
/// </summary>
public static class ODataConvert {
    /// <summary>
    /// Convert a value to string not encoded.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">The options; <see langword="null"/> behaves like <see cref="ODataDateTimeLiteralStyle.Quoted"/>.</param>
    /// <returns>the converted value - null if not supported.</returns>
    public static string? ConvertValueToString(object? value, ODataConvertOptions? options) {
        return value switch {
            null => string.Empty,
            string strValue => strValue,
            bool boolValue => ConvertBooleanToString(boolValue),
            int number => ConvertIntToString(number),
            long number => ConvertLongToString(number),
            float number => ConvertFloatToString(number),
            double number => ConvertDoubleToString(number),
            decimal number => ConvertDecimalToString(number),
            TimeOnly timeOnly => ConvertTimeOnlyToString(timeOnly),
            DateOnly dateOnly => ConvertDateOnlyToString(dateOnly),
            DateTime dateTime => ConvertDateTimeToString(dateTime, options),
            DateTimeOffset dateTimeOffset => ConvertDateTimeOffsetToString(dateTimeOffset),
            _ => null
            //_ => throw new ArgumentException($"Illegal class passed as substitution, found {value.GetType()}"),
        };
    }

    /// <summary>
    /// Converts an integer as a OData string.
    /// </summary>
    /// <param name="intValue">The value to convert.</param>
    /// <returns>The OData string representation of the integer.</returns>
    public static string ConvertIntToString(int intValue) {
        var result = intValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    /// <summary>
    /// Converts an integer as a OData string.
    /// </summary>
    /// <param name="longValue">The value to convert.</param>
    /// <returns>The OData string representation of the integer.</returns>
    public static string ConvertLongToString(long longValue) {
        var result = longValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    /// <summary>
    /// Converts an float as a OData string.
    /// </summary>
    /// <param name="floatValue">The float to convert.</param>
    /// <returns>The OData string representation of the float.</returns>
    public static string ConvertFloatToString(float floatValue) {
        if (float.IsPositiveInfinity(floatValue)) {
            return ("INF");
        } else if (float.IsNegativeInfinity(floatValue)) {
            return ("-INF");
        } else {
            var result = floatValue
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
    }

    /// <summary>
    /// Converts an double as a OData string.
    /// </summary>
    /// <param name="doubleValue">The double to convert.</param>
    /// <returns>The OData string representation of the double.</returns>
    public static string ConvertDoubleToString(double doubleValue) {
        if (double.IsPositiveInfinity(doubleValue)) {
            return ("INF");
        } else if (double.IsNegativeInfinity(doubleValue)) {
            return ("-INF");
        } else {
            var result = doubleValue
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
    }

    /// <summary>
    /// Converts an decimal as a OData string.
    /// </summary>
    /// <param name="decimalValue">The decimal to convert.</param>
    /// <returns>The OData string representation of the decimal.</returns>
    public static string ConvertDecimalToString(decimal decimalValue) {
        var result = decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    /// <summary>
    /// Formats a date as a OData string.
    /// </summary>
    /// <param name="dateOnly">The date to format.</param>
    /// <returns>The date formatted as `yyyy-MM-dd`.</returns>
    public static string ConvertDateOnlyToString(DateOnly dateOnly) {
        var result = dateOnly.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return result;
    }

    /// <summary>
    /// Formats a datetime as a OData string.
    /// </summary>
    /// <param name="dateTimeValue">The date to format.</param>
    /// <param name="options">The options; <see langword="null"/> behaves like <see cref="ODataDateTimeLiteralStyle.Quoted"/>.</param>
    /// <returns>The date formatted.</returns>
    public static string ConvertDateTimeToString(DateTime dateTimeValue, ODataConvertOptions? options) {
        if (options is { ODataDateTimeLiteralStyle: ODataDateTimeLiteralStyle.Unquoted }) {
            if (dateTimeValue.Kind == DateTimeKind.Unspecified) {
                // Edm.DateTimeOffset requires "Z" or an offset
                dateTimeValue = DateTime.SpecifyKind(dateTimeValue, DateTimeKind.Utc);
            }
        }
        string result;
        if ((dateTimeValue.Ticks % 10000000) == 0) {
            result = dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
        } else {
            result = dateTimeValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
        return result;
    }

    /// <summary>
    /// Formats a datetime as a OData string.
    /// </summary>
    /// <param name="dateTimeOffsetValue">The date to format.</param>
    /// <returns>The date formatted.</returns>
    public static string ConvertDateTimeOffsetToString(DateTimeOffset dateTimeOffsetValue) {
        string result;
        if ((dateTimeOffsetValue.Ticks % 10000000) == 0) {
            result = dateTimeOffsetValue.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);
        } else {
            result = dateTimeOffsetValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        }
        return result;
    }

    /// <summary>
    /// Formats a date for OData.
    /// </summary>
    /// <param name="timeOnly">The time to format.</param>
    /// <returns>The time formatted as HH:mm:ss or HH:mm:ss.fffffff.</returns>
    public static string ConvertTimeOnlyToString(TimeOnly timeOnly) {
        if ((timeOnly.Ticks % 10000000) == 0) {
            var result = timeOnly.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            return result;
        } else {
            var result = timeOnly.ToString("HH:mm:ss.fffffff", System.Globalization.CultureInfo.InvariantCulture);
            return result;
        }
    }

    /// <summary>
    /// Converts a Boolean value to its lowercase string representation.
    /// </summary>
    /// <param name="value">The Boolean value to convert.</param>
    /// <returns>The string "true" if <paramref name="value"/> is <see langword="true"/>; otherwise, "false".</returns>
    public static string ConvertBooleanToString(bool value) {
        if (value) {
            return "true";
        } else {
            return "false";
        }
    }
}
