namespace ImageViewer.Tests;

internal static class GifFixture
{
    public static byte[] CreateGif(int width, int height, IReadOnlyList<int> frameDelaysMs)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, "GIF89a");
        WriteUInt16(stream, width);
        WriteUInt16(stream, height);
        stream.WriteByte(0x80);
        stream.WriteByte(0x00);
        stream.WriteByte(0x00);
        stream.Write([0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF]);

        for (var index = 0; index < frameDelaysMs.Count; index++)
        {
            var delayCentiseconds = Math.Max(1, frameDelaysMs[index] / 10);
            stream.Write([0x21, 0xF9, 0x04, 0x00]);
            WriteUInt16(stream, delayCentiseconds);
            stream.Write([0x00, 0x00]);

            stream.WriteByte(0x2C);
            WriteUInt16(stream, 0);
            WriteUInt16(stream, 0);
            WriteUInt16(stream, width);
            WriteUInt16(stream, height);
            stream.WriteByte(0x00);
            stream.WriteByte(0x02);
            stream.Write([0x02, 0x4C, (byte)(0x01 + index), 0x00]);
        }

        stream.WriteByte(0x3B);
        return stream.ToArray();
    }

    private static void WriteAscii(Stream stream, string value)
    {
        foreach (var character in value)
        {
            stream.WriteByte((byte)character);
        }
    }

    private static void WriteUInt16(Stream stream, int value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
    }
}
