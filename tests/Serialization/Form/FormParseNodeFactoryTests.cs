using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Kiota.Serialization.Form.Tests;

public class FormParseNodeFactoryTests
{
    private readonly FormParseNodeFactory _FormParseNodeFactory = new();
    private const string _TestJsonString = "key=value";

    [Fact]
    public async Task GetsWriterForFormContentType()
    {
        var formStream = new MemoryStream(Encoding.UTF8.GetBytes(_TestJsonString));
        var formParseNode = await _FormParseNodeFactory.GetRootParseNodeAsync(_FormParseNodeFactory.ValidContentType, formStream, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(formParseNode.RootNode);
        Assert.IsAssignableFrom<FormParseNode>(formParseNode.RootNode);

        formParseNode.Dispose();
    }
    [Fact]
    public async Task ThrowsArgumentOutOfRangeExceptionForInvalidContentType()
    {
        var streamContentType = "application/octet-stream";
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(_TestJsonString));
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await _FormParseNodeFactory.GetRootParseNodeAsync(streamContentType, jsonStream, TestContext.Current.CancellationToken));

        // Assert
        Assert.NotNull(exception);
        Assert.Equal($"expected a {_FormParseNodeFactory.ValidContentType} content type", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ThrowsArgumentNullExceptionForNoContentType(string? contentType)
    {
        using var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(_TestJsonString));
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await _FormParseNodeFactory.GetRootParseNodeAsync(contentType!, jsonStream, TestContext.Current.CancellationToken));

        // Assert
        Assert.NotNull(exception);
        Assert.Equal("contentType", exception.ParamName);
    }
}
