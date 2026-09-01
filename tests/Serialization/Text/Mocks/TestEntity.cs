using System;
using System.Collections.Generic;
using Brimborium.Kiota.Abstractions.Serialization;

namespace Brimborium.Kiota.Serialization.Text.Tests.Mocks
{
    public class TestEntity : IParsable
    {
        public IDictionary<string, Action<IParseNode>> GetFieldDeserializers() => throw new NotImplementedException();
        public void Serialize(ISerializationWriter writer) => throw new NotImplementedException();
    }
}
