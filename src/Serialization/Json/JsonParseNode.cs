// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Xml;

namespace Microsoft.Kiota.Serialization.Json {

    /// <summary>
    /// The <see cref="IParseNode"/> implementation for the json content type
    /// </summary>
    public class JsonParseNode : IParseNode {
        private JsonElement _JsonNode;
        private readonly KiotaJsonSerializationContext _JsonSerializerContext;

        /// <summary>
        /// The <see cref="JsonParseNode"/> constructor.
        /// </summary>
        /// <param name="node">The JsonElement to initialize the node with</param>
        public JsonParseNode(JsonElement node)
            : this(node, KiotaJsonSerializationContext.Default) {
        }

        /// <summary>
        /// The <see cref="JsonParseNode"/> constructor.
        /// </summary>
        /// <param name="node">The JsonElement to initialize the node with</param>
        /// <param name="jsonSerializerContext">The JsonSerializerContext to utilize.</param>
        public JsonParseNode(JsonElement node, KiotaJsonSerializationContext jsonSerializerContext) {
            this._JsonNode = node;
            this._JsonSerializerContext = jsonSerializerContext;
        }

        /// <summary>
        /// Get the string value from the json node
        /// </summary>
        /// <returns>A string value</returns>
        public string? GetStringValue() => this.GetStringValue(this._JsonNode);

        /// <summary>
        /// Get the boolean value from the json node
        /// </summary>
        /// <returns>A boolean value</returns>
        public bool? GetBoolValue() => this.GetBoolValue(this._JsonNode);

        /// <summary>
        /// Get the byte value from the json node
        /// </summary>
        /// <returns>A byte value</returns>
        public byte? GetByteValue() => this.GetByteValue(this._JsonNode);

        /// <summary>
        /// Get the sbyte value from the json node
        /// </summary>
        /// <returns>A sbyte value</returns>
        public sbyte? GetSbyteValue() => this.GetSbyteValue(this._JsonNode);

        /// <summary>
        /// Get the int value from the json node
        /// </summary>
        /// <returns>A int value</returns>
        public int? GetIntValue() => this.GetIntValue(this._JsonNode);

        /// <summary>
        /// Get the float value from the json node
        /// </summary>
        /// <returns>A float value</returns>
        public float? GetFloatValue() => this.GetFloatValue(this._JsonNode);

        /// <summary>
        /// Get the Long value from the json node
        /// </summary>
        /// <returns>A Long value</returns>
        public long? GetLongValue() => this.GetLongValue(this._JsonNode);

        /// <summary>
        /// Get the double value from the json node
        /// </summary>
        /// <returns>A double value</returns>
        public double? GetDoubleValue() => this.GetDoubleValue(this._JsonNode);

        /// <summary>
        /// Get the decimal value from the json node
        /// </summary>
        /// <returns>A decimal value</returns>
        public decimal? GetDecimalValue() => this.GetDecimalValue(this._JsonNode);

        private static bool ShouldBeDeserializableNumber(JsonValueKind valueKind, JsonNumberHandling numberHandling) => valueKind switch {
            JsonValueKind.Number => true,
            JsonValueKind.String when numberHandling.HasFlag(JsonNumberHandling.AllowReadingFromString) => true,
            _ => false
        };

        /// <summary>
        /// Get the guid value from the json node
        /// </summary>
        /// <returns>A guid value</returns>
        public Guid? GetGuidValue() {
            return this.GetGuidValue(this._JsonNode);
        }

        /// <summary>
        /// Get the <see cref="DateTime"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="DateTime"/> value</returns>
        public DateTime? GetDateTimeValue() {
            return this.GetDateTimeValue(this._JsonNode);
        }

        /// <summary>
        /// Get the <see cref="DateTimeOffset"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="DateTimeOffset"/> value</returns>
        public DateTimeOffset? GetDateTimeOffsetValue() {
            return this.GetDateTimeOffsetValue(this._JsonNode);
        }

        /// <summary>
        /// Get the <see cref="TimeSpan"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="TimeSpan"/> value</returns>
        public TimeSpan? GetTimeSpanValue() {
            return GetTimeSpanValue(this._JsonNode);
        }

        /// <summary>
        /// Get the <see cref="DateOnly"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="DateOnly"/> value</returns>
        public DateOnly? GetDateValue() {
            return this.GetDateValue(this._JsonNode);
        }

        /// <summary>
        /// Get the <see cref="TimeOnly"/> value from the json node
        /// </summary>
        /// <returns>A <see cref="TimeOnly"/> value</returns>
        public TimeOnly? GetTimeValue() {
            return this.GetTimeValue(this._JsonNode);
        }

        /// <summary>
        /// Get the enumeration value of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>An enumeration value or null</returns>
        public T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum {
            var rawValue = this._JsonNode.GetString();
            return EnumHelpers.GetEnumValue<T>(rawValue!);
        }

        /// <summary>
        /// Get the collection of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>A collection of objects</returns>
        public IEnumerable<T> GetCollectionOfObjectValues<T>(ParsableFactory<T> factory) where T : IParsable {
            if (this._JsonNode.ValueKind == JsonValueKind.Array) {
                List<T> result = new();
                var enumerator = this._JsonNode.EnumerateArray();
                if (enumerator.MoveNext()) {
                    var currentParseNode = new JsonParseNode(
                        enumerator.Current, this._JsonSerializerContext) {
                        OnAfterAssignFieldValues = this.OnAfterAssignFieldValues,
                        OnBeforeAssignFieldValues = this.OnBeforeAssignFieldValues
                    };

                    {
                        var value = currentParseNode.GetObjectValue<T>(factory);
                        result.Add(value);
                    }

                    while (enumerator.MoveNext()) {
                        currentParseNode._JsonNode = enumerator.Current;
                        {
                            var value = currentParseNode.GetObjectValue<T>(factory);
                            result.Add(value);
                        }
                    }
                }
                return result;
            } else {
                return Array.Empty<T>();
            }
        }

        /// <summary>
        /// Gets the collection of enum values of the node.
        /// </summary>
        /// <returns>The collection of enum values.</returns>
        public IEnumerable<T?> GetCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() where T : struct, Enum {
            if (this._JsonNode.ValueKind == JsonValueKind.Array) {
                var enumerator = this._JsonNode.EnumerateArray();
                while (enumerator.MoveNext()) {
                    yield return GetEnumValue<T>(enumerator.Current);
                }
            }
        }

        /// <summary>
        /// Gets the byte array value of the node.
        /// </summary>
        /// <returns>The byte array value of the node.</returns>
        public byte[]? GetByteArrayValue() {
            if (this._JsonNode.ValueKind is JsonValueKind.String && this._JsonNode.TryGetBytesFromBase64(out var result)) {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Gets the untyped value of the node
        /// </summary>
        /// <returns>The untyped value of the node.</returns>
        private UntypedNode GetUntypedValue() => this.GetUntypedValue(this._JsonNode);


        /// <summary>
        /// Get the collection of primitives of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <returns>A collection of objects</returns>
        public IEnumerable<T> GetCollectionOfPrimitiveValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>() {
            if (this._JsonNode.ValueKind == JsonValueKind.Array) {
                var genericType = typeof(T);
                List<T> result = new();
                foreach (var collectionValue in this._JsonNode.EnumerateArray()) {
                    if (genericType == TypeConstants.BooleanType) {
                        var value = (T)(object)this.GetBoolValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.ByteType) {
                        var value = (T)(object)this.GetByteValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.SbyteType) {
                        var value = (T)(object)this.GetSbyteValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.StringType) {
                        var value = (T)(object)this.GetStringValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.IntType) {
                        var value = (T)(object)this.GetIntValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.FloatType) {
                        var value = (T)(object)this.GetFloatValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.LongType) {
                        var value = (T)(object)this.GetLongValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.DoubleType) {
                        var value = (T)(object)this.GetDoubleValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.GuidType) {
                        var value = (T)(object)this.GetGuidValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.DateTimeOffsetType) {
                        var value = (T)(object)this.GetDateTimeOffsetValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.TimeSpanType) {
                        var value = (T)(object)GetTimeSpanValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.DateType) {
                        var value = (T)(object)this.GetDateValue(collectionValue)!;
                        result.Add(value);
                    } else if (genericType == TypeConstants.TimeType) {
                        var value = (T)(object)this.GetTimeValue(collectionValue)!;
                        result.Add(value);
                    } else if (this.GetStringValue(collectionValue) is { Length: > 0 } rawValue) {
                        var value = (T)EnumHelpers.GetEnumValue(genericType, rawValue)!;
                        result.Add(value);
                    } else {
                        throw new InvalidOperationException($"unknown type for deserialization {genericType.FullName}");
                    }
                }
                return result;
            }
            return Array.Empty<T>();
        }

        private string? GetStringValue(JsonElement jsonElement) => jsonElement.ValueKind == JsonValueKind.String
            ? jsonElement.Deserialize(this._JsonSerializerContext.String)
            : null;

        private bool? GetBoolValue(JsonElement jsonElement) =>
            jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False
                ? jsonElement.Deserialize(this._JsonSerializerContext.Boolean)
                : null;

        private byte? GetByteValue(JsonElement jsonElement) => jsonElement.ValueKind == JsonValueKind.Number
            ? jsonElement.Deserialize(this._JsonSerializerContext.Byte)
            : null;

        private sbyte? GetSbyteValue(JsonElement jsonElement) => jsonElement.ValueKind == JsonValueKind.Number
            ? jsonElement.Deserialize(this._JsonSerializerContext.SByte)
            : null;

        private int? GetIntValue(JsonElement jsonElement) => ShouldBeDeserializableNumber(jsonElement.ValueKind, this._JsonSerializerContext.Options.NumberHandling)
            ? jsonElement.Deserialize(this._JsonSerializerContext.Int32)
            : null;

        private float? GetFloatValue(JsonElement jsonElement) => ShouldBeDeserializableNumber(jsonElement.ValueKind, this._JsonSerializerContext.Options.NumberHandling)
            ? jsonElement.Deserialize(this._JsonSerializerContext.Single)
            : null;

        private long? GetLongValue(JsonElement jsonElement) => ShouldBeDeserializableNumber(jsonElement.ValueKind, this._JsonSerializerContext.Options.NumberHandling)
            ? jsonElement.Deserialize(this._JsonSerializerContext.Int64)
            : null;

        private double? GetDoubleValue(JsonElement jsonElement) => ShouldBeDeserializableNumber(jsonElement.ValueKind, this._JsonSerializerContext.Options.NumberHandling)
            ? jsonElement.Deserialize(this._JsonSerializerContext.Double)
            : null;

        private decimal? GetDecimalValue(JsonElement jsonElement) => ShouldBeDeserializableNumber(jsonElement.ValueKind, this._JsonSerializerContext.Options.NumberHandling)
            ? jsonElement.Deserialize(this._JsonSerializerContext.Decimal)
            : null;

        private Guid? GetGuidValue(JsonElement jsonElement) {
            if (jsonElement.ValueKind != JsonValueKind.String) {
                return null;
            }

            if (jsonElement.TryGetGuid(out var guid)) {
                return guid;
            }

            if (string.IsNullOrEmpty(jsonElement.GetString())) {
                return null;
            }

            return jsonElement.Deserialize(this._JsonSerializerContext.Guid);
        }

        private DateTime? GetDateTimeValue(JsonElement jsonElement) {
            if (jsonElement.ValueKind != JsonValueKind.String) {
                return null;
            }

            if (jsonElement.TryGetDateTime(out var dateTime)) {
                return dateTime;
            }

            if (TryGetUsingTypeInfo(jsonElement, this._JsonSerializerContext.DateTime, out var convertedDateTime)) {
                return convertedDateTime;
            }

            if (DateTime.TryParse(jsonElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)) {
                return dto;
            }

            return null;
        }

        private DateTimeOffset? GetDateTimeOffsetValue(JsonElement jsonElement) {
            if (jsonElement.ValueKind != JsonValueKind.String) {
                return null;
            }

            if (jsonElement.TryGetDateTimeOffset(out var dateTimeOffset)) {
                return dateTimeOffset;
            }

            if (TryGetUsingTypeInfo(jsonElement, this._JsonSerializerContext.DateTimeOffset, out var convertedDateTimeOffset)) {
                return convertedDateTimeOffset;
            }

            if (DateTimeOffset.TryParse(jsonElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto)) {
                return dto;
            }

            return null;
        }

        private static TimeSpan? GetTimeSpanValue(JsonElement jsonElement) {
            var jsonString = jsonElement.GetString();
            if (string.IsNullOrEmpty(jsonString)) {
                return null;
            }

            // Parse an ISO8601 duration.http://en.wikipedia.org/wiki/ISO_8601#Durations to a TimeSpan
            return XmlConvert.ToTimeSpan(jsonString);
        }

        private DateOnly? GetDateValue(JsonElement jsonElement) {
            if (jsonElement.ValueKind != JsonValueKind.String) {
                if (TryGetUsingTypeInfo(jsonElement, this._JsonSerializerContext.DateOnly, out var date)) {
                    return date;
                }
            } else {
                if (DateTime.TryParse(jsonElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)) {
                    return DateOnly.FromDateTime(dt);
                }
            }

            return null;
        }

        private TimeOnly? GetTimeValue(JsonElement jsonElement) {
            if (jsonElement.ValueKind != JsonValueKind.String) {
                if (TryGetUsingTypeInfo(jsonElement, this._JsonSerializerContext.TimeOnly, out var time)) {
                    return time;
                }
            } else {
                if (DateTime.TryParse(jsonElement.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result)) {
                    return TimeOnly.FromDateTime(result);
                }
            }

            return null;
        }

        private static T? GetEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(JsonElement jsonElement) where T : struct, Enum {
            var rawValue = jsonElement.GetString();
            return EnumHelpers.GetEnumValue<T>(rawValue!);
        }

        /// <summary>
        /// Gets the collection of untyped values of the node.
        /// </summary>
        /// <returns>The collection of untyped values.</returns>
        private IEnumerable<UntypedNode> GetCollectionOfUntypedValues(JsonElement jsonNode) {
            if (jsonNode.ValueKind == JsonValueKind.Array) {
                foreach (var collectionValue in jsonNode.EnumerateArray()) {
                    yield return this.GetUntypedValue(collectionValue);
                }
            }
        }

        /// <summary>
        /// Gets the collection of properties in the untyped object.
        /// </summary>
        /// <returns>The collection of properties in the untyped object.</returns>
        private IDictionary<string, UntypedNode> GetPropertiesOfUntypedObject(JsonElement jsonNode) {
            var properties = new Dictionary<string, UntypedNode>();
            if (jsonNode.ValueKind == JsonValueKind.Object) {
                foreach (var objectValue in jsonNode.EnumerateObject()) {
                    properties[objectValue.Name] = this.GetUntypedValue(objectValue.Value);
                }
            }
            return properties;
        }

        private UntypedNode GetUntypedValue(JsonElement jsonNode) => jsonNode.ValueKind switch {
            JsonValueKind.Number when jsonNode.TryGetInt32(out var intValue) => new UntypedInteger(intValue),
            JsonValueKind.Number when jsonNode.TryGetInt64(out var longValue) => new UntypedLong(longValue),
            JsonValueKind.Number when jsonNode.TryGetDecimal(out var decimalValue) => new UntypedDecimal(decimalValue),
            JsonValueKind.Number when jsonNode.TryGetSingle(out var floatValue) => new UntypedFloat(floatValue),
            JsonValueKind.Number when jsonNode.TryGetDouble(out var doubleValue) => new UntypedDouble(doubleValue),
            JsonValueKind.String => new UntypedString(jsonNode.GetString()),
            JsonValueKind.True or JsonValueKind.False => new UntypedBoolean(jsonNode.GetBoolean()),
            JsonValueKind.Array => new UntypedArray(this.GetCollectionOfUntypedValues(jsonNode)),
            JsonValueKind.Object => new UntypedObject(this.GetPropertiesOfUntypedObject(jsonNode)),
            JsonValueKind.Null or JsonValueKind.Undefined => new UntypedNull(),
            _ => throw new InvalidOperationException($"unexpected additional value type during deserialization json kind : {jsonNode.ValueKind}")
        };

        /// <summary>
        /// The action to perform before assigning field values.
        /// </summary>
        public Action<IParsable>? OnBeforeAssignFieldValues { get; set; }

        /// <summary>
        /// The action to perform after assigning field values.
        /// </summary>
        public Action<IParsable>? OnAfterAssignFieldValues { get; set; }

        /// <summary>
        /// Get the object of type <typeparam name="T"/>from the json node
        /// </summary>
        /// <param name="factory">The factory to use to create the model object.</param>
        /// <returns>A object of the specified type</returns>
        public T GetObjectValue<T>(ParsableFactory<T> factory) where T : IParsable {
            // until interface exposes GetUntypedValue()
            var genericType = typeof(T);
            if (genericType == typeof(UntypedNode)) {
                return (T)(object)this.GetUntypedValue();
            }
            var item = factory(this);
            this.OnBeforeAssignFieldValues?.Invoke(item);
            this.AssignFieldValues(item);
            this.OnAfterAssignFieldValues?.Invoke(item);
            return item;
        }
        private void AssignFieldValues<T>(T item) where T : IParsable {
            if (this._JsonNode.ValueKind != JsonValueKind.Object) {
                return;
            }

            IDictionary<string, object>? itemAdditionalData = null;
            if (item is IAdditionalDataHolder holder) {
                holder.AdditionalData ??= new Dictionary<string, object>();
                itemAdditionalData = holder.AdditionalData;
            }

            var fieldDeserializers = item.GetFieldDeserializers();
            JsonParseNode? jsonParseNode = null;
            foreach (var fieldValue in this._JsonNode.EnumerateObject()) {
                if (fieldDeserializers.TryGetValue(fieldValue.Name, out var fieldDeserializer)) {
                    if (fieldValue.Value.ValueKind == JsonValueKind.Null) {
                        continue;// If the property is already null just continue. As calling functions like GetDouble,GetBoolValue do not process JsonValueKind.Null.
                    }

                    // Debug.WriteLine($"found property {fieldValue.Name} to deserialize");
                    if (jsonParseNode is null) {
                        jsonParseNode = new JsonParseNode(fieldValue.Value, this._JsonSerializerContext) {
                            OnBeforeAssignFieldValues = this.OnBeforeAssignFieldValues,
                            OnAfterAssignFieldValues = this.OnAfterAssignFieldValues
                        };
                    } else { 
                        jsonParseNode._JsonNode = fieldValue.Value;
                    }
                    fieldDeserializer.Invoke(jsonParseNode);
                } else if (itemAdditionalData != null) {
                    // Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize");
                    _ = IDictionaryExtensions.TryAdd(itemAdditionalData, fieldValue.Name, this.TryGetAnything(fieldValue.Value)!);
                } else {
                    // Debug.WriteLine($"found additional property {fieldValue.Name} to deserialize but the model doesn't support additional data");
                }
            }
        }

        private object? TryGetAnything(JsonElement element) {
            switch (element.ValueKind) {
                case JsonValueKind.Number:
                    if (element.TryGetDecimal(out var dec)) {
                        return dec;
                    } else if (element.TryGetDouble(out var db)) {
                        return db;
                    } else if (element.TryGetInt16(out var s)) {
                        return s;
                    } else if (element.TryGetInt32(out var i)) {
                        return i;
                    } else if (element.TryGetInt64(out var l)) {
                        return l;
                    } else if (element.TryGetSingle(out var f)) {
                        return f;
                    } else if (element.TryGetUInt16(out var us)) {
                        return us;
                    } else if (element.TryGetUInt32(out var ui)) {
                        return ui;
                    } else if (element.TryGetUInt64(out var ul)) {
                        return ul;
                    } else {
                        throw new InvalidOperationException("unexpected additional value type during number deserialization");
                    }

                case JsonValueKind.String:
                    if (element.TryGetDateTime(out var dt)) {
                        return dt;
                    } else if (element.TryGetDateTimeOffset(out var dto)) {
                        return dto;
                    } else if (element.TryGetGuid(out var g)) {
                        return g;
                    } else {
                        return element.GetString();
                    }

                case JsonValueKind.Array:
                case JsonValueKind.Object:
                    return this.GetUntypedValue(element);
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                default:
                    throw new InvalidOperationException($"unexpected additional value type during deserialization json kind : {element.ValueKind}");
            }
        }

        /// <summary>
        /// Get the child node of the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier of the child node</param>
        /// <returns>An instance of <see cref="IParseNode"/></returns>
        public IParseNode? GetChildNode(string identifier) {
            if (this._JsonNode.ValueKind == JsonValueKind.Object && this._JsonNode.TryGetProperty(identifier ?? throw new ArgumentNullException(nameof(identifier)), out var jsonElement)) {
                return new JsonParseNode(jsonElement, this._JsonSerializerContext) {
                    OnBeforeAssignFieldValues = this.OnBeforeAssignFieldValues,
                    OnAfterAssignFieldValues = this.OnAfterAssignFieldValues
                };
            }

            return default;
        }

        private static bool TryGetUsingTypeInfo<T>(JsonElement currentElement, JsonTypeInfo<T>? typeInfo, out T? deserializedValue) {
            try {
                deserializedValue = currentElement.Deserialize(typeInfo!);
                return true;
            } catch (Exception) {
                deserializedValue = default;
                return false;
            }
        }
    }
}
