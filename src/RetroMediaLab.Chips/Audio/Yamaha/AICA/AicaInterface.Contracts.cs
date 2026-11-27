using System.IO;
using System.Numerics;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal static partial class AicaInterface
{
    internal const uint AramSizeMax = 8 * 1024 * 1024;
    internal const uint ArmClock = 45_158_400 / 2;
    private const int Sh4MainClock = 200 * 1000 * 1000;
    private const int G2BusClock = 25 * 1000 * 1000;

    internal static uint RamSize => Sh4DreamcastRuntime.Platform.AramSize;
    internal static uint RamMask => Sh4DreamcastRuntime.Platform.AramMask;
    internal static bool MuteAudio => Sh4DreamcastRuntime.Settings.Aica.MuteAudio;
    internal static bool FastForwardMode => Sh4DreamcastRuntime.Settings.Input.FastForwardMode;

    internal static partial uint GetRtcNow();
    internal static partial T ReadRtcRegister<T>(uint address) where T : unmanaged, IBinaryInteger<T>;
    internal static partial void WriteRtcRegister<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>;
    internal static partial T ReadAicaRegister<T>(uint address) where T : unmanaged, IBinaryInteger<T>;
    internal static partial void WriteAicaRegister<T>(uint address, T data) where T : unmanaged, IBinaryInteger<T>;

    internal static void Init() => Aica.Init();
    internal static void Reset(bool hard) => Aica.Reset(hard);
    internal static void Term() => Aica.Term();
    internal static partial void Serialize(BinaryWriter writer, bool rollback);
    internal static partial void Deserialize(BinaryReader reader, int version, bool rollback);

    internal static partial void InitRtc();
    internal static partial void ResetRtc(bool hard);
    internal static partial void TermRtc();

    internal static void SetMidiReceiver(Action<byte>? handler) => AicaMemory.SetMidiReceiver(handler);
    internal static void MidiSend(byte data) => Aica.MidiSend(data);

    internal static partial void SbInit();
    internal static partial void SbReset(bool hard);
    internal static partial void SbTerm();
}
