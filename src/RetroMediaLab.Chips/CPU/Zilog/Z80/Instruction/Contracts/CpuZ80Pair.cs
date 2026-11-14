namespace RetroMediaLab.Chips.CPU.Zilog.Z80.Instruction;

public struct CpuZ80Pair
{
    private uint _data;
    public int Data { readonly get => unchecked((int)_data); set => _data = unchecked((uint)value); }
    public int Word { readonly get => (int)(_data & 0xffff); set => _data = (_data & 0xffff0000) | ((uint)value & 0xffff); }
    public int Low { readonly get => (int)(_data & 255); set => _data = (_data & 0xffffff00) | ((uint)value & 255); }
    public int High { readonly get => (int)(_data >> 8 & 255); set => _data = (_data & 0xffff00ff) | (((uint)value & 255) << 8); }
}

