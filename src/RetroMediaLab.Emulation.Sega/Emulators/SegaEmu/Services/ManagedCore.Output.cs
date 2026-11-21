using RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;

using System.IO;
using System.Buffers.Binary;

namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Services;

internal sealed partial class ManagedCore
{
    private int _skippedFrames;
    private bool ShouldSkipFrame()
    {
        string option = GetOption("frameskip");
        if (option == "disabled" || _audioStatus is not { IsActive: true, CapacityFrames: > 0 } status)
        { _skippedFrames = 0; return false; }
        bool skip = option == "auto" ? status.BufferedFrames == 0 : status.BufferedFrames * 100L / status.CapacityFrames < Number("frameskip_threshold", 33);
        if (skip && _skippedFrames < 30) { _skippedFrames++; return true; }
        _skippedFrames = 0; return false;
    }
    private sealed record VideoPresentation(VideoLayoutKey Key, int Width, int Height, int Pitch,
        int BytesPerPixel, int Left, int NormalWidth, bool Interlaced, bool Cursor, float Aspect);
    private readonly record struct VideoLayoutKey(int OutputWidth, int ViewportWidth, int ViewportHeight,
        int X, int Y, bool Interlaced, int Depth, byte Hardware, int Register0,
        uint MasterClock, byte Pal, int Overscan, int Extra, object? Options);
    private VideoPresentation? _presentation;

    private VideoPresentation Presentation(SegaBoard system)
    {
        bool interlaced = system.Video.interlaced != 0 && _settings.Video.render != 0;
        var key = new VideoLayoutKey(system.Video.OutputWidth, system.Video.ViewportWidth,
            system.Video.ViewportHeight, system.Video.ViewportX, system.Video.ViewportY,
            interlaced, system.Video.EffectivePixelDepth, system.Hardware, system.Video.reg[0] & 0x20,
            system.MasterClock, system.Video.vdp_pal, _settings.Video.overscan, _settings.Video.gg_extra, _configuration?.Options);
        if (_presentation is { } cached && cached.Key == key) return cached;
        int height = (key.ViewportHeight + key.Y * 2) * (interlaced ? 2 : 1);
        int normalWidth = key.ViewportWidth + key.X * 2;
        string border = GetOption("left_border");
        int left = border is "left border" or "left & right borders" ? 8 : 0;
        int right = border == "left & right borders" ? 8 : 0;
        if (key.X != 0 || key.Register0 == 0 || key.Hardware is not (0x10 or 0x20 or 0x21 or 0x81)) left = right = 0;
        int bytesPerPixel = key.Depth == 32 ? 4 : 2;
        int width = key.OutputWidth - left - right;
        return _presentation = new(key, width, height, width * bytesPerPixel, bytesPerPixel, left,
            normalWidth, interlaced, GetOption("gun_cursor") == "enabled", CalculateAspect(system, normalWidth, height));
    }

    private void PublishVideo(SegaBoard system)
    {
        VideoPresentation layout = Presentation(system);
        // Published arrays are immutable for consumers; no buffer is recycled here.
        byte[] pixels = new byte[layout.Pitch * layout.Height];
        int sourceOffset = layout.Left * layout.BytesPerPixel;
        if (layout.Pitch == BitmapPitch)
            _bitmap.AsSpan(sourceOffset, pixels.Length).CopyTo(pixels);
        else
            for (int line = 0; line < layout.Height; line++)
                _bitmap.AsSpan(line * BitmapPitch + sourceOffset, layout.Pitch).CopyTo(pixels.AsSpan(line * layout.Pitch));
        if (layout.Cursor)
            DrawGunCursors(system, pixels, layout.Width, layout.Height, layout.Pitch,
                layout.BytesPerPixel, layout.Left, layout.NormalWidth, layout.Interlaced);
        LatestVideoFrame = new(pixels, layout.Width, layout.Height, layout.Pitch,
            layout.BytesPerPixel == 4 ? EmulationPixelFormat.Xrgb8888 : EmulationPixelFormat.Rgb565,
            layout.Aspect, _sequence, _timestamp);
    }
    private static void DrawGunCursors(SegaBoard system, Span<byte> pixels, int width, int height,
        int pitch, int bytesPerPixel, int croppedLeft, int normalWidth, bool interlaced)
    {
        for (int device = 0; device < system.Input.DeviceTypes.Length; device++)
        {
            if (system.Input.DeviceTypes[device] != 4) continue;
            int inputX = system.Input.AnalogInputs[device * 2], inputY = system.Input.AnalogInputs[device * 2 + 1];
            if (inputX < 0 || inputY < 0 || inputX >= system.Video.ViewportWidth || inputY >= system.Video.ViewportHeight) continue;
            int x = (inputX + system.Video.ViewportX) * system.Video.OutputWidth / normalWidth - croppedLeft;
            int y = (inputY + system.Video.ViewportY) * (interlaced ? 2 : 1);
            uint color = device == 0 ? 0x000000ffu : 0x00ff0000u;
            ushort color565 = device == 0 ? (ushort)0x001f : (ushort)0xf800;
            for (int offset = -5; offset <= 5; offset++)
            {
                WriteCursorPixel(pixels, x + offset, y, width, height, pitch, bytesPerPixel, color, color565);
                WriteCursorPixel(pixels, x, y + offset, width, height, pitch, bytesPerPixel, color, color565);
            }
        }
    }
    private static void WriteCursorPixel(Span<byte> pixels, int x, int y, int width, int height,
        int pitch, int bytesPerPixel, uint color, ushort color565)
    {
        if ((uint)x >= width || (uint)y >= height) return;
        Span<byte> pixel = pixels.Slice(y * pitch + x * bytesPerPixel, bytesPerPixel);
        if (bytesPerPixel == 4) BinaryPrimitives.WriteUInt32LittleEndian(pixel, color);
        else BinaryPrimitives.WriteUInt16LittleEndian(pixel, color565);
    }
    private float CalculateAspect(SegaBoard system, int width, int height)
    {
        string aspect = _configuration?.Model == ModelConstants.Sg1000 ? "auto" : GetOption("aspect_ratio");
        if (aspect == "4:3") return 4f / 3f;
        if (aspect == "Uncorrected") return (float)width / height;
        if (aspect is not ("NTSC PAR" or "PAL PAR") && system.Hardware is 0x40 or 0x41 && _settings.Video.overscan == 0 && _settings.Video.gg_extra == 0)
            return 1.2f * width / height;
        double dotRate = system.MasterClock / (system.Video.ViewportWidth == 320 ? 8.0 : 10.0);
        double sampleRate = aspect == "NTSC PAR" ? 135000000.0 / 11 : aspect == "PAL PAR" ? 14750000 : system.Video.vdp_pal != 0 ? 14750000 : 135000000.0 / 11;
        return (float)(sampleRate / dotRate * width / (height * 2.0));
    }
}
