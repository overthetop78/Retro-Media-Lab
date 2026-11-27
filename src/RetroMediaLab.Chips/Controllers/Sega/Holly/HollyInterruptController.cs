using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using static RetroMediaLab.Chips.Dreamcast.Holly.HollySystemBus;

namespace RetroMediaLab.Chips.Dreamcast.Holly;

internal static class HollyInterruptController
{
    private static void Rl6Pending()
    {
        bool normal = (SB_ISTNRM & SB_IML6NRM) != 0;
        bool error = (SB_ISTERR & SB_IML6ERR) != 0;
        bool external = (SB_ISTEXT & SB_IML6EXT) != 0;
        bool secondaryNormal = (SB_ISTNRM1 & SB_IML6NRM) != 0;

        Sh4DreamcastRuntime.AddressSpace.Interrupts.SetPending(Sh4InterruptId.Irl9, normal || error || external || secondaryNormal);
    }

    private static void Rl4Pending()
    {
        bool normal = (SB_ISTNRM & SB_IML4NRM) != 0;
        bool error = (SB_ISTERR & SB_IML4ERR) != 0;
        bool external = (SB_ISTEXT & SB_IML4EXT) != 0;
        bool secondaryNormal = (SB_ISTNRM1 & SB_IML4NRM) != 0;

        Sh4DreamcastRuntime.AddressSpace.Interrupts.SetPending(Sh4InterruptId.Irl11, normal || error || external || secondaryNormal);
    }

    private static void Rl2Pending()
    {
        bool normal = (SB_ISTNRM & SB_IML2NRM) != 0;
        bool error = (SB_ISTERR & SB_IML2ERR) != 0;
        bool external = (SB_ISTEXT & SB_IML2EXT) != 0;
        bool secondaryNormal = (SB_ISTNRM1 & SB_IML2NRM) != 0;

        Sh4DreamcastRuntime.AddressSpace.Interrupts.SetPending(Sh4InterruptId.Irl13, normal || error || external || secondaryNormal);
    }

    internal static void RaiseInterrupt(HollyInterruptId interrupt)
    {
        byte type = unchecked((byte)((uint)interrupt >> 8));
        uint mask = 1u << unchecked((byte)interrupt);
        switch (type)
        {
            case 0:
                SB_ISTNRM |= mask;
                break;
            case 1:
                SB_ISTEXT |= mask;
                break;
            case 2:
                SB_ISTERR |= mask;
                break;
        }
        Rl2Pending();
        Rl4Pending();
        Rl6Pending();
    }

    internal static void RaiseInterruptBothClx(HollyInterruptId interrupt)
    {
        byte type = unchecked((byte)((uint)interrupt >> 8));
        uint mask = 1u << unchecked((byte)interrupt);
        switch (type)
        {
            case 0:
                SB_ISTNRM1 |= mask;
                SB_ISTNRM |= mask;
                break;
            case 1:
                SB_ISTEXT |= mask;
                break;
            case 2:
                SB_ISTERR |= mask;
                break;
        }
        Rl2Pending();
        Rl4Pending();
        Rl6Pending();
    }

    private static uint ReadSbIstnrm(uint address, bool naomi2)
    {
        uint result = (naomi2 && (address & 0x02000000) != 0 ? SB_ISTNRM1 : SB_ISTNRM) & 0x3fffffff;
        if (SB_ISTEXT != 0)
            result |= 0x40000000;

        if (SB_ISTERR != 0)
            result |= 0x80000000;

        return result;
    }

    private static void WriteSbIstnrm(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            SB_ISTNRM1 &= ~data;
        else
            SB_ISTNRM &= ~data;
        Rl2Pending();
        Rl4Pending();
        Rl6Pending();
    }

    internal static void CancelInterrupt(HollyInterruptId interrupt)
    {
        byte type = unchecked((byte)((uint)interrupt >> 8));
        uint mask = ~(1u << unchecked((byte)interrupt));
        switch (type)
        {
            case 0:
                SB_ISTNRM &= mask;
                break;
            case 1:
                SB_ISTEXT &= mask;
                break;
            case 2:
                SB_ISTERR &= mask;
                break;
        }
        Rl2Pending();
        Rl4Pending();
        Rl6Pending();
    }

    private static void WriteSbIstext(uint address, uint data)
    {
    }

    private static void WriteSbIsterr(uint address, uint data)
    {
        SB_ISTERR &= ~data;
        Rl2Pending();
        Rl4Pending();
        Rl6Pending();
    }

    private static void WriteSbIML6NRM(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML6NRM = data;
        Rl6Pending();
    }

    private static void WriteSbIML4NRM(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML4NRM = data;
        Rl4Pending();
    }

    private static void WriteSbIML2NRM(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML2NRM = data;
        Rl2Pending();
    }

    private static void WriteSbIML6EXT(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML6EXT = data;
        Rl6Pending();
    }

    private static void WriteSbIML4EXT(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML4EXT = data;
        Rl4Pending();
    }

    private static void WriteSbIML2EXT(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML2EXT = data;
        Rl2Pending();
    }

    private static void WriteSbIML6ERR(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML6ERR = data;
        Rl6Pending();
    }

    private static void WriteSbIML4ERR(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML4ERR = data;
        Rl4Pending();
    }

    private static void WriteSbIML2ERR(uint address, uint data, bool naomi2)
    {
        if (naomi2 && (address & 0x02000000) != 0)
            return;

        SB_IML2ERR = data;
        Rl2Pending();
    }

    internal static void Init()
    {
    }

    internal static void Term()
    {
    }

    internal static void Reset(bool hard)
    {
        if (hard)
        {
            Registers.SetWriteHandler(SB_ISTEXT_addr, WriteSbIstext);
            Registers.SetWriteHandler(SB_ISTERR_addr, WriteSbIsterr);

            if (Sh4DreamcastRuntime.Platform.IsNaomi2)
            {
                Registers.SetHandlers(SB_ISTNRM_addr, static address => ReadSbIstnrm(address, true),
                    static (address, data) => WriteSbIstnrm(address, data, true));
                Registers.SetWriteHandler(SB_IML6NRM_addr, static (address, data) => WriteSbIML6NRM(address, data, true));
                Registers.SetWriteHandler(SB_IML4NRM_addr, static (address, data) => WriteSbIML4NRM(address, data, true));
                Registers.SetWriteHandler(SB_IML2NRM_addr, static (address, data) => WriteSbIML2NRM(address, data, true));
                Registers.SetWriteHandler(SB_IML6EXT_addr, static (address, data) => WriteSbIML6EXT(address, data, true));
                Registers.SetWriteHandler(SB_IML4EXT_addr, static (address, data) => WriteSbIML4EXT(address, data, true));
                Registers.SetWriteHandler(SB_IML2EXT_addr, static (address, data) => WriteSbIML2EXT(address, data, true));
                Registers.SetWriteHandler(SB_IML6ERR_addr, static (address, data) => WriteSbIML6ERR(address, data, true));
                Registers.SetWriteHandler(SB_IML4ERR_addr, static (address, data) => WriteSbIML4ERR(address, data, true));
                Registers.SetWriteHandler(SB_IML2ERR_addr, static (address, data) => WriteSbIML2ERR(address, data, true));
            }
            else
            {
                Registers.SetHandlers(SB_ISTNRM_addr, static address => ReadSbIstnrm(address, false),
                    static (address, data) => WriteSbIstnrm(address, data, false));
                Registers.SetWriteHandler(SB_IML6NRM_addr, static (address, data) => WriteSbIML6NRM(address, data, false));
                Registers.SetWriteHandler(SB_IML4NRM_addr, static (address, data) => WriteSbIML4NRM(address, data, false));
                Registers.SetWriteHandler(SB_IML2NRM_addr, static (address, data) => WriteSbIML2NRM(address, data, false));
                Registers.SetWriteHandler(SB_IML6EXT_addr, static (address, data) => WriteSbIML6EXT(address, data, false));
                Registers.SetWriteHandler(SB_IML4EXT_addr, static (address, data) => WriteSbIML4EXT(address, data, false));
                Registers.SetWriteHandler(SB_IML2EXT_addr, static (address, data) => WriteSbIML2EXT(address, data, false));
                Registers.SetWriteHandler(SB_IML6ERR_addr, static (address, data) => WriteSbIML6ERR(address, data, false));
                Registers.SetWriteHandler(SB_IML4ERR_addr, static (address, data) => WriteSbIML4ERR(address, data, false));
                Registers.SetWriteHandler(SB_IML2ERR_addr, static (address, data) => WriteSbIML2ERR(address, data, false));
            }
        }
    }
}
