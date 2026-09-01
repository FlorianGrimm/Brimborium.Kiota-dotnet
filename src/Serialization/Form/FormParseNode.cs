using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Brimborium.Kiota.Abstractions;
using Brimborium.Kiota.Abstractions.Extensions;
using Brimborium.Kiota.Abstractions.Helpers;
using Brimborium.Kiota.Abstractions.Serialization;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Brimborium.Kiota.Serialization.Form;
/// <summary>Represents a parse node that can be used to parse a form url encoded string.</summary>
public class FormParseNode : IParseNode {
    private readonly string _RawValue;
    private string DecodedValue => Uri.UnescapeDataString(_RawValue);
    private readonly Dictionary<string, string> _Fields;
    private static readonly char[] _PairDelimiter = ['='];
    private static readonly char[] _EntriesDelimiter = ['&'];
    /// <summary>Initializes a new instance of the <see cref="FormParseNode"/> class.</summary>
    /// <param name="rawValue">The raw value to parse.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="rawValue"/> is null.</exception>
    public FormParseNode(string rawValue) {
        _RawValue = rawValue ?? throw new ArgumentNullException(nameof(rawValue));
        _Fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string[] pairs = rawValue.Split(_EntriesDelimiter, StringSplitOptions.RemoveEmptyEntries);
        foreach(string pair in pairs) {
            string[] keyValue = pair.Split(_PairDelimiter, StringSplitOptions.RemoveEmptyEntries);
            if(keyValue.Length == 2) {
                string key = SanitizeKey(keyValue[0]);
                string value = keyValue[1].Trim();

                if(_Fields.ContainsKey(key)) {
                    _Fields[key] += $",{value}";
                }
                else {
                    _Fields.Add(key, value);
                }
            }
        }
    }

    private static string SanitizeKey(string key) {
        if(string.IsNullOrEmpty(key)) {
            return key;
        }

        return Uri.UnescapeDataString(key.Trim());
    }
    /// <inheritdoc/>
    public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }
    /// <inheritdoc/>
    public Action<IParsable>? OnAfterAssignFieldValues { get; set; }
    /// <inheritdoc/>
    public bool? GetBoolValue() => bool.TryParse(DecodedValue, out var result) && result;
    /// <inheritdoc/>
    public byte[]? GetByteArrayValue() {
        var rawValue = DecodedValue;
        if(string.IsNullOrEmpty(rawValue)) {
            return null;
        }

        return Convert.FromBase64String(rawValue);
    }
    /// <inheritdoc/>
    public byte? GetByteValue() => byte.TryParse(DecodedValue, out var result) ? result : null;
    /// <inheritdoc/>
    public IParseNode? GetChildNode(string identifier) => _Fields.TryGetValue(SanitizeKey(identifier), out var value) ?
        new FormParseNode(value) {
            OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
            OnAfterAssignFieldValues = OnAfterAssignFieldValues
        } : null;
    /// <inheritdoc/>
    public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable => throw new InvalidOperationException("collections are not supported with uri form encoding");

    private static readonly Type _BooleanType = typeof(bool?);
    private static readonly Type _ByteType = typeof(byte?);
    private static readonly Type _SbyteType = typeof(sbyte?);
    private static readonly Type _StringType = typeof(string);
    private static readonly Type _IntType = typeof(int?);
    private static readonly Type _LongType = typeof(long?);
    private static readonly Type _DecimalType = typeof(decimal?);
    private static readonly Type _FloatType = typeof(float?);
    private static readonly Type _DoubleType = typeof(double?);
    private static readonly Type _GuidType = typeof(Guid?);
    private static readonly Type _DateTimeOffsetType = typeof(DateTimeOffset?);
    private static readonly Type _TimeSpanType = typeof(TimeSpan?);
    private static readonly Type _DateType = typeof(DateOnly?);
    private static readonly Type _TimeType = typeof(TimeOnly?);
    private static readonly char[] _ComaSeparator = [','];

    /// <summary>
    /// Get the collection of primitives of type <typeparam name="T"/>from the form node
    /// </summary>
    /// <returns>A collection of objects</returns>
#if NET5_0_OR_GREATER
    public IEnumerable<T> GetCollectionOfPrimitiveValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>()
#else
    public IEnumerable<T> GetCollectionOfPrimitiveValues<T>()
#endif
    {
        var genericType = typeof(T);
        var primitiveValueCollection = DecodedValue.Split(_ComaSeparator, StringSplitOptions.RemoveEmptyEntries);
        foreach(var collectionValue in primitiveValueCollection) {
            var decodedCollectionValue = Uri.UnescapeDataString(collectionValue);
            if(genericType == _BooleanType) {
                yield return (T)(object)GetBoolValue(decodedCollectionValue)!;
            }
            else if(genericType == _ByteType) {
                yield return (T)(object)GetByteValue(decodedCollectionValue)!;
            }
            else if(genericType == _SbyteType) {
                yield return (T)(object)GetSbyteValue(decodedCollectionValue)!;
            }
            else if(genericType == _StringType) {
                yield return (T)(object)decodedCollectionValue;
            }
            else if(genericType == _IntType) {
                yield return (T)(object)GetIntValue(decodedCollectionValue)!;
            }
            else if(genericType == _LongType) {
                yield return (T)(object)GetLongValue(decodedCollectionValue)!;
            }
            else if(genericType == _FloatType) {
                yield return (T)(object)GetFloatValue(decodedCollectionValue)!;
            }
            else if(genericType == _DoubleType) {
                yield return (T)(object)GetDoubleValue(decodedCollectionValue)!;
            }
            else if(genericType == _DecimalType) {
                yield return (T)(object)GetDecimalValue(decodedCollectionValue)!;
            }
            else if(genericType == _GuidType) {
                yield return (T)(object)GetGuidValue(decodedCollectionValue)!;
            }
            else if(genericType == _DateTimeOffsetType) {
                yield return (T)(object)GetDateTimeOffsetValue(decodedCollectionValue)!;
            }
            else if(genericType == _TimeSpanType) {
                yield return (T)(object)GetTimeSpanValue(decodedCollectionValue)!;
            }
            else if(genericType == _DateType) {
                yield return (T)(object)GetDateValue(decodedCollectionValue)!;
            }
            else if(genericType == _TimeType) {
                yield return (T)(object)GetTimeValue(decodedCollectionValue)!;
            }
            else {
                throw new InvalidOperationException($"unknown type for deserialization {genericType.FullName}");
            }
        }
    }
    /// <inheritdoc/>
    public DateTimeOffset? GetDateTimeOffsetValue() => DateTimeOffset.TryParse(DecodedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    /// <inheritdoc/>
    public DateOnly? GetDateValue() => DateTime.TryParse(DecodedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? DateOnly.FromDateTime(result) : null;
    /// <inheritdoc/>
    public decimal? GetDecimalValue() => decimal.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    /// <inheritdoc/>
    public double? GetDoubleValue() => double.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    /// <inheritdoc/>
    public float? GetFloatValue() => float.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    /// <inheritdoc/>
    public Guid? GetGuidValue() => Guid.TryParse(DecodedValue, out var result) ? result : null;
    /// <inheritdoc/>
    public int? GetIntValue() => int.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    /// <inheritdoc/>
    public long? GetLongValue() => long.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    /// <inheritdoc/>
    public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable {
        var item = factory(this);
        OnBeforeAssignFieldValues?.Invoke(item);
        AssignFieldValues(item);
        OnAfterAssignFieldValues?.Invoke(item);
        return item;
    }
    private void AssignFieldValues<T>(T item) where T : IParsable {
        if(_Fields.Count == 0) {
            return;
        }

        IDictionary<string, object>? itemAdditionalData = null;
        if(item is IAdditionalDataHolder holder) {
            holder.AdditionalData ??= new Dictionary<string, object>();
            itemAdditionalData = holder.AdditionalData;
        }
        var fieldDeserializers = item.GetFieldDeserializers();

        foreach(var fieldValue in _Fields) {
            if(fieldDeserializers.TryGetValue(fieldValue.Key, out var fieldDeserializer)) {
                if("null".Equals(fieldValue.Value, StringComparison.OrdinalIgnoreCase)) {
                    continue;// If the property is already null just continue. As calling functions like GetDouble,GetBoolValue do not process null.
                }

                // Debug.WriteLine($"found property {fieldValue.Key} to deserialize");
                fieldDeserializer.Invoke(new FormParseNode(fieldValue.Value) {
                    OnBeforeAssignFieldValues = OnBeforeAssignFieldValues,
                    OnAfterAssignFieldValues = OnAfterAssignFieldValues
                });
            }
            else if(itemAdditionalData != null) {
                // Debug.WriteLine($"found additional property {fieldValue.Key} to deserialize");
                _ = IDictionaryExtensions.TryAdd(itemAdditionalData, fieldValue.Key, fieldValue.Value);
            }
            else {
                // Debug.WriteLine($"found additional property {fieldValue.Key} to deserialize but the model doesn't support additional data");
            }
        }
    }

    /// <inheritdoc/>
    public sbyte? GetSbyteValue() => sbyte.TryParse(DecodedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    /// <inheritdoc/>
    public string GetStringValue() => DecodedValue;

    /// <inheritdoc/>
    public TimeSpan? GetTimeSpanValue() {
        var rawString = DecodedValue;
        if(string.IsNullOrEmpty(rawString)) {
            return null;
        }

        // Parse an ISO8601 duration.http://en.wikipedia.org/wiki/ISO_8601#Durations to a TimeSpan
        return XmlConvert.ToTimeSpan(rawString);
    }

    /// <inheritdoc/>
    public TimeOnly? GetTimeValue() => DateTime.TryParse(DecodedValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? TimeOnly.FromDateTime(result) : null;

    private static bool? GetBoolValue(string value) => bool.TryParse(value, out var result) && result;
    private static byte? GetByteValue(string value) => byte.TryParse(value, out var result) ? result : null;
    private static sbyte? GetSbyteValue(string value) => sbyte.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static int? GetIntValue(string value) => int.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static long? GetLongValue(string value) => long.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static float? GetFloatValue(string value) => float.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static double? GetDoubleValue(string value) => double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static decimal? GetDecimalValue(string value) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;
    private static Guid? GetGuidValue(string value) => Guid.TryParse(value, out var result) ? result : null;
    private static DateTimeOffset? GetDateTimeOffsetValue(string value) => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result : null;
    private static DateOnly? GetDateValue(string value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? DateOnly.FromDateTime(result) : null;
    private static TimeOnly? GetTimeValue(string value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? TimeOnly.FromDateTime(result) : null;
    private static TimeSpan? GetTimeSpanValue(string value) {
        if(string.IsNullOrEmpty(value)) {
            return null;
        }

        // Parse an ISO8601 duration.http://en.wikipedia.org/wiki/ISO_8601#Durations to a TimeSpan
        return XmlConvert.ToTimeSpan(value);
    }

    /// <inheritdoc/>
#if NET5_0_OR_GREATER
    public IEnumerable<T?> GetCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
#else
    public IEnumerable<T?> GetCollectionOfEnumValues<T>() where T : struct, Enum
#endif
    {
        foreach(var v in DecodedValue.Split(_ComaSeparator, StringSplitOptions.RemoveEmptyEntries)) {
            yield return EnumHelpers.GetEnumValue<T>(v);
        }
    }

    /// <inheritdoc/>
#if NET5_0_OR_GREATER
    public T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum
#else
    public T? GetEnumValue<T>() where T : struct, Enum
#endif
    {
        return EnumHelpers.GetEnumValue<T>(DecodedValue);
    }
}
