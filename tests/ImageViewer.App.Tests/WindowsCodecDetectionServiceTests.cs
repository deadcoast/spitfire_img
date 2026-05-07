using ImageViewer.App.Services;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageViewer.App.Tests;

public sealed class WindowsCodecDetectionServiceTests
{
    [Fact]
    public async Task DetectCodecsAsync_ChecksHeicAndAvifIndependentlyAndCachesResult()
    {
        var registry = new RecordingCodecRegistry(heicSupported: true, avifSupported: false);
        var detectionTime = new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero);
        var service = new WindowsCodecDetectionService(registry, () => detectionTime);

        var first = await service.DetectCodecsAsync();
        var second = await service.DetectCodecsAsync();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.True(first.Value.HeicSupported);
        Assert.False(first.Value.AvifSupported);
        Assert.Equal(detectionTime, first.Value.DetectionTime);
        Assert.Same(first.Value, second.Value);
        Assert.Equal(1, registry.HeicChecks);
        Assert.Equal(1, registry.AvifChecks);
        Assert.True(service.IsHeicSupported().Value);
        Assert.False(service.IsAvifSupported().Value);
    }

    [Fact]
    public async Task DetectCodecsAsync_WhenRegistryAccessFails_AssumesOnlyThatCodecUnavailable()
    {
        var registry = new RecordingCodecRegistry(
            heicSupported: true,
            avifSupported: true,
            throwOnHeic: true);
        var service = new WindowsCodecDetectionService(registry, () => DateTimeOffset.UnixEpoch);

        var result = await service.DetectCodecsAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HeicSupported);
        Assert.True(result.Value.AvifSupported);
        Assert.Equal(1, registry.HeicChecks);
        Assert.Equal(1, registry.AvifChecks);
    }

    [Fact]
    public void StoreUrls_ReturnDocumentedMicrosoftStoreProtocolUrls()
    {
        var service = new WindowsCodecDetectionService(new RecordingCodecRegistry(), () => DateTimeOffset.UnixEpoch);

        Assert.Equal("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG", service.GetHeicStoreUrl().Value);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V", service.GetAvifStoreUrl().Value);
    }

    [Fact]
    public async Task DetectCodecsOnStartupAsync_InvokesRegisteredCodecDetectionService()
    {
        var codecDetection = new RecordingCodecDetectionService();
        var services = new ServiceCollection()
            .AddSingleton<ICodecDetectionService>(codecDetection)
            .BuildServiceProvider();

        var result = await App.DetectCodecsOnStartupAsync(services);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, codecDetection.DetectCalls);
    }

    private sealed class RecordingCodecDetectionService : ICodecDetectionService
    {
        public int DetectCalls { get; private set; }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            DetectCalls++;
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(false, false, DateTimeOffset.UnixEpoch)));
        }

        public Result<bool> IsHeicSupported()
        {
            return Result<bool>.Success(false);
        }

        public Result<bool> IsAvifSupported()
        {
            return Result<bool>.Success(false);
        }

        public Result<string> GetHeicStoreUrl()
        {
            return Result<string>.Success("heic");
        }

        public Result<string> GetAvifStoreUrl()
        {
            return Result<string>.Success("avif");
        }
    }

    private sealed class RecordingCodecRegistry : IWindowsCodecRegistry
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;
        private readonly bool _throwOnHeic;
        private readonly bool _throwOnAvif;

        public RecordingCodecRegistry(
            bool heicSupported = false,
            bool avifSupported = false,
            bool throwOnHeic = false,
            bool throwOnAvif = false)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
            _throwOnHeic = throwOnHeic;
            _throwOnAvif = throwOnAvif;
        }

        public int HeicChecks { get; private set; }

        public int AvifChecks { get; private set; }

        public bool IsHeicCodecInstalled()
        {
            HeicChecks++;
            return _throwOnHeic
                ? throw new UnauthorizedAccessException("registry denied")
                : _heicSupported;
        }

        public bool IsAvifCodecInstalled()
        {
            AvifChecks++;
            return _throwOnAvif
                ? throw new UnauthorizedAccessException("registry denied")
                : _avifSupported;
        }
    }
}
