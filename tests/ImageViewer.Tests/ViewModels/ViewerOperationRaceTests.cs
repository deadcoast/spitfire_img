using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.ViewModels;

public sealed class ViewerOperationRaceTests
{
    [Fact]
    public async Task OpenFile_WhenOlderDecoderFinishesLate_KeepsNewerImageAndPlaylist()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "operation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var first = Path.Combine(directory, "first.png");
            var second = Path.Combine(directory, "second.png");
            await File.WriteAllBytesAsync(first, [1]);
            await File.WriteAllBytesAsync(second, [2]);
            var decoder = new DeferredDecoder(first);
            var viewer = CreateViewer(directory, decoder);
            var oldOpen = viewer.OpenFileAsync(first);
            await decoder.Started.Task;
            await viewer.OpenFileAsync(second);
            decoder.Release.TrySetResult();
            await oldOpen;

            Assert.Equal(second, viewer.ViewerState.CurrentImage?.FilePath);
            Assert.Equal(second, viewer.PlaylistState.CurrentItem?.FilePath);
            Assert.Null(viewer.ErrorMessage);
            Assert.False(viewer.IsLoading);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task CancelLoading_WhenDecoderThrowsCancellation_DoesNotBecomeUnhandledUiException()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "operation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var file = Path.Combine(directory, "image.png");
            await File.WriteAllBytesAsync(file, [1]);
            var decoder = new DeferredDecoder(file, honorCancellation: true);
            var viewer = CreateViewer(directory, decoder);
            var open = viewer.OpenFileAsync(file);
            await decoder.Started.Task;
            viewer.CancelLoading();
            await open;
            Assert.Null(viewer.ViewerState.CurrentImage);
            Assert.Null(viewer.ErrorMessage);
            Assert.False(viewer.IsLoading);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveTransforms_SerializesEditsAndDoesNotReplaceNewerNavigation(bool navigateDuringSave)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "operation-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var first = Path.Combine(directory, "first.png");
            var second = Path.Combine(directory, "second.png");
            await File.WriteAllBytesAsync(first, [1]);
            await File.WriteAllBytesAsync(second, [2]);
            var transforms = new DeferredTransformService();
            var viewer = CreateViewer(directory, new DeferredDecoder(string.Empty), transforms);
            await viewer.OpenFileAsync(first);
            await viewer.ApplyTransformAsync(TransformOperation.RotateRight);
            var save = viewer.SaveTransformsAsync();
            await transforms.Started.Task;
            Assert.True(viewer.IsSavingTransforms);
            await viewer.SaveTransformsAsync();
            await viewer.ApplyTransformAsync(TransformOperation.FlipHorizontal);
            viewer.UndoTransform();
            Assert.Equal([TransformOperation.RotateRight], viewer.TransformState.PendingTransforms);
            if (navigateDuringSave) await viewer.OpenFileAsync(second);
            transforms.Release.TrySetResult();
            await save;

            Assert.Equal(1, transforms.SaveCount);
            Assert.False(viewer.IsSavingTransforms);
            Assert.False(viewer.TransformState.HasUnsavedChanges);
            Assert.Equal(navigateDuringSave ? second : first, viewer.ViewerState.CurrentImage?.FilePath);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void ReloadImage_WithEqualMetadata_NotifiesRendererOfChangedPixels()
    {
        var state = new ViewerState();
        state.SetImage(new DecodedImage("image.bmp", new ImageMetadata(100, 100, 40000)));
        var notifications = 0;
        state.PropertyChanged += (_, args) => { if (args.PropertyName == nameof(ViewerState.CurrentImage)) notifications++; };

        state.SetImage(new DecodedImage("image.bmp", new ImageMetadata(100, 100, 40000)));

        Assert.Equal(1, notifications);
    }

    private static MainViewModel CreateViewer(string directory, IImageDecodeService decoder, ITransformService? transforms = null) =>
        new(new FileOpenService(decoder), decoder, new FolderPlaylistService(decoder), new NavigationService(decoder),
            new SettingsService(directory), new ErrorLogService(directory), new SearchService(),
            new ThumbnailService(decoder), new RecentFilesService(directory), new ExportService(), transforms ?? new TransformService(), new MetadataService());

    private sealed class DeferredTransformService : ITransformService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int SaveCount { get; private set; }

        public Task<Result<TransformedImage>> ApplyTransformAsync(DecodedImage sourceImage,
            IReadOnlyList<TransformOperation> operations, CancellationToken cancellationToken = default) =>
            new TransformService().ApplyTransformAsync(sourceImage, operations, cancellationToken);

        public async Task<Result> SaveTransformsAsync(string filePath,
            IReadOnlyList<TransformOperation> operations, CancellationToken cancellationToken = default)
        {
            SaveCount++;
            Started.TrySetResult();
            await Release.Task;
            return Result.Success();
        }
    }

    private sealed class DeferredDecoder(string delayedFile, bool honorCancellation = false) : IImageDecodeService
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<Result<DecodedImage>> DecodeImageAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (filePath == delayedFile)
            {
                Started.TrySetResult();
                if (honorCancellation) await Release.Task.WaitAsync(cancellationToken);
                else await Release.Task;
            }
            return Result<DecodedImage>.Success(new DecodedImage(filePath, new ImageMetadata(100, 100, 1)));
        }
        public bool IsSupportedFormat(string extension) => ImageFormatCatalog.IsSupportedFormat(extension);
        public bool IsDeferredFormat(string extension) => ImageFormatCatalog.IsDeferredFormat(extension);
        public Task<Result<int>> GetFrameCountAsync(string filePath, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<int>.Success(1));
    }
}
