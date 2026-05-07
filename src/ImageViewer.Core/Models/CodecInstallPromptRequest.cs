namespace ImageViewer.Core.Models;

public sealed record CodecInstallPromptRequest(
    string FormatName,
    string Message,
    string StoreUrl);
