using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed unsafe partial class PvrCustomTexture
{
    internal static readonly PvrCustomTexture Instance = new();

    private sealed class CustomTextureSource : PvrBaseCustomTextureSource
    {
        private bool _customTexturesAvailable;
        private readonly string _texturesPath;
        private readonly SortedDictionary<uint, string> _textureMap = new();

        internal CustomTextureSource(string gameId)
        {
            _texturesPath = PvrRuntime.GetTextureLoadPath(gameId);
            if (_texturesPath.Length != 0)
            {
                _customTexturesAvailable = PvrRuntime.TextureStorage.Exists(_texturesPath);
                if (_customTexturesAvailable)
                    Trace.TraceInformation($"Found custom textures directory: {_texturesPath}");
            }
        }

        internal override bool ShouldReplace() => PvrConfiguration.CustomTextures && _customTexturesAvailable;
        internal override bool ShouldPreload() => ShouldReplace() && PvrConfiguration.PreloadCustomTextures;
        internal override nuint GetTextureCount() => (nuint)_textureMap.Count;

        internal override bool LoadMap()
        {
            _textureMap.Clear();
            foreach (var item in PvrRuntime.TextureStorage.DirectoryTree(_texturesPath))
            {
                string extension = PvrRuntime.GetFileExtension(item.Name);
                if (extension != "jpg" && extension != "jpeg" && extension != "png")
                    continue;
                int dotPosition = item.Name.LastIndexOf('.');
                string basename = dotPosition < 0 ? item.Name : item.Name[..dotPosition];
                uint hash = ParseHexFileName(basename, out int length);
                if (length < basename.Length)
                {
                    Trace.TraceInformation($"Invalid hash {basename}");
                    continue;
                }
                _textureMap[hash] = item.Path;
            }
            return _textureMap.Count != 0;
        }

        internal override void PreloadTextures(Action<uint, PvrCustomTextureData> callback, Func<bool>? stopRequested)
        {
            foreach ((uint hash, string _) in _textureMap)
            {
                if (stopRequested is not null && stopRequested())
                    return;
                int width = 0;
                int height = 0;
                byte* data = LoadCustomTexture(hash, ref width, ref height);
                if (data != null)
                {
                    int size = checked(width * height * 4);
                    byte[] pixels = new ReadOnlySpan<byte>(data, size).ToArray();
                    PvrTextureImageCodec.Free(data);
                    callback(hash, new PvrCustomTextureData(pixels, width, height));
                }
            }
        }

        internal override byte* LoadCustomTexture(uint hash, ref int width, ref int height)
        {
            if (!_textureMap.TryGetValue(hash, out string? path))
                return null;

            using Stream? file = PvrRuntime.TextureStorage.OpenFile(path, "rb");
            if (file is null)
                return null;

            PvrTextureImageCodec.FlipVerticallyOnLoadThread(true);
            return PvrTextureImageCodec.LoadFromFile(file, ref width, ref height, out _, 4);
        }

        internal override bool IsTextureReplaced(uint hash) => _textureMap.ContainsKey(hash);

        private static uint ParseHexFileName(string value, out int length)
        {
            int index = 0;
            while (index < value.Length && value[index] is ' ' or '\t' or '\r' or '\n' or '\f' or '\v')
                index++;

            bool negative = false;
            if (index < value.Length && value[index] is '+' or '-')
                negative = value[index++] == '-';

            if (index + 2 < value.Length && value[index] == '0' && (value[index + 1] is 'x' or 'X')
                && HexDigit(value[index + 2]) >= 0)
                index += 2;

            int digitStart = index;
            ulong number = 0;
            ulong limit = negative ? 0x8000000000000000ul : 0x7ffffffffffffffful;
            while (index < value.Length)
            {
                int digit = HexDigit(value[index]);
                if (digit < 0)
                    break;
                number = number > (limit - (uint)digit) / 16 ? limit : number * 16 + (uint)digit;
                index++;
            }

            length = digitStart == index ? 0 : index;
            return negative ? unchecked((uint)-(long)number) : unchecked((uint)number);
        }

        private static int HexDigit(char value)
        {
            if (value is >= '0' and <= '9')
                return value - '0';
            if (value is >= 'a' and <= 'f')
                return value - 'a' + 10;
            if (value is >= 'A' and <= 'F')
                return value - 'A' + 10;
            return -1;
        }
    }

    private void LoadTexture(PvrBaseTextureCacheData texture)
    {
        if (texture.CustomImageData != null)
        {
            NativeMemory.Free(texture.CustomImageData);
            texture.CustomImageData = null;
        }
        if (texture.Dirty == 0)
        {
            int width = 0;
            int height = 0;
            byte* image = LoadTexture(texture.TextureHash, ref width, ref height);
            if (image == null && texture.OldVqTextureHash != 0)
                image = LoadTexture(texture.OldVqTextureHash, ref width, ref height);
            if (image == null)
                image = LoadTexture(texture.OldTextureHash, ref width, ref height);
            if (image != null)
            {
                texture.CustomWidth = (uint)width;
                texture.CustomHeight = (uint)height;
                texture.CustomImageData = image;
            }
        }
        Interlocked.Decrement(ref texture.CustomLoadInProgress);
    }

    private static string GetGameId() => PvrRuntime.ContentGameId.TrimEnd(' ').Replace(' ', '_');

    internal partial bool Init()
    {
        if (!_initialized)
        {
            Volatile.Write(ref _stopPreload, false);
            ResetPreloadProgress();
            Volatile.Write(ref _pendingPreloads, 0);
            _initialized = true;

            string gameId = GetGameId();
            if (gameId.Length > 0)
                AddSource(new CustomTextureSource(gameId));
        }

        return _loaderThread is not null;
    }

    internal partial bool Enabled() => _loaderThread is not null;

    internal partial bool Preloaded() => Volatile.Read(ref _preloadTotal) > 0;

    internal partial bool IsPreloading()
    {
        if (Volatile.Read(ref _pendingPreloads) > 0)
            return true;

        GetPreloadProgress(out int loaded, out int total, out _);
        return total > 0 && loaded < total;
    }

    internal partial void AddSource(PvrBaseCustomTextureSource source)
    {
        _sources.Add(source);
        if (_initialized)
        {
            if (_loaderThread is null && source.ShouldReplace())
                _loaderThread = new PvrWorkerThread("CustomTexLoader");
            if (_loaderThread is not null)
            {
                if (source.ShouldPreload())
                    Interlocked.Increment(ref _pendingPreloads);
                _loaderThread.Run(() => PrepareSource(source));
            }
        }
    }

    internal partial void Terminate()
    {
        Volatile.Write(ref _stopPreload, true);
        if (_loaderThread is { } loaderThread)
        {
            try
            {
                loaderThread.Stop();
            }
            finally
            {
                loaderThread.Dispose();
                _loaderThread = null;
            }
        }
        foreach (PvrBaseCustomTextureSource source in _sources)
        {
            try
            {
                source.Terminate();
            }
            finally
            {
                source.Dispose();
            }
        }
        _sources.Clear();
        _preloadedTextures.Clear();
        ResetPreloadProgress();
        _initialized = false;
    }

    private byte* LoadTexture(uint hash, ref int width, ref int height)
    {
        if (_preloadedTextures.TryGetValue(hash, out PvrCustomTextureData texture))
        {
            width = texture.Width;
            height = texture.Height;
            nuint size = (nuint)width * (nuint)height * 4;
            byte* buffer;
            try
            {
                buffer = (byte*)NativeMemory.Alloc(size);
            }
            catch (OutOfMemoryException)
            {
                return null;
            }
            if (buffer == null)
                return null;
            texture.Data.AsSpan(0, (int)size).CopyTo(new Span<byte>(buffer, (int)size));
            return buffer;
        }

        foreach (PvrBaseCustomTextureSource source in _sources)
        {
            if (source.ShouldReplace())
            {
                byte* data = source.LoadCustomTexture(hash, ref width, ref height);
                if (data != null)
                    return data;
            }
        }
        return null;
    }

    private bool IsTextureReplaced(PvrBaseTextureCacheData texture)
    {
        if (_preloadedTextures.ContainsKey(texture.TextureHash))
            return true;
        if (texture.OldVqTextureHash != 0 && _preloadedTextures.ContainsKey(texture.OldVqTextureHash))
            return true;
        if (texture.OldTextureHash != 0 && _preloadedTextures.ContainsKey(texture.OldTextureHash))
            return true;

        foreach (PvrBaseCustomTextureSource source in _sources)
        {
            if (source.ShouldReplace())
            {
                if (source.IsTextureReplaced(texture.TextureHash))
                    return true;
                if (texture.OldVqTextureHash != 0 && source.IsTextureReplaced(texture.OldVqTextureHash))
                    return true;
                if (texture.OldTextureHash != 0 && source.IsTextureReplaced(texture.OldTextureHash))
                    return true;
            }
        }
        return false;
    }

    internal partial void LoadCustomTextureAsync(PvrBaseTextureCacheData texture)
    {
        if (!Init())
            return;

        Interlocked.Increment(ref texture.CustomLoadInProgress);
        _loaderThread!.Run(() => LoadTexture(texture));
    }

    internal partial void DumpTexture(PvrBaseTextureCacheData texture, int width, int height, void* source)
    {
        if (!PvrConfiguration.DumpTextures)
            return;

        if (PvrConfiguration.DumpUniqueTextures && (texture.Updates > 1 || texture.Tcw.PixelFmt == (uint)PixelFormat.PixelYUV))
            return;

        if (!PvrConfiguration.DumpReplacedTextures && IsTextureReplaced(texture))
            return;

        string baseDumpDirectory = PvrRuntime.GetTextureDumpPath();
        if (!Directory.Exists(baseDumpDirectory))
            Directory.CreateDirectory(baseDumpDirectory);
        string gameId = GetGameId();
        if (gameId.Length == 0)
            return;

        baseDumpDirectory += gameId + "/";
        if (!Directory.Exists(baseDumpDirectory))
            Directory.CreateDirectory(baseDumpDirectory);

        string path = baseDumpDirectory + texture.TextureHash.ToString("x", CultureInfo.InvariantCulture) + ".png";
        ushort* pixels = (ushort*)source;
        byte* destinationBuffer;
        try
        {
            destinationBuffer = (byte*)NativeMemory.Alloc((nuint)(width * height * 4));
        }
        catch (OutOfMemoryException)
        {
            Trace.TraceError("Dump texture: out of memory");
            return;
        }
        if (destinationBuffer == null)
        {
            Trace.TraceError("Dump texture: out of memory");
            return;
        }
        byte* destination = destinationBuffer;

        try
        {
            for (int y = 0; y < height; y++)
            {
                if (!PvrRuntime.IsDirectX(PvrConfiguration.RendererType))
                {
                    switch (texture.TextureType)
                    {
                        case PvrTextureType._4444:
                            for (int x = 0; x < width; x++)
                            {
                                *destination++ = (byte)((((*pixels >> 12) & 0xf) << 4) | ((*pixels >> 12) & 0xf));
                                *destination++ = (byte)((((*pixels >> 8) & 0xf) << 4) | ((*pixels >> 8) & 0xf));
                                *destination++ = (byte)((((*pixels >> 4) & 0xf) << 4) | ((*pixels >> 4) & 0xf));
                                *destination++ = (byte)(((*pixels & 0xf) << 4) | (*pixels & 0xf));
                                pixels++;
                            }
                            break;
                        case PvrTextureType._565:
                            for (int x = 0; x < width; x++)
                            {
                                *(uint*)destination = PvrUnpacker565To32<PvrRgbaPacker>.Unpack(*pixels);
                                destination += 4;
                                pixels++;
                            }
                            break;
                        case PvrTextureType._5551:
                            for (int x = 0; x < width; x++)
                            {
                                *destination++ = (byte)((((*pixels >> 11) & 0x1f) << 3) | ((*pixels >> 13) & 7));
                                *destination++ = (byte)((((*pixels >> 6) & 0x1f) << 3) | ((*pixels >> 8) & 7));
                                *destination++ = (byte)((((*pixels >> 1) & 0x1f) << 3) | ((*pixels >> 3) & 7));
                                *destination++ = (*pixels & 1) != 0 ? (byte)255 : (byte)0;
                                pixels++;
                            }
                            break;
                        case PvrTextureType._8888:
                            NativeMemory.Copy(pixels, destination, (nuint)(width * 4));
                            destination += width * 4;
                            pixels += width * 2;
                            break;
                        default:
                            Trace.TraceWarning($"dumpTexture: unsupported picture format {(uint)texture.TextureType:x}");
                            return;
                    }
                }
                else
                {
                    switch (texture.TextureType)
                    {
                        case PvrTextureType._4444:
                            for (int x = 0; x < width; x++)
                            {
                                *(uint*)destination = PvrUnpacker4444To32<PvrRgbaPacker>.Unpack(*pixels);
                                destination += 4;
                                pixels++;
                            }
                            break;
                        case PvrTextureType._565:
                            for (int x = 0; x < width; x++)
                            {
                                *(uint*)destination = PvrUnpacker565To32<PvrRgbaPacker>.Unpack(*pixels);
                                destination += 4;
                                pixels++;
                            }
                            break;
                        case PvrTextureType._5551:
                            for (int x = 0; x < width; x++)
                            {
                                *(uint*)destination = PvrUnpacker1555To32<PvrRgbaPacker>.Unpack(*pixels);
                                destination += 4;
                                pixels++;
                            }
                            break;
                        case PvrTextureType._8888:
                            for (int x = 0; x < width; x++)
                            {
                                *(uint*)destination = PvrUnpacker8888<PvrRgbaPacker>.Unpack(*(uint*)pixels);
                                destination += 4;
                                pixels += 2;
                            }
                            break;
                        default:
                            Trace.TraceWarning($"dumpTexture: unsupported picture format {(uint)texture.TextureType:x}");
                            return;
                    }
                }
            }

            PvrTextureImageCodec.FlipVerticallyOnWrite(true);
            PvrTextureImageCodec.WritePngToFunction(SaveTexture, path, width, height, 4, destinationBuffer, 0);
        }
        finally
        {
            NativeMemory.Free(destinationBuffer);
        }
    }

    private static void SaveTexture(string context, void* data, int size)
    {
        FileStream file;
        try
        {
            file = File.Open(context, FileMode.Create, FileAccess.Write);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"Dump texture: can't save to file {context}: error {exception.HResult}");
            return;
        }

        using (file)
            file.Write(new ReadOnlySpan<byte>(data, size));
    }

    private void PrepareSource(PvrBaseCustomTextureSource source)
    {
        bool shouldPreload = source.ShouldPreload();
        if (!Volatile.Read(ref _stopPreload) && source.LoadMap())
        {
            if (shouldPreload)
            {
                int count = (int)source.GetTextureCount();
                if (count > 0)
                {
                    Interlocked.Add(ref _preloadTotal, count);
                    source.PreloadTextures((hash, data) =>
                    {
                        int size = data.Data.Length;
                        _preloadedTextures[hash] = data;
                        Interlocked.Increment(ref _preloadLoaded);
                        Interlocked.Add(ref _preloadLoadedSize, size);
                    }, () => Volatile.Read(ref _stopPreload));
                }
            }
        }

        if (shouldPreload)
            Interlocked.Decrement(ref _pendingPreloads);
    }

    internal partial void GetPreloadProgress(out int completed, out int total, out nuint loadedSize)
    {
        total = Volatile.Read(ref _preloadTotal);
        if (total == 0 && Volatile.Read(ref _pendingPreloads) > 0)
            total = -1;
        completed = Volatile.Read(ref _preloadLoaded);
        loadedSize = (nuint)Volatile.Read(ref _preloadLoadedSize);
    }

    private void ResetPreloadProgress()
    {
        Volatile.Write(ref _preloadTotal, 0);
        Volatile.Write(ref _preloadLoaded, 0);
        Volatile.Write(ref _preloadLoadedSize, 0);
    }
}
