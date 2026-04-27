using ImageViewer.Core.Models;

namespace ImageViewer.Tests.Models;

public sealed class ResultTests
{
    [Fact]
    public void Success_GenericResult_ExposesValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_GenericResult_ExposesError()
    {
        var error = ViewerError.UnsupportedFormatError("ValidateFile", "image.gif");

        var result = Result<int>.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_NonGenericResult_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }
}
