namespace RetroMediaLab.Chips.Common.Memory;

public sealed class BytePageMap
{
    public Memory<byte>[] Pages { get; }
    public BytePageMap(Memory<byte>[] pages) { Pages = pages; }
    public ArrayPointer<byte> this[int index]
    {
        get => new(Pages[index]);
        set => Pages[index] = value.IsNull ? default : value.Memory;
    }
}
