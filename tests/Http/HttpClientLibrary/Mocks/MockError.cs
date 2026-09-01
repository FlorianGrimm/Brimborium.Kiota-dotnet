using System;
using System.Collections.Generic;
using Brimborium.Kiota.Abstractions;
using Brimborium.Kiota.Abstractions.Serialization;

namespace Brimborium.Kiota.Http.HttpClientLibrary.Tests.Mocks;

public class MockError(string message) : ApiException(message), IParsable
{
    public IDictionary<string, Action<IParseNode>> GetFieldDeserializers()
    {
        return new Dictionary<string, Action<IParseNode>>();
    }

    public void Serialize(ISerializationWriter writer)
    {
    }
}

