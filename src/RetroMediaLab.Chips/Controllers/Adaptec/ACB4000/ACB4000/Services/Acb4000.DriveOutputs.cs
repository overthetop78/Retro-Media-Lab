using RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Enums;
using C = RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants.Acb4000Constants;

namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Services;

public sealed partial class Acb4000
{
    private int _unit;
    private int _selectedHead;
    private int _formatCylinder;
    private int _formatHead;
    private bool _readGate;
    private bool _writeGate;

    public event Action<bool, bool>? DriveGatesChanged;
    public event Action<int, int, int, int>? DriveSelectionChanged;

    public int Unit
    {
        get => _unit;
        private set
        {
            if (_unit == value) return;
            _unit = value;
            PublishDriveSelection();
            UpdateDriveGates();
        }
    }

    public int SelectedHead
    {
        get => _selectedHead;
        private set { if (_selectedHead == value) return; _selectedHead = value; PublishDriveSelection(); }
    }

    public int FormatCylinder
    {
        get => _formatCylinder;
        private set { if (_formatCylinder == value) return; _formatCylinder = value; PublishDriveSelection(); }
    }

    public int FormatHead
    {
        get => _formatHead;
        private set { if (_formatHead == value) return; _formatHead = value; PublishDriveSelection(); }
    }

    private void PublishDriveSelection() => DriveSelectionChanged?.Invoke(_unit, _selectedHead, _formatCylinder, _formatHead);

    private void SetStage(Acb4000Stage stage)
    {
        if (_stage == stage) return;
        _stage = stage;
        _hostInterfaceChanged = true;
        UpdateDriveGates();
    }

    // Resolve only when a cause changes. Cell clocks consume the retained electrical levels.
    private void UpdateDriveGates()
    {
        var ready = _unit < C.DriveCount && _driveReady[_unit] && !_eccRegisters.ResetAsserted;
        var read = ready && _stage is Acb4000Stage.Read or Acb4000Stage.WriteSearch or Acb4000Stage.Parameters;
        var write = ready && !_driveWriteFault && (_stage == Acb4000Stage.Format && _formatChannel.WriteGate
            || _stage == Acb4000Stage.Write && _writeChannel.WriteGate);
        if (_readGate == read && _writeGate == write) return;
        _readGate = read;
        _writeGate = write;
        DriveGatesChanged?.Invoke(read, write);
    }
}
