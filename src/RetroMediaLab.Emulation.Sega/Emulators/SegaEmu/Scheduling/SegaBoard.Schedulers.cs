namespace RetroMediaLab.Emulation.Sega.Emulators.SegaEmu.Boards;
internal abstract partial class SegaBoard
{
    protected Action<int> CreateMegaDriveFrameScheduler() => new MegaDriveFrameScheduler(this).Execute;
    protected Action<int> CreateMegaCdFrameScheduler() => new MegaCdFrameScheduler(this).Execute;
    protected Action<int> CreateEightBitFrameScheduler() => new EightBitFrameScheduler(this).Execute;
}
