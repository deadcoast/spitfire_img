namespace ImageViewer.Core.Models;

public sealed record CodecInstallPrompt(
    string FormatName,
    string CodecName,
    string StoreUrl,
    string Message);
