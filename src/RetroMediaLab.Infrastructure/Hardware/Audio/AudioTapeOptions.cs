using System.Globalization;
using RetroMediaLab.Infrastructure.Constants;

namespace RetroMediaLab.Infrastructure.Hardware.Audio;

internal readonly record struct AudioTapeOptions(int ChannelCount, int ChannelIndex)
{
    internal static AudioTapeOptions Read(IReadOnlyDictionary<string, string> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var count = ReadNumber(options, AudioTapeMediaOptionKeys.ChannelCount);
        var index = ReadNumber(options, AudioTapeMediaOptionKeys.ChannelIndex);
        if (count is <= 0 or > short.MaxValue || index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(options), "An explicit valid audio channel selection is required.");
        return new(count, index);
    }

    internal static int ReadNumber(IReadOnlyDictionary<string, string> options, string key) =>
        options.TryGetValue(key, out var value) && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result)
            ? result : throw new ArgumentException($"Audio tape option '{key}' is missing or invalid.", nameof(options));
}
