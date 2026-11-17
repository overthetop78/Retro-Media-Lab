namespace RetroMediaLab.Chips.Common.Memory;

public sealed class BytePageMap
{
    private readonly Action? _mappingChanged;
    public Memory<byte>[] Pages { get; }
    public BytePageMap(Memory<byte>[] pages) : this(pages, null) { }
    /// <summary>Notifies the owner when a window is replaced through this indexer.</summary>
    public BytePageMap(Memory<byte>[] pages, Action? mappingChanged)
    { Pages = pages; _mappingChanged = mappingChanged; }
    public ArrayPointer<byte> this[int index]
    {
        get => new(Pages[index]);
        set
        {
            Memory<byte> memory = value.IsNull ? default : value.Memory;
            if (Pages[index].Equals(memory)) return;
            Pages[index] = memory;
            _mappingChanged?.Invoke();
        }
    }
}
