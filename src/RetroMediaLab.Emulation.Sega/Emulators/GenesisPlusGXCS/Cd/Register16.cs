using System.Runtime.InteropServices;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct Register16
{
    public ushort Word;
    public byte Low { readonly get => (byte)Word; set => Word = (ushort)((Word & 0xff00) | value); }
    public byte High { readonly get => (byte)(Word >> 8); set => Word = (ushort)((Word & 0xff) | (value << 8)); }
    public static implicit operator ushort(Register16 register) => register.Word;
    public static implicit operator Register16(ushort value) => new() { Word = value };
}
