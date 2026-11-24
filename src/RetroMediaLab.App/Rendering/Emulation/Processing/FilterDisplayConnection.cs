using System.Numerics;

namespace RetroMediaLab.App.Rendering.Emulation.Processing;

internal sealed class FilterDisplayConnection
{
    private byte[]? _connection;
    private byte[]? _decoded;

    internal byte[]? Apply(ReadOnlySpan<byte> source, VideoFrame frame, EmulationVideoProcessingConfiguration configuration)
    {
        if (configuration.DisplayTechnology == EmulationVideoDisplayTechnology.Crt)
            return FilterCrtConnection.Apply(source, frame, configuration, ref _connection);
        if (!EmulationVideoProcessingConfigurationFunctions.UsesPanelSignal(configuration))
            return null;

        var panel = EmulationVideoProcessingConfigurationFunctions.PanelSignal(configuration);
        var connected = FilterCrtConnection.Apply(source, frame, panel.Connection, panel.ConnectionDegradation,
            panel.ColorStandards, panel.MonitorPalette, ref _connection);
        var signal = CrtSignal.ForLcd(panel, frame);
        var decoder = (int)signal.Tuning.Y;
        var unlocked = signal.Tuning.X > 0;
        if (connected is not null) source = connected;

        var width = frame.Width;
        var height = frame.Height;
        var length = checked(width * height * 4);
        if (_decoded is null || _decoded.Length < length) _decoded = new byte[length];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var uv = unlocked ? signal.SourceUv(new Vector2((x + .5f) / width, (y + .5f) / height)) : default;
            var output = (y * width + x) * 4;
            _decoded[output + 3] = 255;
            if (unlocked && !CrtSignal.IsOnScreen(uv))
            {
                _decoded.AsSpan(output, 3).Clear();
                continue;
            }
            var sx = unlocked ? Math.Min(width - 1, (int)(uv.X * width)) : x;
            var sy = unlocked ? Math.Min(height - 1, (int)(uv.Y * height)) : y;
            var input = (sy * width + sx) * 4;
            var neighbor = decoder == 1 ? (sy * width + Math.Max(0, sx - 1)) * 4
                : (Math.Max(0, sy - 1) * width + sx) * 4;
            var center = Read(source, input);
            var color = CrtSignal.Decode(center, decoder == 0 ? center : Read(source, neighbor), decoder, sy);
            _decoded[output] = (byte)MathF.Round(color.Z * 255f);
            _decoded[output + 1] = (byte)MathF.Round(color.Y * 255f);
            _decoded[output + 2] = (byte)MathF.Round(color.X * 255f);
        }
        var screen = EmulationVideoProcessingConfigurationFunctions.ScreenType(configuration);
        if (screen == EmulationVideoScreenType.LedBacklitLcd)
            FilterLedBacklitLcdDisplay.PrepareZones(_decoded, width, height, configuration.Led.BacklightType);
        else if (screen == EmulationVideoScreenType.Oled)
            FilterOledDisplay.PrepareBrightness(_decoded, length);
        return _decoded;
    }

    private static Vector3 Read(ReadOnlySpan<byte> source, int offset) =>
        new Vector3(source[offset + 2], source[offset + 1], source[offset]) / 255f;
}
