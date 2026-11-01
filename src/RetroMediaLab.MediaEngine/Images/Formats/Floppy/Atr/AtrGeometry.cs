using RetroMediaLab.MediaEngine.Enums;

namespace RetroMediaLab.MediaEngine.Images.Formats.Floppy.Atr;

/// <summary>Décrit soit une géométrie physique ATR connue, soit son adressage logique linéaire.</summary>
internal sealed record AtrGeometry(int Cylinders, int Heads, int SectorsPerTrack, SectorImageAddressingKind AddressingKind);
