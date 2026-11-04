using RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Enums;
using C = RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Constants.Mc6847Constants;

namespace RetroMediaLab.Chips.Video.Motorola.M6847.Chip6847.Services;

/// <summary>Compteurs d’adresse de la puce, indépendants de toute RAM ou carte appelante.</summary>
internal sealed class Mc6847AddressCounter
{
    private int _vertical;
    private int _horizontal;
    private int _row;
    private Mc6847Mode _mode;
    internal bool MemorySelect { get; set; } = true;
    internal int HorizontalMask => Mc6847DataPath.GetBytesPerLine(_mode) - 1;
    internal ushort Value => (ushort)((_vertical & ~HorizontalMask) | (_horizontal & HorizontalMask));
    internal ushort? DrivenAddress => MemorySelect ? Value : null;

    internal Mc6847Mode Mode
    {
        get => _mode;
        set
        {
            if ((_mode & Mc6847Mode.Graphics) != 0 && (value & Mc6847Mode.Graphics) == 0) _row = 0;
            _mode = value;
        }
    }

    internal void BeginField()
    {
        _vertical = _horizontal = _row = 0;
    }

    internal void BeginLine() => _horizontal = 0;

    internal void AdvanceCell() => _horizontal = (_horizontal + 1) & HorizontalMask;

    internal void EndActiveLine()
    {
        if (++_row < Mc6847DataPath.GetRowsPerMemoryLine(_mode)) return;
        _row = 0;
        _vertical = (_vertical + Mc6847DataPath.GetBytesPerLine(_mode)) & C.AddressMask;
    }

    internal void Reset()
    {
        BeginField();
        _mode = Mc6847Mode.None;
        MemorySelect = true;
    }
}
