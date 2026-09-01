using System;
using System.Collections.Generic;
using Brimborium.Kiota.Abstractions.Serialization;

namespace Brimborium.Kiota.Serialization.Json.Tests.Mocks;

public class ConverterTestEntity : IParsable
{
    public Guid? Id { get; set; }

    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() => new Dictionary<string, Action<IParseNode>>
    {
        { "id", n => Id = n.GetGuidValue() }
    };

    public void Serialize(ISerializationWriter writer)
    {
        _ = writer ?? throw new ArgumentNullException(nameof(writer));
        writer.WriteGuidValue("id", Id);
    }
}