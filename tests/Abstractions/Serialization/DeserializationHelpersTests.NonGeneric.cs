
using Microsoft.Kiota.Abstractions.Serialization;
using Microsoft.Kiota.Abstractions.Tests.Mocks;
using Moq;

using System.Diagnostics.CodeAnalysis;

using Xunit;

namespace Microsoft.Kiota.Abstractions.Tests.Serialization;

public partial class DeserializationHelpersTests
{

    [Fact]
    [RequiresDynamicCode("testonly")]
    public async Task DeserializesObjectUntypedWithoutReflectionAsync()
    {
        var strValue = "{'id':'123'}";
        var mockParseNode = new Mock<IParseNode>();
        mockParseNode.Setup(x => x.GetObjectValue(It.IsAny<ParsableFactory<TestEntity>>())).Returns(new TestEntity()
        {
            Id = "123"
        });
        var mockJsonParseNodeFactory = new Mock<IParseNodeFactory>();
        mockJsonParseNodeFactory.Setup(x => x.GetRootParseNodeAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new GetRootParseNodeResult(mockParseNode.Object, null, null)));
        mockJsonParseNodeFactory.Setup(x => x.ValidContentType).Returns(_jsonContentType);
        ParseNodeFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[_jsonContentType] = mockJsonParseNodeFactory.Object;

        var result = (TestEntity?)await KiotaSerializer.DeserializeAsync(typeof(TestEntity), _jsonContentType, strValue, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("123", result.Id);
    }

    [Fact]
    [RequiresDynamicCode("DeserializeCollectionAsync")]
    public async Task DeserializesCollectionOfObjectUntypedAsync()
    {
        var strValue = "{'id':'123'}";
        var mockParseNode = new Mock<IParseNode>();
        mockParseNode.Setup(x => x.GetCollectionOfObjectValues(It.IsAny<ParsableFactory<TestEntity>>())).Returns(new List<TestEntity> {
            new TestEntity()
            {
                Id = "123"
            }
        });
        var mockJsonParseNodeFactory = new Mock<IParseNodeFactory>();
        mockJsonParseNodeFactory.Setup(x => x.GetRootParseNodeAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new GetRootParseNodeResult(mockParseNode.Object, null, null)));
        mockJsonParseNodeFactory.Setup(x => x.ValidContentType).Returns(_jsonContentType);
        ParseNodeFactoryRegistry.DefaultInstance.ContentTypeAssociatedFactories[_jsonContentType] = mockJsonParseNodeFactory.Object;

        var result = await KiotaSerializer.DeserializeCollectionAsync(typeof(TestEntity), _jsonContentType, strValue, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(result);
        var first = result.First() as TestEntity;
        Assert.NotNull(first);
        Assert.Equal("123", first.Id);
    }
}
