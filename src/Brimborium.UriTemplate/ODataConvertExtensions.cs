using System.Text;

namespace Brimborium.UriTemplate;

/// <summary>
/// Provides methods for OData value formatting with options for Uri, encoded.
/// </summary>
public static class ODataConvertExtensions {
    extension(ODataConvertOptions options) {
        /// <summary>
        /// Append the value to the output.
        /// </summary>
        /// <param name="value">The value to append</param>
        /// <param name="output">The target.</param>
        public bool AppendValue(object? value, StringBuilder output) {
            if (value is null) {
                options.AppendNull(output);
                return true;
            } else if (value is string stringValue) {
                options.AppendStringValue(stringValue, output);
                return true;
            } else if (value is int intValue) {
                options.AppendInt(intValue, output);
                return true;
            } else if (value is long longValue) {
                options.AppendLong(longValue, output);
                return true;
            } else if (value is float floatValue) {
                options.AppendFloat(floatValue, output);
                return true;
            } else if (value is double doubleValue) {
                options.AppendDouble(doubleValue, output);
                return true;
            } else if (value is decimal decimalValue) {
                options.AppendDecimal(decimalValue, output);
                return true;
            } else if (value is DateTimeOffset dateTimeOffsetValue) {
                options.AppendDateTimeOffset(dateTimeOffsetValue, output);
                return true;
            } else if (value is DateTime dateTimeValue) {
                options.AppendDateTime(dateTimeValue, output);
                return true;
            } else if (value is DateOnly dateOnlyValue) {
                options.AppendDateOnly(dateOnlyValue, output);
                return true;
            } else if (value is TimeOnly timeOnlyValue) {
                options.AppendTimeOnly(timeOnlyValue, output);
                return true;
            } else if (value is bool boolValue) {
                options.AppendBoolean(boolValue, output);
                return true;
            } else {
                throw new NotSupportedException(value.GetType().Name);
                // return false;
            }
        }

        /// <summary>
        /// Append null.
        /// </summary>
        /// <param name="output"></param>
        public void AppendNull(StringBuilder output) {
            output.Append("null");
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="stringValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendStringValue(string stringValue, StringBuilder output) {
            _ = output.Append('\'');
            UriCharacterUtility.AppendODataStringContent(stringValue, output);
            _ = output.Append('\'');
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="intValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendInt(int intValue, StringBuilder output) {
            var result = ODataConvert.ConvertIntToString(intValue);
            _ = output.Append(result);
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="longValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendLong(long longValue, StringBuilder output) {
            var result = ODataConvert.ConvertLongToString(longValue);
            _ = output.Append(result);
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="floatValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendFloat(float floatValue, StringBuilder output) {
            var result = ODataConvert.ConvertFloatToString(floatValue);
            _ = output.Append(EncodePlus(result));
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="doubleValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendDouble(double doubleValue, StringBuilder output) {
            var result = ODataConvert.ConvertDoubleToString(doubleValue);
            _ = output.Append(EncodePlus(result));
        }

        /// <summary>
        /// Append a value as a OData string.
        /// </summary>
        /// <param name="decimalValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendDecimal(decimal decimalValue, StringBuilder output) {
            var result = ODataConvert.ConvertDecimalToString(decimalValue);
            _ = output.Append(result);
        }

        /// <summary>
        /// Appends the OData-formatted date value as <c>yyyy-MM-dd</c>.
        /// </summary>
        /// <param name="dateOnly">The date to format.</param>
        /// <param name="output">The target.</param>
        public void AppendDateOnly(DateOnly dateOnly, StringBuilder output) {
            var valueText = ODataConvert.ConvertDateOnlyToString(dateOnly);
            AppendQuotedAsConfigured(options, valueText, output);
        }

        /// <summary>
        /// Appends the OData-formatted time value.
        /// </summary>
        /// <param name="timeOnly">The value to convert.</param>
        /// <param name="output">The target.</param>
        public void AppendTimeOnly(TimeOnly timeOnly, StringBuilder output) {
            var valueText = ODataConvert.ConvertTimeOnlyToString(timeOnly);
            AppendQuotedAsConfigured(options, valueText, output);
        }

        /// <summary>
        /// Appends the OData-formatted date and time value.
        /// </summary>
        /// <param name="dateTimeValue">The value to convert.</param>
        /// <param name="output">The target.</param>
        public void AppendDateTime(DateTime dateTimeValue, StringBuilder output) {
            var valueText = ODataConvert.ConvertDateTimeToString(dateTimeValue, options);
            AppendQuotedAsConfigured(options, EncodePlus(valueText), output);
        }

        /// <summary>
        /// Appends the value OData-formatted.
        /// </summary>
        /// <param name="dateTimeOffsetValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendDateTimeOffset(DateTimeOffset dateTimeOffsetValue, StringBuilder output) {
            var valueText = ODataConvert.ConvertDateTimeOffsetToString(dateTimeOffsetValue);
            AppendQuotedAsConfigured(options, EncodePlus(valueText), output);
        }

        /// <summary>
        /// Appends the value OData-formatted.
        /// </summary>
        /// <param name="booleanValue">The value to append.</param>
        /// <param name="output">The target.</param>
        public void AppendBoolean(bool booleanValue, StringBuilder output) {
            var valueText = ODataConvert.ConvertBooleanToString(booleanValue);
            output.Append(valueText);
        }

    }

    internal static string EncodePlus(string value) {
        if (value.Contains('+')) {
            return value.Replace("+", "%2B");
        } else {
            return value;
        }
    }

    internal static void AppendQuotedAsConfigured(
        ODataConvertOptions options,
        string valueEncoded,
        StringBuilder output
        ) {
        if (options is { ODataDateTimeLiteralStyle: ODataDateTimeLiteralStyle.Unquoted }) {
            _ = output.Append(valueEncoded);
        } else {
            _ = output.Append('\'').Append(valueEncoded).Append('\'');
        }
    }
}