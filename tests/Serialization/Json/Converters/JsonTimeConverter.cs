using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Serialization.Json.Tests.Converters;

/// <summary>
/// Converts a Time object or value to/from JSON.
/// </summary>
public class JsonTimeConverter : JsonConverter<TimeOnly>
{
    /// <inheritdoc />
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null
            ? new TimeOnly()
            : ReadInternal(ref reader);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => WriteInternal(writer, value);

    private static TimeOnly ReadInternal(ref Utf8JsonReader reader)
        //=> new TimeOnly(DateTime.ParseExact(reader.GetString()!, "HH__mm__ss", CultureInfo.InvariantCulture));
        => TimeOnly.ParseExact(reader.GetString()!, "HH__mm__ss", CultureInfo.InvariantCulture);

    private static void WriteInternal(Utf8JsonWriter writer, TimeOnly value)
        => writer.WriteStringValue(value.ToString());
}