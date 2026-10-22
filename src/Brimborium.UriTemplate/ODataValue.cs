namespace Brimborium.UriTemplate;

/// <summary>
/// Wraps a CLR value and appends it in its OData literal representation.
/// </summary>
/// <param name="Value">The value to format as an OData literal.</param>
public sealed record ODataValue(object? Value) : IODataValue {
    /// <inheritdoc/>
    public bool AppendValue(in UriTemplateTarget target) {
        var output = target.Output;
        if (this.Value is null) {
            _ = output.Append("null");
            return true;
        } else if (this.Value is string stringValue) {
            _ = output.Append('\'');
            target.AddODataValue(stringValue);
            _ = output.Append('\'');
            return true;
        } else if (this.Value is int intValue) {
            _ = target.Append(intValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is long longValue) {
            _ = target.Append(longValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is float floatValue) {
            _ = target.Append(floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is double doubleValue) {
            _ = target.Append(doubleValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is decimal decimalValue) {
            _ = target.Append(decimalValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return true;
        } else if (this.Value is DateTimeOffset dateTimeOffsetValue) {
            if ((dateTimeOffsetValue.Ticks % 10000000) == 0) {
                var txt= dateTimeOffsetValue.ToString("yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture);
                _ = target.Append($"'{txt}'");
            } else {
                var txt = dateTimeOffsetValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                _ = target.Append($"'{txt}'");
            }
            return true;
        } else if (this.Value is DateTime dateTimeValue) {
            if ((dateTimeValue.Ticks % 10000000) == 0) {
                var txt = dateTimeValue.ToString("yyyy-MM-ddTHH:mm:ssK", System.Globalization.CultureInfo.InvariantCulture);
                _ = target.Append($"'{txt}'");
            } else { 
                var txt = dateTimeValue.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
                _ = target.Append($"'{txt}'");
            }
            return true;
        } else if (this.Value is DateOnly dateOnlyValue) {
            var txt = dateOnlyValue.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            _ = target.Append($"'{txt}'");
            return true;
        } else if (this.Value is TimeOnly timeOnlyValue) {
            var txt = timeOnlyValue.ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            _ = target.Append($"'{txt}'");
            return true;
        } else if (this.Value is bool boolValue) {
            if (boolValue) {
                _ = target.Append("true");
            } else {
                _ = target.Append("false");
            }
            return true;
        } else {
            throw new NotSupportedException(this.Value.GetType().Name);
            // return false;
        }
    }
}