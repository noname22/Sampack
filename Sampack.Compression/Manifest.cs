namespace Sampack.Compression;
internal class Manifest
{
    public Version FormatVersion { get; set; } = new Version(1, 0, 0);
    public string GuesserName { get; set; } = string.Empty;
    public int ChannelCount { get; set; }
    public int BytesPerSample { get; set; }
    public long SampleCount { get; set; }
    public int BlockSize { get; set; }
}
