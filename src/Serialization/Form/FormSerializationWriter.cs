using Microsoft.IO;
using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Microsoft.Kiota.Serialization.Form;

/// <summary>Represents a serialization writer that can be used to write a form url encoded string.</summary>
public class FormSerializationWriter : ISerializationWriter {
    private int _Depth;
    private readonly StringBuilder _Builder = new();
    private static readonly RecyclableMemoryStreamManager _MemoryStreamManager = new RecyclableMemoryStreamManager();
    /// <inheritdoc/>
    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }
    /// <inheritdoc/>
    public Action<IParsable>? OnAfterObjectSerialization { get; set; }
    /// <inheritdoc/>
    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }
    /// <inheritdoc/>
    public void Dispose() {
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc/>
    public Stream GetSerializedContent() => _MemoryStreamManager.GetStream(Encoding.UTF8.GetBytes(this._Builder.ToString()));
    /// <inheritdoc/>
    public void WriteAdditionalData(IDictionary<string, object> value) {
        if (value == null) {
            return;
        }

        foreach (var dataValue in value) {
            this.WriteAnyValue(dataValue.Key, dataValue.Value);
        }
    }

    private void WriteAnyValue(string? key, object value) {
        switch (value) {
            case null:
                this.WriteNullValue(key);
                break;
            case decimal d:
                this.WriteDecimalValue(key, d);
                break;
            case bool b:
                this.WriteBoolValue(key, b);
                break;
            case byte b:
                this.WriteByteValue(key, b);
                break;
            case sbyte b:
                this.WriteSbyteValue(key, b);
                break;
            case int i:
                this.WriteIntValue(key, i);
                break;
            case float f:
                this.WriteFloatValue(key, f);
                break;
            case long l:
                this.WriteLongValue(key, l);
                break;
            case double d:
                this.WriteDoubleValue(key, d);
                break;
            case Guid g:
                this.WriteGuidValue(key, g);
                break;
            case DateTimeOffset dto:
                this.WriteDateTimeOffsetValue(key, dto);
                break;
            case TimeSpan timeSpan:
                this.WriteTimeSpanValue(key, timeSpan);
                break;
            case TimeOnly time:
                this.WriteTimeValue(key, time);
                break;
            case IEnumerable<object> coll:
                this.WriteCollectionOfPrimitiveValues(key, coll);
                break;
            case byte[] coll:
                this.WriteByteArrayValue(key, coll);
                break;
            case IParsable:
                throw new InvalidOperationException("Form serialization does not support nested objects.");
            default:
                this.WriteStringValue(key, value.ToString());// works for Date and String types
                break;
        }
    }

    /// <inheritdoc/>
    public void WriteBoolValue(string? key, bool? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString().ToLowerInvariant());
        }
    }

    /// <inheritdoc/>
    public void WriteByteArrayValue(string? key, byte[]? value) {
        if (value != null)//empty array is meaningful
{
            this.WriteStringValue(key, value.Length > 0 ? Convert.ToBase64String(value) : string.Empty);
        }
    }

    /// <inheritdoc/>
    public void WriteByteValue(string? key, byte? value) {
        if (value.HasValue) {
            this.WriteIntValue(key, Convert.ToInt32(value.Value));
        }
    }

    /// <inheritdoc/>
    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values) where T : IParsable => throw new InvalidOperationException("Form serialization does not support collections.");

    /// <inheritdoc/>
    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values) {
        if (values == null) {
            return;
        }

        foreach (var value in values) {
            if (value != null) {
                this.WriteAnyValue(key, value);
            }
        }
    }

    /// <inheritdoc/>
    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString("o", CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteDateValue(string? key, DateOnly? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString());
        }
    }

    /// <inheritdoc/>
    public void WriteDecimalValue(string? key, decimal? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteDoubleValue(string? key, double? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteFloatValue(string? key, float? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteGuidValue(string? key, Guid? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString("D"));
        }
    }

    /// <inheritdoc/>
    public void WriteIntValue(string? key, int? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteLongValue(string? key, long? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc/>
    public void WriteNullValue(string? key) {
        this.WriteStringValue(key, "null");
    }

    /// <inheritdoc/>
    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge) where T : IParsable {
        if (this._Depth > 0) {
            throw new InvalidOperationException("Form serialization does not support nested objects.");
        }

        this._Depth++;
        if (value == null && !Array.Exists(additionalValuesToMerge, static x => x is not null)) {
            return;
        }

        if (value != null) {
            this.OnBeforeObjectSerialization?.Invoke(value);
            this.OnStartObjectSerialization?.Invoke(value, this);
            value.Serialize(this);
        }
        foreach (var additionalValueToMerge in additionalValuesToMerge) {
            if (additionalValueToMerge is null) {
                continue;
            }

            this.OnBeforeObjectSerialization?.Invoke(additionalValueToMerge);
            this.OnStartObjectSerialization?.Invoke(additionalValueToMerge, this);
            additionalValueToMerge.Serialize(this);
            this.OnAfterObjectSerialization?.Invoke(additionalValueToMerge);
        }
        if (value != null) {
            this.OnAfterObjectSerialization?.Invoke(value);
        }
    }

    /// <inheritdoc/>
    public void WriteSbyteValue(string? key, sbyte? value) {
        if (value.HasValue) {
            this.WriteIntValue(key, Convert.ToInt32(value.Value));
        }
    }

    /// <inheritdoc/>
    public void WriteStringValue(string? key, string? value) {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) {
            return;
        }

        if (this._Builder.Length > 0) {
            _ = this._Builder.Append('&');
        }

        _ = this._Builder.Append(Uri.EscapeDataString(key)).Append('=').Append(Uri.EscapeDataString(value));
    }

    /// <inheritdoc/>
    public void WriteTimeSpanValue(string? key, TimeSpan? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, XmlConvert.ToString(value.Value));
        }
    }

    /// <inheritdoc/>
    public void WriteTimeValue(string? key, TimeOnly? value) {
        if (value.HasValue) {
            this.WriteStringValue(key, value.Value.ToString());
        }
    }

    /// <inheritdoc/>
    public void WriteCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, IEnumerable<T?>? values) where T : struct, Enum {
        if (values == null) {
            return;
        }

        StringBuilder? valueNames = null;
        foreach (var x in values) {
            if (x.HasValue && EnumHelpers.GetEnumStringValue(x.Value) is string valueName) {
                if (valueNames == null) {
                    valueNames = new StringBuilder();
                } else {
                    _ = valueNames.Append(",");
                }

                _ = valueNames.Append(valueName);
            }
        }

        if (valueNames is not null) {
            this.WriteStringValue(key, valueNames.ToString());
        }
    }

    /// <inheritdoc/>
    public void WriteEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, T? value) where T : struct, Enum {
        if (value.HasValue) {
            if (typeof(T).IsDefined(typeof(FlagsAttribute))) {
                T[] values = Enum.GetValues<T>();
                StringBuilder valueNames = new StringBuilder();
                foreach (var x in values) {
                    if (value.Value.HasFlag(x) && EnumHelpers.GetEnumStringValue(x) is string valueName) {
                        if (valueNames.Length > 0) {
                            valueNames.Append(",");
                        }

                        valueNames.Append(valueName);
                    }
                }
                this.WriteStringValue(key, valueNames.ToString());
            } else {
                this.WriteStringValue(key, EnumHelpers.GetEnumStringValue(value.Value));
            }
        }
    }
}
