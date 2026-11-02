namespace RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Services;

/// <summary>FIFO bornée du composant, sans file d'attente artificielle côté hôte.</summary>
public sealed class ArchimedesVidcFifo(int capacity)
:RetroMediaLab.Chips.Video.Acorn.Archimedes.VIDC.Interfaces.IArchimedesVidcFifo{
    private readonly uint[] _words = new uint[capacity];
    private int _read;
    private int _write;
    public int Count { get; private set; }
    public int Capacity => _words.Length;

    public void Clear() => _read = _write = Count = 0;

    public void Supply(ReadOnlySpan<uint> words)
    {
        foreach (var word in words)
        {
            if (Count == Capacity) break;
            _words[_write] = word;
            _write = (_write + 1) % Capacity;
            Count++;
        }
    }

    public bool TryTake(out uint word)
    {
        word = 0;
        if (Count == 0) return false;
        word = _words[_read];
        _read = (_read + 1) % Capacity;
        Count--;
        return true;
    }
}
