using RetroMediaLab.App.Services.Storage;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal static class VideoShaderDiskCache
{
    internal const int MaximumBinarySize = 16 * 1024 * 1024;

    internal static string Entry(string renderer, params string[] inputs)
    {
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\0', inputs))));
        return Path.Combine(StoragePaths.ShaderCacheDirectory, renderer, key + ".bin");
    }

    internal static byte[]? Read(string path)
    {
        try
        {
            using var input = File.OpenRead(path);
            var length = input.Length - SHA256.HashSizeInBytes;
            if (length is <= 0 or > MaximumBinarySize) return null;
            Span<byte> expected = stackalloc byte[SHA256.HashSizeInBytes];
            input.ReadExactly(expected);
            var binary = new byte[(int)length];
            input.ReadExactly(binary);
            Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
            SHA256.HashData(binary, digest);
            return digest.SequenceEqual(expected) ? binary : null;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal static void Write(string path, ReadOnlySpan<byte> binary)
    {
        if (binary.Length is <= 0 or > MaximumBinarySize) return;
        var pending = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using (var output = File.Create(pending))
            {
                Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
                SHA256.HashData(binary, digest);
                output.Write(digest);
                output.Write(binary);
            }
            File.Move(pending, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        finally
        {
            try { File.Delete(pending); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
