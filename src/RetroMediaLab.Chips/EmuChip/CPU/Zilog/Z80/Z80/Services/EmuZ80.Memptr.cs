namespace RetroMediaLab.Chips.EmuChip.CPU.Zilog.Z80.Z80.Services;

public sealed partial class EmuZ80
{
    // Rapport matériel boo_boo/Kladov 2006 : chargements/stores, contrôle,
    // arithmétique de paire, indexation et rotations de chiffres.
    private void LatchByteStoreAddress(ushort address) =>
        _registers.WZ = (ushort)((_registers.A << 8) | ((address + 1) & 0xff));

    private void LatchNextAddress(ushort address) => _registers.WZ = unchecked((ushort)(address + 1));
}
