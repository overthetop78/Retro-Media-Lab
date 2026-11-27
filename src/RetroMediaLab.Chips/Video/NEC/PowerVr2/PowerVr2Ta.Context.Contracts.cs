using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

using static PvrRegisters;

internal unsafe struct TaVertex
{
    internal float X;
    internal float Y;
    internal float Z;

    internal fixed byte Col[4];
    internal fixed byte Spc[4];

    internal float U;
    internal float V;

    internal fixed byte Col1[4];
    internal fixed byte Spc1[4];

    internal float U1;
    internal float V1;

    internal float Nx;
    internal float Ny;
    internal float Nz;
}

[InlineArray(2)]
internal struct TaFloatPair { private float _element; }

[InlineArray(2)]
internal struct TaBoolPair { private bool _element; }

internal struct PolyParam
{
    internal uint First;
    internal uint Count;
    internal PvrBaseTextureCacheData? Texture;
    internal TSP Tsp;
    internal TCW Tcw;
    internal PCW Pcw;
    internal ISP_TSP Isp;
    internal float ZvZ;
    internal uint TileClip;
    internal TSP Tsp1;
    internal TCW Tcw1;
    internal PvrBaseTextureCacheData? Texture1;

    internal int MvMatrix;
    internal int NormalMatrix;
    internal int ProjMatrix;
    internal TaFloatPair GlossCoef;
    internal int LightModel;
    internal TaBoolPair EnvMapping;
    internal TaBoolPair ConstantColor;

    internal void Init()
    {
        First = 0;
        Count = 0;
        Texture = null;
        Tsp.full = 0;
        Tcw.full = 0;
        Pcw.full = 0;
        Isp.full = 0;
        ZvZ = 0;
        TileClip = 0;
        Tsp1.full = uint.MaxValue;
        Tcw1.full = uint.MaxValue;
        Texture1 = null;

        MvMatrix = -1;
        NormalMatrix = -1;
        ProjMatrix = -1;
        GlossCoef[0] = 0;
        GlossCoef[1] = 0;
        LightModel = -1;
        EnvMapping[0] = false;
        EnvMapping[1] = false;
        ConstantColor[0] = false;
        ConstantColor[1] = false;
    }

    internal readonly bool EquivalentIgnoreCullingDirection(in PolyParam other) =>
        ((Pcw.full ^ other.Pcw.full) & 0x300ce) == 0
        && ((Isp.full ^ other.Isp.full) & 0xf4000000) == 0
        && Tcw.full == other.Tcw.full
        && Tsp.full == other.Tsp.full
        && TileClip == other.TileClip
        && Tcw1.full == other.Tcw1.full
        && Tsp1.full == other.Tsp1.full
        && MvMatrix == other.MvMatrix
        && NormalMatrix == other.NormalMatrix
        && ProjMatrix == other.ProjMatrix
        && GlossCoef[0] == other.GlossCoef[0]
        && GlossCoef[1] == other.GlossCoef[1]
        && LightModel == other.LightModel
        && EnvMapping[0] == other.EnvMapping[0]
        && ConstantColor[0] == other.ConstantColor[0]
        && EnvMapping[1] == other.EnvMapping[1]
        && ConstantColor[1] == other.ConstantColor[1];

    internal readonly bool IsNaomi2() => ProjMatrix != -1;
}

internal struct ModifierVolumeParam
{
    internal uint First;
    internal uint Count;
    internal ISP_Modvol Isp;
    internal uint TileClip;

    internal int MvMatrix;
    internal int ProjMatrix;

    internal void Init()
    {
        First = 0;
        Count = 0;
        Isp.full = 0;
        TileClip = 0;
        MvMatrix = -1;
        ProjMatrix = -1;
    }

    internal readonly bool IsNaomi2() => ProjMatrix != -1;
}

internal struct ModTriangle
{
    internal float X0;
    internal float Y0;
    internal float Z0;
    internal float X1;
    internal float Y1;
    internal float Z1;
    internal float X2;
    internal float Y2;
    internal float Z2;
}

internal unsafe struct TaDataContext
{
    internal byte* ThdData;
    internal byte* ThdRoot;
    internal byte* ThdOldData;

    internal void Clear() => ThdOldData = ThdData = ThdRoot;

    internal void ClearPartial()
    {
        ThdOldData = ThdData;
        ThdData = ThdRoot;
    }

    internal readonly byte* End() => ThdData == ThdRoot ? ThdOldData : ThdData;

    internal void Reset(byte* pointer)
    {
        ThdRoot = pointer;
        Clear();
    }
}

internal struct RenderPass
{
    internal bool AutoSort;
    internal bool ZClear;
    internal bool MvOpTrShared;
    internal uint OpCount;
    internal uint MvoCount;
    internal uint PtCount;
    internal uint TrCount;
    internal uint MvoTrCount;
    internal uint SortedTrCount;
}

internal unsafe struct N2Matrix
{
    internal fixed float Mat[16];
}

internal unsafe struct N2Light
{
    internal fixed float Color[4];
    internal fixed float Direction[4];
    internal fixed float Position[4];

    internal int Parallel;
    internal int Routing;
    internal int Dmode;
    internal int Smode;
    internal fixed int Diffuse[2];
    internal fixed int Specular[2];

    internal float AttnDistA;
    internal float AttnDistB;
    internal float AttnAngleA;
    internal float AttnAngleB;
    internal int DistAttnMode;
    internal fixed int Pad[3];
}

[InlineArray(16)]
internal struct N2Lights { private N2Light _element; }

[InlineArray(4)]
internal struct N2Float4 { private float _element; }

[InlineArray(2)]
internal struct N2Float4Pair { private N2Float4 _element; }

internal unsafe struct N2LightModel
{
    internal N2Lights Lights;
    internal N2Float4Pair AmbientBase;
    internal N2Float4Pair AmbientOffset;
    internal fixed int AmbientMaterialBase[2];
    internal fixed int AmbientMaterialOffset[2];

    internal int LightCount;
    internal int UseBaseOver;
    internal int BumpId1;
    internal int BumpId2;
}

internal struct SortedTriangle
{
    internal uint PolyIndex;
    internal uint First;
    internal uint Count;
}

internal record struct PvrInt2(int X, int Y)
{
    public static PvrInt2 operator +(PvrInt2 left, PvrInt2 right) => new(left.X + right.X, left.Y + right.Y);
    public static PvrInt2 operator -(PvrInt2 left, PvrInt2 right) => new(left.X - right.X, left.Y - right.Y);
}

internal struct PvrRect(PvrInt2 origin, PvrInt2 size)
{
    internal PvrInt2 Origin = origin;
    internal PvrInt2 Size = size;

    internal readonly PvrInt2 BottomRight() => Origin + Size - new PvrInt2(1, 1);
}

internal sealed partial class RenderContext
{
    internal float FZMax;
    internal bool IsRtt;
    internal bool ClearFramebuffer;
    internal PvrInt2 GlobClip;
    internal SCALER_CTL_type ScalerCtl;
    internal PvrRect FbClip;
    internal PvrRect TileClip;
    internal uint FbWLineStride;
    internal uint FbWSof1;
    internal FB_W_CTRL_type FbWCtrl;
    internal uint FramebufferWidth;
    internal uint FramebufferHeight;
    internal int SwapInterval;
    internal RGBAColor FogClampMin;
    internal RGBAColor FogClampMax;

    internal readonly List<TaVertex> Verts = [];
    internal readonly List<uint> Idx = [];
    internal readonly List<ModTriangle> ModTrig = [];
    internal readonly List<ModifierVolumeParam> GlobalParamMvo = [];
    internal readonly List<ModifierVolumeParam> GlobalParamMvoTr = [];
    internal readonly List<PolyParam> GlobalParamOp = [];
    internal readonly List<PolyParam> GlobalParamPt = [];
    internal readonly List<PolyParam> GlobalParamTr = [];
    internal readonly List<RenderPass> RenderPasses = [];
    internal readonly List<SortedTriangle> SortedTriangles = [];
    internal readonly List<N2Matrix> Matrices = [];
    internal readonly List<N2LightModel> LightModels = [];

    internal void Clear()
    {
        Idx.Clear();
        GlobalParamOp.Clear();
        GlobalParamPt.Clear();
        GlobalParamTr.Clear();
        ModTrig.Clear();
        GlobalParamMvo.Clear();
        GlobalParamMvoTr.Clear();
        RenderPasses.Clear();
        SortedTriangles.Clear();

        var polygon = new PolyParam();
        polygon.Init();
        GlobalParamOp.Add(polygon);
        if (Verts.Count > 4)
            Verts.RemoveRange(4, Verts.Count - 4);
        while (Verts.Count < 4)
            Verts.Add(default);

        FZMax = 1.0f;
        Matrices.Clear();
        LightModels.Clear();
        ClearFramebuffer = false;
    }
}

internal sealed unsafe class TaContext : IDisposable
{
    internal uint Address;
    internal uint LastFrameUsed;
    internal TaDataContext Tad;
    internal readonly RenderContext Rend = new();
    internal TaContext? NextContext;

    internal byte* GetTaDataBegin() => Tad.ThdRoot;

    internal byte* GetTaDataEnd() => Tad.End();

    internal void Allocate()
    {
        Tad.Reset((byte*)NativeMemory.AlignedAlloc(PowerVr2Ta.TaDataSize, 32));
        Rend.Verts.EnsureCapacity(32768);
        Rend.Idx.EnsureCapacity(32768);
        Rend.GlobalParamOp.EnsureCapacity(4096);
        Rend.GlobalParamPt.EnsureCapacity(4096);
        Rend.GlobalParamTr.EnsureCapacity(4096);
        Rend.GlobalParamMvo.EnsureCapacity(4096);
        Rend.GlobalParamMvoTr.EnsureCapacity(4096);
        Rend.ModTrig.EnsureCapacity(16384);

        if (Sh4DreamcastRuntime.Platform.IsNaomi2)
        {
            Rend.Matrices.EnsureCapacity(2000);
            Rend.LightModels.EnsureCapacity(150);
        }
        Reset();
    }

    internal void Reset()
    {
        Sh4HostRuntime.Verify(Tad.End() - Tad.ThdRoot <= PowerVr2Ta.TaDataSize);
        Tad.Clear();
        NextContext = null;
        Rend.Clear();
    }

    public void Dispose()
    {
        try
        {
            Sh4HostRuntime.Verify(Tad.End() - Tad.ThdRoot <= PowerVr2Ta.TaDataSize);
        }
        finally
        {
            NativeMemory.AlignedFree(Tad.ThdRoot);
            Tad.Reset(null);
        }
    }
}

internal static unsafe partial class PowerVr2Ta
{
    internal const int MaxPasses = 10;
    internal const int TaDataSize = 8 * 1024 * 1024;
    internal const uint NoTaContext = uint.MaxValue;
}

internal sealed class TaParserException : Exception
{
    internal TaParserException() : base("")
    {
    }
}
