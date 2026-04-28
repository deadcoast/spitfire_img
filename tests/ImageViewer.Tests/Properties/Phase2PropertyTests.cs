using ImageViewer.Core.Models;
using ImageViewer.Core.Services;
using ImageViewer.Core.ViewModels;

namespace ImageViewer.Tests.Properties;

public sealed class Phase2PropertyTests
{
    private readonly Random _random = new(67890);

    [Fact]
    public async Task TransformDimensions_FollowRotationAndFlipRules_ForRandomSequences()
    {
        var service = new TransformService();

        for (var iteration = 0; iteration < 150; iteration++)
        {
            var width = _random.Next(1, 10000);
            var height = _random.Next(1, 10000);
            var expectedWidth = width;
            var expectedHeight = height;
            var operations = RandomOperations(_random.Next(1, 20));

            foreach (var operation in operations)
            {
                if (operation is TransformOperation.RotateLeft or TransformOperation.RotateRight)
                {
                    (expectedWidth, expectedHeight) = (expectedHeight, expectedWidth);
                }
            }

            var image = new DecodedImage(@"C:\Images\property.png", new ImageMetadata(width, height, 10));
            var result = await service.ApplyTransformAsync(image, operations);

            Assert.True(result.IsSuccess);
            Assert.Equal(expectedWidth, result.Value.Metadata.PixelWidth);
            Assert.Equal(expectedHeight, result.Value.Metadata.PixelHeight);
        }
    }

    [Fact]
    public void TransformUndoRedo_RestoresPendingSequence_ForRandomSequences()
    {
        for (var iteration = 0; iteration < 150; iteration++)
        {
            var state = new TransformState();
            var operations = RandomOperations(_random.Next(1, 20));
            foreach (var operation in operations)
            {
                state.ApplyTransform(operation);
            }

            var undoCount = _random.Next(0, operations.Length + 1);
            for (var index = 0; index < undoCount; index++)
            {
                state.Undo();
            }

            for (var index = 0; index < undoCount; index++)
            {
                state.Redo();
            }

            Assert.Equal(operations, state.PendingTransforms);
        }
    }

    private TransformOperation[] RandomOperations(int count)
    {
        var values = Enum.GetValues<TransformOperation>();
        return Enumerable.Range(0, count)
            .Select(_ => values[_random.Next(values.Length)])
            .ToArray();
    }
}
