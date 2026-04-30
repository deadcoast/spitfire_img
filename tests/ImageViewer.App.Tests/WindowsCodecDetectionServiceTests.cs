using ImageViewer.App;
using ImageViewer.App.Services;
using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageViewer.App.Tests;

public sealed class WindowsCodecDetectionServiceTests
{
    private static readonly DateTimeOffset FixedDetectionTime = new(2026, 4, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task DetectCodecsAsync_ChecksHeicAndAvifIndependently()
    {
        var probe = new RecordingCodecRegistryProbe(heicSupported: true, avifSupported: false);
        var service = new WindowsCodecDetectionService(probe, () => FixedDetectionTime);

        var result = await service.DetectCodecsAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HeicSupported);
        Assert.False(result.Value.AvifSupported);
        Assert.Equal(FixedDetectionTime, result.Value.DetectionTime);
        Assert.Equal(1, probe.HeicChecks);
        Assert.Equal(1, probe.AvifChecks);
    }

    [Fact]
    public async Task DetectCodecsAsync_CachesAvailabilityForSession()
    {
        var probe = new RecordingCodecRegistryProbe(heicSupported: true, avifSupported: true);
        var service = new WindowsCodecDetectionService(probe, () => FixedDetectionTime);

        var first = await service.DetectCodecsAsync();
        var second = await service.DetectCodecsAsync();
        var heic = service.IsHeicSupported();
        var avif = service.IsAvifSupported();

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Same(first.Value, second.Value);
        Assert.True(heic.IsSuccess);
        Assert.True(heic.Value);
        Assert.True(avif.IsSuccess);
        Assert.True(avif.Value);
        Assert.Equal(1, probe.HeicChecks);
        Assert.Equal(1, probe.AvifChecks);
    }

    [Fact]
    public async Task CodecDetectionCaching_Property_ReusesCachedResultForRandomCheckSequences()
    {
        var random = new Random(97531);

        for (var iteration = 0; iteration < 120; iteration++)
        {
            var heicSupported = random.Next(2) == 0;
            var avifSupported = random.Next(2) == 0;
            var timeCalls = 0;
            var detectionTime = FixedDetectionTime.AddMinutes(iteration);
            var probe = new RecordingCodecRegistryProbe(heicSupported, avifSupported);
            var service = new WindowsCodecDetectionService(
                probe,
                () =>
                {
                    timeCalls++;
                    return detectionTime.AddSeconds(timeCalls);
                });

            var first = await service.DetectCodecsAsync();
            Assert.True(first.IsSuccess);

            var operationCount = random.Next(2, 40);
            for (var operation = 0; operation < operationCount; operation++)
            {
                switch ((CodecCheckOperation)random.Next(3))
                {
                    case CodecCheckOperation.DetectAll:
                        var detected = await service.DetectCodecsAsync();
                        Assert.True(detected.IsSuccess);
                        Assert.Equal(first.Value, detected.Value);
                        Assert.Equal(first.Value.DetectionTime, detected.Value.DetectionTime);
                        break;
                    case CodecCheckOperation.CheckHeic:
                        var heic = service.IsHeicSupported();
                        Assert.True(heic.IsSuccess);
                        Assert.Equal(heicSupported, heic.Value);
                        break;
                    case CodecCheckOperation.CheckAvif:
                        var avif = service.IsAvifSupported();
                        Assert.True(avif.IsSuccess);
                        Assert.Equal(avifSupported, avif.Value);
                        break;
                }
            }

            var last = await service.DetectCodecsAsync();
            Assert.Equal(first.Value, last.Value);
            Assert.Equal(first.Value.DetectionTime, last.Value.DetectionTime);
            Assert.Equal(1, probe.HeicChecks);
            Assert.Equal(1, probe.AvifChecks);
            Assert.Equal(1, timeCalls);
        }
    }

    [Fact]
    public async Task DetectCodecsAsync_TreatsRegistryAccessErrorsAsUnavailable()
    {
        var probe = new ThrowingCodecRegistryProbe();
        var service = new WindowsCodecDetectionService(probe, () => FixedDetectionTime);

        var result = await service.DetectCodecsAsync();

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HeicSupported);
        Assert.False(result.Value.AvifSupported);
        Assert.Equal(FixedDetectionTime, result.Value.DetectionTime);
    }

    [Fact]
    public void StoreUrls_ReturnMicrosoftStoreProtocolLinks()
    {
        var service = new WindowsCodecDetectionService(new RecordingCodecRegistryProbe(), () => FixedDetectionTime);

        var heicUrl = service.GetHeicStoreUrl();
        var avifUrl = service.GetAvifStoreUrl();

        Assert.True(heicUrl.IsSuccess);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9PMMSR1CGPWG", heicUrl.Value);
        Assert.True(avifUrl.IsSuccess);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9MVZQVXJBQ9V", avifUrl.Value);
    }

    [Fact]
    public void IsSupportedMethods_ReturnFailureBeforeStartupDetection()
    {
        var service = new WindowsCodecDetectionService(new RecordingCodecRegistryProbe(), () => FixedDetectionTime);

        var heic = service.IsHeicSupported();
        var avif = service.IsAvifSupported();

        Assert.True(heic.IsFailure);
        Assert.Equal(ErrorCategory.CodecDetectionError, heic.Error.Category);
        Assert.True(avif.IsFailure);
        Assert.Equal(ErrorCategory.CodecDetectionError, avif.Error.Category);
    }

    [Fact]
    public async Task InitializeStartupServicesAsync_DetectsCodecsOnceAtStartup()
    {
        var service = new RecordingCodecDetectionService();
        var provider = new ServiceCollection()
            .AddSingleton<ICodecDetectionService>(service)
            .BuildServiceProvider();

        await App.InitializeStartupServicesAsync(provider);

        Assert.Equal(1, service.DetectCalls);
    }

    [Fact]
    public async Task WindowsImageDecodeService_HeicWithCodecAvailable_UsesWindowsMetadataReader()
    {
        var filePath = @"C:\Photos\phone.heic";
        var codecService = new RecordingCodecDetectionService(heicSupported: true);
        var reader = new RecordingWindowsImageMetadataReader(
            Result<ImageMetadata>.Success(new ImageMetadata(4032, 3024, 123456)));
        var service = new WindowsImageDecodeService(codecService, reader, new ImageViewer.Core.Services.ImageDecodeService());

        var result = await service.DecodeImageAsync(filePath);

        Assert.True(result.IsSuccess);
        Assert.Equal(filePath, result.Value.FilePath);
        Assert.Equal(4032, result.Value.Metadata.PixelWidth);
        Assert.Equal(3024, result.Value.Metadata.PixelHeight);
        Assert.Equal([filePath], reader.ReadFilePaths);
        Assert.True(service.IsSupportedFormat(".heic"));
    }

    [Fact]
    public async Task WindowsImageDecodeService_HeicWithoutCodec_ReturnsCodecUnavailable()
    {
        var codecService = new RecordingCodecDetectionService(heicSupported: false);
        var reader = new RecordingWindowsImageMetadataReader(Result<ImageMetadata>.Failure(
            ViewerError.DecodeError("ReadMetadata", @"C:\Photos\phone.heic")));
        var service = new WindowsImageDecodeService(codecService, reader, new ImageViewer.Core.Services.ImageDecodeService());

        var result = await service.DecodeImageAsync(@"C:\Photos\phone.heic");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Empty(reader.ReadFilePaths);
        Assert.False(service.IsSupportedFormat(".heic"));
    }

    [Fact]
    public async Task WindowsImageDecodeService_AvifWithCodecAvailable_UsesWindowsMetadataReader()
    {
        var filePath = @"C:\Photos\modern.avif";
        var codecService = new RecordingCodecDetectionService(avifSupported: true);
        var reader = new RecordingWindowsImageMetadataReader(
            Result<ImageMetadata>.Success(new ImageMetadata(3840, 2160, 456789)));
        var service = new WindowsImageDecodeService(codecService, reader, new ImageViewer.Core.Services.ImageDecodeService());

        var result = await service.DecodeImageAsync(filePath);

        Assert.True(result.IsSuccess);
        Assert.Equal(filePath, result.Value.FilePath);
        Assert.Equal(3840, result.Value.Metadata.PixelWidth);
        Assert.Equal(2160, result.Value.Metadata.PixelHeight);
        Assert.Equal([filePath], reader.ReadFilePaths);
        Assert.True(service.IsSupportedFormat(".avif"));
    }

    [Fact]
    public async Task WindowsImageDecodeService_AvifWithoutCodec_ReturnsCodecUnavailable()
    {
        var codecService = new RecordingCodecDetectionService(avifSupported: false);
        var reader = new RecordingWindowsImageMetadataReader(Result<ImageMetadata>.Failure(
            ViewerError.DecodeError("ReadMetadata", @"C:\Photos\modern.avif")));
        var service = new WindowsImageDecodeService(codecService, reader, new ImageViewer.Core.Services.ImageDecodeService());

        var result = await service.DecodeImageAsync(@"C:\Photos\modern.avif");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCategory.CodecUnavailableError, result.Error.Category);
        Assert.Empty(reader.ReadFilePaths);
        Assert.False(service.IsSupportedFormat(".avif"));
    }

    [Fact]
    public void WindowsImageDecodeService_ReportsHeicAndAvifSupportIndependently()
    {
        var codecService = new RecordingCodecDetectionService(heicSupported: true, avifSupported: false);
        var reader = new RecordingWindowsImageMetadataReader(
            Result<ImageMetadata>.Success(new ImageMetadata(1, 1, 1)));
        var service = new WindowsImageDecodeService(codecService, reader, new ImageViewer.Core.Services.ImageDecodeService());

        Assert.True(service.IsSupportedFormat(".heic"));
        Assert.False(service.IsSupportedFormat(".avif"));
    }

    private sealed class RecordingCodecRegistryProbe : ICodecRegistryProbe
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;

        public RecordingCodecRegistryProbe(bool heicSupported = false, bool avifSupported = false)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
        }

        public int HeicChecks { get; private set; }

        public int AvifChecks { get; private set; }

        public bool IsHeicCodecRegistered()
        {
            HeicChecks++;
            return _heicSupported;
        }

        public bool IsAvifCodecRegistered()
        {
            AvifChecks++;
            return _avifSupported;
        }
    }

    private enum CodecCheckOperation
    {
        DetectAll,
        CheckHeic,
        CheckAvif
    }

    private sealed class ThrowingCodecRegistryProbe : ICodecRegistryProbe
    {
        public bool IsHeicCodecRegistered()
        {
            throw new UnauthorizedAccessException("registry denied");
        }

        public bool IsAvifCodecRegistered()
        {
            throw new IOException("registry unavailable");
        }
    }

    private sealed class RecordingCodecDetectionService : ICodecDetectionService
    {
        private readonly bool _heicSupported;
        private readonly bool _avifSupported;

        public RecordingCodecDetectionService(bool heicSupported = false, bool avifSupported = false)
        {
            _heicSupported = heicSupported;
            _avifSupported = avifSupported;
        }

        public int DetectCalls { get; private set; }

        public Task<Result<CodecAvailability>> DetectCodecsAsync(CancellationToken cancellationToken = default)
        {
            DetectCalls++;
            return Task.FromResult(Result<CodecAvailability>.Success(
                new CodecAvailability(_heicSupported, _avifSupported, FixedDetectionTime)));
        }

        public Result<bool> IsHeicSupported()
        {
            return Result<bool>.Success(_heicSupported);
        }

        public Result<bool> IsAvifSupported()
        {
            return Result<bool>.Success(_avifSupported);
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

    private sealed class RecordingWindowsImageMetadataReader : IWindowsImageMetadataReader
    {
        private readonly Result<ImageMetadata> _result;

        public RecordingWindowsImageMetadataReader(Result<ImageMetadata> result)
        {
            _result = result;
        }

        public List<string> ReadFilePaths { get; } = [];

        public Task<Result<ImageMetadata>> ReadMetadataAsync(
            string filePath,
            CancellationToken cancellationToken = default)
        {
            ReadFilePaths.Add(filePath);
            return Task.FromResult(_result);
        }
    }
}
