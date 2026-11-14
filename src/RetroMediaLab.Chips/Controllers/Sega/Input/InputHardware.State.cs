using RetroMediaLab.Chips.Common.Memory;

namespace RetroMediaLab.Chips.Controllers.Sega.Input;

public sealed partial class InputHardware
{
    private sealed class t_input
    {
        public readonly byte[] system = new byte[2];
        public readonly byte[] dev = new byte[8];
        public readonly ushort[] pad = new ushort[8];
        public readonly short[] analog = new short[16];
        public int x_offset;
        public int y_offset;
    }
    private sealed class Input_activator
    {
        public byte State;
        public byte Counter;
    }
    private sealed class Input_gamepad
    {
        public byte State;
        public byte Counter;
        public byte Timeout;
        public uint Latency;
    }
    private sealed class Input_flipflop
    {
        public byte Latch;
        public byte Counter;
    }
    private sealed class Input_board
    {
        public byte State;
        public byte Counter;
        public byte Port;
    }
    private sealed class Input_lightgun
    {
        public byte State;
        public byte Port;
    }
    private sealed class Input_mouse
    {
        public byte State;
        public byte Counter;
        public byte Wait;
        public byte Port;
    }
    private sealed class Input_paddle
    {
        public byte State;
        public byte Counter;
    }
    private sealed class Input_sportspad
    {
        public byte State;
        public byte Counter;
    }
    private sealed class Input_teamplayer
    {
        public byte State;
        public byte Counter;
        public readonly byte[] Table = new byte[12];
    }
    private sealed class Input_tablet
    {
        public byte axis;
        public byte busy;
    }
    private sealed class Input_xe_1ap
    {
        public byte State;
        public byte Counter;
        public byte Latency;
    }
    private readonly t_input input = new();
    private readonly int[] old_system = [-1,-1];
    private byte latch;
    private readonly Input_activator[] activator = [new(), new()];
    private readonly Input_gamepad[] gamepad = [new(), new(), new(), new(), new(), new(), new(), new()];
    private readonly Input_flipflop[] flipflop = [new(), new()];
    private readonly Input_board board = new();
    private readonly Input_lightgun lightgun = new();
    private readonly Input_mouse mouse = new();
    private readonly Input_paddle[] paddle = [new(), new()];
    private readonly Input_sportspad[] sportspad = [new(), new()];
    private readonly Input_teamplayer[] teamplayer = [new(), new()];
    private readonly Input_tablet tablet = new();
    private readonly Input_xe_1ap[] xe_1ap = [new(), new()];
}
