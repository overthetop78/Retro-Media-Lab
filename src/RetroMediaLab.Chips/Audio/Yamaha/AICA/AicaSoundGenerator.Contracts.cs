namespace RetroMediaLab.Chips.Audio.Yamaha.AICA;

internal struct Fp22_10
{
    internal uint Full;

    internal uint Fp
    {
        readonly get => Full & 0x3ff;
        set => Full = (Full & ~0x3ffu) | (value & 0x3ffu);
    }

    internal uint Ip
    {
        readonly get => Full >> 10;
        set => Full = (Full & 0x3ffu) | (value << 10);
    }
}

internal struct SignedFp22_10
{
    internal int Full;

    internal uint Fp
    {
        readonly get => (uint)Full & 0x3ff;
        set => Full = (Full & ~0x3ff) | (int)(value & 0x3ffu);
    }

    internal int Ip
    {
        readonly get => Full >> 10;
        set => Full = (Full & 0x3ff) | (value << 10);
    }
}

internal struct Fp20_12
{
    internal uint Full;

    internal uint Fp
    {
        readonly get => Full & 0xfff;
        set => Full = (Full & ~0xfffu) | (value & 0xfffu);
    }

    internal uint Ip
    {
        readonly get => Full >> 12;
        set => Full = (Full & 0xfffu) | (value << 12);
    }
}
