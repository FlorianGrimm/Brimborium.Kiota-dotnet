using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace Microsoft.Kiota.Serialization.Text;
/// <summary>
/// The <see cref="IParseNode"/> implementation for the text/plain content type
/// </summary>
public class TextParseNode : IParseNode {
    internal const string NoStructuredDataMessage = "text does not support structured data";
    private readonly string? _Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextParseNode"/> class.
    /// </summary>
    /// <param name="text">The text value.</param>
    public TextParseNode(string? text) {
        this._Text = text?.Trim('"');
    }
    /// <inheritdoc />
    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }
    /// <inheritdoc />
    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }
    /// <inheritdoc />
    public bool? GetBoolValue() => bool.TryParse(this._Text, out var result) ? result : null;

    /// <inheritdoc />
    public byte[]? GetByteArrayValue() => string.IsNullOrEmpty(this._Text) ? null : Convert.FromBase64String(this._Text);

    /// <inheritdoc />
    public byte? GetByteValue() => byte.TryParse(this._Text, out var result) ? result : null;

    /// <inheritdoc />
    public IParseNode GetChildNode(string identifier) => throw new InvalidOperationException(NoStructuredDataMessage);

    /// <inheritdoc />
    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable => throw new InvalidOperationException(NoStructuredDataMessage);

    /// <inheritdoc />
    public IEnumerable<T> GetCollectionOfPrimitiveValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() => throw new InvalidOperationException(NoStructuredDataMessage);

    /// <inheritdoc />
    public DateTime? GetDateTimeValue() => DateTime.TryParse(this._Text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;

    /// <inheritdoc />
    public DateTimeOffset? GetDateTimeOffsetValue() => DateTimeOffset.TryParse(this._Text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;

    /// <inheritdoc />
    public DateOnly? GetDateValue() => DateTime.TryParse(this._Text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? DateOnly.FromDateTime(result) : null;
    
    /// <inheritdoc />
    public decimal? GetDecimalValue() => decimal.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public double? GetDoubleValue() => double.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public float? GetFloatValue() => float.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public Guid? GetGuidValue() => Guid.TryParse(this._Text, out var result) ? result : null;
    
    /// <inheritdoc />
    public int? GetIntValue() => int.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public long? GetLongValue() => long.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable => throw new InvalidOperationException(NoStructuredDataMessage);
    
    /// <inheritdoc />
    public sbyte? GetSbyteValue() => sbyte.TryParse(this._Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    
    /// <inheritdoc />
    public string? GetStringValue() => this._Text;
    
    /// <inheritdoc />
    public TimeSpan? GetTimeSpanValue() => string.IsNullOrEmpty(this._Text) ? null : XmlConvert.ToTimeSpan(this._Text);
    
    /// <inheritdoc />
    public TimeOnly? GetTimeValue() => DateTime.TryParse(this._Text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? TimeOnly.FromDateTime(result) : null;

    /// <inheritdoc />
    public IEnumerable<T?> GetCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
        => throw new InvalidOperationException(NoStructuredDataMessage);
    
    /// <inheritdoc />
    public T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
        => this._Text is null ? null : EnumHelpers.GetEnumValue<T>(this._Text);
}
