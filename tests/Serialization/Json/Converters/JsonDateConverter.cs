using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Serialization.Json.Tests.Converters;

/// <summary>
/// Converts a Date object or value to/from JSON.
/// </summary>
public class JsonDateConverter : JsonConverter<DateOnly>
{
    /// <inheritdoc />
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null
            ? new DateOnly()
            : ReadInternal(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        => WriteInternal(writer, value);

    private static DateOnly ReadInternal(ref Utf8JsonReader reader)
        => DateOnly.ParseExact(reader.GetString()!, "dd---MM---yyyy", CultureInfo.InvariantCulture);
    //=> new DateOnly(DateTime.ParseExact(reader.GetString()!, "dd---MM---yyyy", CultureInfo.InvariantCulture));

    private static void WriteInternal(Utf8JsonWriter writer, DateOnly value)
        => writer.WriteStringValue(value.ToString());
}