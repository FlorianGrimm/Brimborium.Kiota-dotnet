using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using Microsoft.IO;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Extensions;
using Microsoft.Kiota.Abstractions.Helpers;
using Microsoft.Kiota.Abstractions.Serialization;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Microsoft.Kiota.Serialization.Text;

/// <summary>
/// The <see cref="ISerializationWriter"/> implementation for text content types.
/// </summary>
public class TextSerializationWriter : ISerializationWriter, IDisposable
{
    private static readonly RecyclableMemoryStreamManager _MemoryStreamManager = new RecyclableMemoryStreamManager();
    private readonly RecyclableMemoryStream _Stream = _MemoryStreamManager.GetStream();
    private readonly StreamWriter _Writer;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextSerializationWriter"/> class.
    /// </summary>
    public TextSerializationWriter()
    {
        _Writer = new(
            _Stream,
            // Default encoding
            encoding: new System.Text.UTF8Encoding(false, true),
            // Default buffer size
            bufferSize: 1024,
            leaveOpen: true);
    }
    private bool _Written;

    /// <inheritdoc />
    public Action<IParsable>? OnBeforeObjectSerialization { get; set; }
    /// <inheritdoc />
    public Action<IParsable>? OnAfterObjectSerialization { get; set; }
    /// <inheritdoc />
    public Action<IParsable, ISerializationWriter>? OnStartObjectSerialization { get; set; }
    /// <inheritdoc />
    public void Dispose()
    {
        _Writer?.Dispose();
        GC.SuppressFinalize(this);
    }
    /// <inheritdoc />
    public Stream GetSerializedContent()
    {
        _Writer.Flush();
        _Stream.Position = 0;
        return _Stream;
    }
    /// <inheritdoc />
    public void WriteAdditionalData(IDictionary<string, object> value) => throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
    /// <inheritdoc />
    public void WriteBoolValue(string? key, bool? value) => WriteStringValue(key, value?.ToString());
    /// <inheritdoc />
    public void WriteByteArrayValue(string? key, byte[]? value) => WriteStringValue(key, value?.Length > 0 ? Convert.ToBase64String(value) : string.Empty);
    /// <inheritdoc />
    public void WriteByteValue(string? key, byte? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteCollectionOfObjectValues<T>(string? key, IEnumerable<T>? values) where T : IParsable => throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
    /// <inheritdoc />
    public void WriteCollectionOfPrimitiveValues<T>(string? key, IEnumerable<T>? values) => throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
    /// <inheritdoc />
    public void WriteDateTimeOffsetValue(string? key, DateTimeOffset? value) => WriteStringValue(key, value.HasValue ? value.Value.ToString("o", CultureInfo.InvariantCulture) : null);
    /// <inheritdoc />
    public void WriteDateValue(string? key, DateOnly? value) => WriteStringValue(key, value?.ToString());
    /// <inheritdoc />
    public void WriteDecimalValue(string? key, decimal? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteDoubleValue(string? key, double? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteFloatValue(string? key, float? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteGuidValue(string? key, Guid? value) => WriteStringValue(key, value?.ToString());
    /// <inheritdoc />
    public void WriteIntValue(string? key, int? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteLongValue(string? key, long? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteNullValue(string? key) => WriteStringValue(key, "null");
    /// <inheritdoc />
    public void WriteObjectValue<T>(string? key, T? value, params IParsable?[] additionalValuesToMerge) where T : IParsable => throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
    /// <inheritdoc />
    public void WriteSbyteValue(string? key, sbyte? value) => WriteStringValue(key, value?.ToString(CultureInfo.InvariantCulture));
    /// <inheritdoc />
    public void WriteStringValue(string? key, string? value)
    {
        if(!string.IsNullOrEmpty(key)) {
            throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
        }

        if(!string.IsNullOrEmpty(value)) {
            if(_Written) {
                throw new InvalidOperationException("a value was already written for this serialization writer, text content only supports a single value");
            }
            else
            {
                _Writer.Write(value);
                _Written = true;
            }
        }
    }
    /// <inheritdoc />
    public void WriteTimeSpanValue(string? key, TimeSpan? value) => WriteStringValue(key, value.HasValue ? XmlConvert.ToString(value.Value) : null);
    /// <inheritdoc />
    public void WriteTimeValue(string? key, TimeOnly? value) => WriteStringValue(key, value?.ToString());
    /// <inheritdoc />
#if NET5_0_OR_GREATER
    public void WriteCollectionOfEnumValues<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, IEnumerable<T?>? values) where T : struct, Enum
#else
    public void WriteCollectionOfEnumValues<T>(string? key, IEnumerable<T?>? values) where T : struct, Enum
#endif
    => throw new InvalidOperationException(TextParseNode.NoStructuredDataMessage);
    /// <inheritdoc />
#if NET5_0_OR_GREATER
    public void WriteEnumValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>(string? key, T? value) where T : struct, Enum
#else
    public void WriteEnumValue<T>(string? key, T? value) where T : struct, Enum
#endif
    => WriteStringValue(key, value.HasValue ? EnumHelpers.GetEnumStringValue(value.Value) : null);
}
