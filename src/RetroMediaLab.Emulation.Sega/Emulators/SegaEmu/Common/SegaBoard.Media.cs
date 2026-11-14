using System.IO;
using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Media;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Common;

internal abstract partial class SegaBoard
{
    private int TryMountAddonDisc(string path)
    {
        Span<byte> header = stackalloc byte[0x210];
        int result = TryMountDiscImage(path,header);
        if (result > 0) header.CopyTo(_cd.DataController.Ram.Span);
        return result;
    }
    private int TryMountDiscImage(string path, Span<byte> header)
    {
        _cancellation.ThrowIfCancellationRequested();
        if (!File.Exists(path)) return 0;
        string extension = Path.GetExtension(path).ToLowerInvariant();
        if (extension is not (".cue" or ".iso" or ".chd" or ".bin")) return 0;
        MediaEngineDisc? disc = null; bool transferred = false;
        try
        {
            disc = MediaEngineDisc.OpenAsync(path,_cancellation).GetAwaiter().GetResult();
            disc.ReadHeader(header[..0x210]);
            // A plain cartridge must not instantiate the Mega-CD hardware just to probe its file name.
            _cd.Drive.Mount(disc); transferred = true; return 1;
        }
        catch (Exception error) when (error is IOException or NotSupportedException or AggregateException)
        {
            return extension == ".bin" ? 0 : -1;
        }
        finally { if (!transferred) disc?.Dispose(); }
    }
}
