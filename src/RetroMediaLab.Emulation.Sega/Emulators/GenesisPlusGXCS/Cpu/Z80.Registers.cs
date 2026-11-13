namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cpu;

internal sealed partial class Z80<TBus> where TBus : class, IZ80Bus
{
    private int PCD { get => _state.pc.Data; set => _state.pc.Data = value; }
    private int PC { get => _state.pc.Word; set => _state.pc.Word = value; }
    private int SPD { get => _state.sp.Data; set => _state.sp.Data = value; }
    private int SP { get => _state.sp.Word; set => _state.sp.Word = value; }
    private int AFD { get => _state.af.Data; set => _state.af.Data = value; }
    private int AF { get => _state.af.Word; set => _state.af.Word = value; }
    private int A { get => _state.af.High; set => _state.af.High = value; }
    private int F { get => _state.af.Low; set => _state.af.Low = value; }
    private int BCD { get => _state.bc.Data; set => _state.bc.Data = value; }
    private int BC { get => _state.bc.Word; set => _state.bc.Word = value; }
    private int B { get => _state.bc.High; set => _state.bc.High = value; }
    private int C { get => _state.bc.Low; set => _state.bc.Low = value; }
    private int DED { get => _state.de.Data; set => _state.de.Data = value; }
    private int DE { get => _state.de.Word; set => _state.de.Word = value; }
    private int D { get => _state.de.High; set => _state.de.High = value; }
    private int E { get => _state.de.Low; set => _state.de.Low = value; }
    private int HLD { get => _state.hl.Data; set => _state.hl.Data = value; }
    private int HL { get => _state.hl.Word; set => _state.hl.Word = value; }
    private int H { get => _state.hl.High; set => _state.hl.High = value; }
    private int L { get => _state.hl.Low; set => _state.hl.Low = value; }
    private int IXD { get => _state.ix.Data; set => _state.ix.Data = value; }
    private int IX { get => _state.ix.Word; set => _state.ix.Word = value; }
    private int HX { get => _state.ix.High; set => _state.ix.High = value; }
    private int LX { get => _state.ix.Low; set => _state.ix.Low = value; }
    private int IYD { get => _state.iy.Data; set => _state.iy.Data = value; }
    private int IY { get => _state.iy.Word; set => _state.iy.Word = value; }
    private int HY { get => _state.iy.High; set => _state.iy.High = value; }
    private int LY { get => _state.iy.Low; set => _state.iy.Low = value; }
    private int WZ { get => _state.wz.Word; set => _state.wz.Word = value; }
    private int WZ_H { get => _state.wz.High; set => _state.wz.High = value; }
    private int WZ_L { get => _state.wz.Low; set => _state.wz.Low = value; }
    private int I { get => _state.i; set => _state.i = value & 255; }
    private int R { get => _state.r; set => _state.r = value & 255; }
    private int R2 { get => _state.r2; set => _state.r2 = value & 255; }
    private int IM { get => _state.im; set => _state.im = value & 255; }
    private int IFF1 { get => _state.iff1; set => _state.iff1 = value & 255; }
    private int IFF2 { get => _state.iff2; set => _state.iff2 = value & 255; }
    private int HALT { get => _state.halt; set => _state.halt = value & 255; }
}
