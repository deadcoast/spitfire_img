namespace ImageViewer.Core.Models;

public sealed record ComparisonSet(
    ComparisonLayout Layout,
    IReadOnlyList<ComparisonImage> Images);
