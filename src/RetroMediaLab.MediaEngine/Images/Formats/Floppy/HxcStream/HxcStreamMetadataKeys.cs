namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.HxcStream;

/// <summary>Container data and native metadata remain scoped to their physical track.</summary>
internal static class HxcStreamMetadataKeys
{
    internal static string Native(int cylinder, int head) => $"hxcstream.native.{cylinder}.{head}";
    internal static string Digest(int cylinder, int head) => $"hxcstream.digest.{cylinder}.{head}";
    internal static string Fields(int cylinder, int head) => $"hxcstream.field.{cylinder}.{head}.";
}
