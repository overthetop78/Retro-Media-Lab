using System.Buffers.Binary;

namespace RetroMediaLab.Chips.Memory.Flash.Cfi;

public sealed partial class FlashCfi
{
    public const int M29W320Eb = 0, S29Gl064N04 = 1;
    public const int ContextSize = 12;
    private readonly IFlashCfiBus _bus;
    public FlashCfi(IFlashCfiBus bus) => _bus = bus ?? throw new ArgumentNullException(nameof(bus));
    public void Initialize(int type, ReadOnlySpan<ushort> otpData)
    {
        if ((uint)type >= 2) throw new ArgumentOutOfRangeException(nameof(type));
        flash.addr = flash.data = 0; flash.mode = 0; flash.Type = type;
        flash.otp_area.AsSpan().Clear(); flash.readmask = flash_readmask[type];
        otpData[..Math.Min(otpData.Length, flash.otp_area.Length)].CopyTo(flash.otp_area);
    }
    public void Write(uint address, uint data)
    {
        uint block = address >> 15 & 255;
        if (flash.mode == 3)
        {
            _bus.ProgramWord(address, (ushort)(data & _bus.ReadWord(address)));
            flash.mode = 0;
        }
        else
        {
            address &= 0xfff; data &= 255;
            if (data == 0xf0 && flash.mode < 7) flash.mode = 0;
            else switch (flash.mode)
            {
                case 1:
                    if (address == 0x555) flash.mode = data switch { 0x80 => 4, 0x88 => 7, 0x90 => 2, 0xa0 => 3, _ => flash.mode };
                    break;
                case 7:
                    if (flash.addr == 0x555 && flash.data == 0xaa && address == 0x2aa && data == 0x55) flash.mode = 8;
                    break;
                case 8:
                    if (flash.addr == 0x555 && flash.data == 0x90 && data == 0) flash.mode = 0;
                    break;
                case 4:
                    if (flash.addr == 0x555 && flash.data == 0xaa && address == 0x2aa && data == 0x55) flash.mode = 5;
                    break;
                case 5:
                    if (address == 0x555 && data == 0x10) { _bus.EraseChip(); flash.mode = 0; }
                    else if (data == 0x30) { _bus.EraseBlock(block); flash.mode = 0; }
                    break;
                default:
                    if (flash.addr == 0x555 && flash.data == 0xaa && address == 0x2aa && data == 0x55) flash.mode = 1;
                    else if (address == 0x55 && data == 0x98) flash.mode = 6;
                    break;
            }
        }
        flash.addr = address; flash.data = data;
    }
    public uint Read(uint address) => flash.mode switch
    {
        7 or 8 => flash.otp_area[address & 127],
        2 => flash.id[(int)(address & flash.readmask)],
        6 => address is >= 16 and < 81 ? flash.cfi_data[(int)address - 16] : 0u,
        _ => _bus.ReadWord(address)
    };
    public int SaveContext(Span<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Flash context is too short.", nameof(state));
        BinaryPrimitives.WriteUInt32LittleEndian(state, flash.addr);
        BinaryPrimitives.WriteUInt32LittleEndian(state[4..], flash.data);
        BinaryPrimitives.WriteInt32LittleEndian(state[8..], flash.mode); return ContextSize;
    }
    public int LoadContext(ReadOnlySpan<byte> state)
    {
        if (state.Length < ContextSize) throw new ArgumentException("Flash context is too short.", nameof(state));
        flash.addr = BinaryPrimitives.ReadUInt32LittleEndian(state);
        flash.data = BinaryPrimitives.ReadUInt32LittleEndian(state[4..]);
        flash.mode = BinaryPrimitives.ReadInt32LittleEndian(state[8..]); return ContextSize;
    }
}
