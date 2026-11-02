namespace RetroMediaLab.Emulation.Acorn.Emulators.AcornEmu.Components.Vidc.Services;

/// <summary>FIFO bornée du composant, sans file d'attente artificielle côté hôte.</summary>
internal sealed class ArchimedesVidcFifo(int capacity)
{
    private readonly uint[] _words = new uint[capacity];
    private int _read;
    private int _write;
    internal int Count { get; private set; }
    internal int Capacity => _words.Length;

    internal void Clear() => _read = _write = Count = 0;

    internal void Supply(ReadOnlySpan<uint> words)
    {
        foreach (var word in words)
        {
            if (Count == Capacity) break;
            _words[_write] = word;
            _write = (_write + 1) % Capacity;
            Count++;
        }
    }

    internal bool TryTake(out uint word)
    {
        word = 0;
        if (Count == 0) return false;
        word = _words[_read];
        _read = (_read + 1) % Capacity;
        Count--;
        return true;
    }
}
