using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct PCW
{
    [FieldOffset(0)] internal uint full;
    [FieldOffset(0)] internal byte obj_ctrl;

    internal uint UV_16bit
    {
        readonly get => (full >> 0) & 0x1u;
        set => full = (full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint Gouraud
    {
        readonly get => (full >> 1) & 0x1u;
        set => full = (full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint Offset
    {
        readonly get => (full >> 2) & 0x1u;
        set => full = (full & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint Texture
    {
        readonly get => (full >> 3) & 0x1u;
        set => full = (full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint Col_Type
    {
        readonly get => (full >> 4) & 0x3u;
        set => full = (full & ~(0x3u << 4)) | ((value & 0x3u) << 4);
    }

    internal uint Volume
    {
        readonly get => (full >> 6) & 0x1u;
        set => full = (full & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint Shadow
    {
        readonly get => (full >> 7) & 0x1u;
        set => full = (full & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint User_Clip
    {
        readonly get => (full >> 16) & 0x3u;
        set => full = (full & ~(0x3u << 16)) | ((value & 0x3u) << 16);
    }

    internal uint Strip_Len
    {
        readonly get => (full >> 18) & 0x3u;
        set => full = (full & ~(0x3u << 18)) | ((value & 0x3u) << 18);
    }

    internal uint Group_En
    {
        readonly get => (full >> 23) & 0x1u;
        set => full = (full & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint ListType
    {
        readonly get => (full >> 24) & 0x7u;
        set => full = (full & ~(0x7u << 24)) | ((value & 0x7u) << 24);
    }

    internal uint EndOfStrip
    {
        readonly get => (full >> 28) & 0x1u;
        set => full = (full & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint ParaType
    {
        readonly get => (full >> 29) & 0x7u;
        set => full = (full & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct ISP_TSP
{
    [FieldOffset(0)] internal uint full;

    internal uint DCalcCtrl
    {
        readonly get => (full >> 20) & 0x1u;
        set => full = (full & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint CacheBypass
    {
        readonly get => (full >> 21) & 0x1u;
        set => full = (full & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint UV_16b
    {
        readonly get => (full >> 22) & 0x1u;
        set => full = (full & ~(0x1u << 22)) | ((value & 0x1u) << 22);
    }

    internal uint Gouraud
    {
        readonly get => (full >> 23) & 0x1u;
        set => full = (full & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint Offset
    {
        readonly get => (full >> 24) & 0x1u;
        set => full = (full & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint Texture
    {
        readonly get => (full >> 25) & 0x1u;
        set => full = (full & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint ZWriteDis
    {
        readonly get => (full >> 26) & 0x1u;
        set => full = (full & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint CullMode
    {
        readonly get => (full >> 27) & 0x3u;
        set => full = (full & ~(0x3u << 27)) | ((value & 0x3u) << 27);
    }

    internal uint DepthMode
    {
        readonly get => (full >> 29) & 0x7u;
        set => full = (full & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct ISP_Modvol
{
    [FieldOffset(0)] internal uint full;

    internal uint id
    {
        readonly get => (full >> 0) & 0x3ffffffu;
        set => full = (full & ~(0x3ffffffu << 0)) | ((value & 0x3ffffffu) << 0);
    }

    internal uint VolumeLast
    {
        readonly get => (full >> 26) & 0x1u;
        set => full = (full & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint CullMode
    {
        readonly get => (full >> 27) & 0x3u;
        set => full = (full & ~(0x3u << 27)) | ((value & 0x3u) << 27);
    }

    internal uint DepthMode
    {
        readonly get => (full >> 29) & 0x7u;
        set => full = (full & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct TSP
{
    [FieldOffset(0)] internal uint full;

    internal uint TexV
    {
        readonly get => (full >> 0) & 0x7u;
        set => full = (full & ~(0x7u << 0)) | ((value & 0x7u) << 0);
    }

    internal uint TexU
    {
        readonly get => (full >> 3) & 0x7u;
        set => full = (full & ~(0x7u << 3)) | ((value & 0x7u) << 3);
    }

    internal uint ShadInstr
    {
        readonly get => (full >> 6) & 0x3u;
        set => full = (full & ~(0x3u << 6)) | ((value & 0x3u) << 6);
    }

    internal uint MipMapD
    {
        readonly get => (full >> 8) & 0xfu;
        set => full = (full & ~(0xfu << 8)) | ((value & 0xfu) << 8);
    }

    internal uint SupSample
    {
        readonly get => (full >> 12) & 0x1u;
        set => full = (full & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }

    internal uint FilterMode
    {
        readonly get => (full >> 13) & 0x3u;
        set => full = (full & ~(0x3u << 13)) | ((value & 0x3u) << 13);
    }

    internal uint ClampV
    {
        readonly get => (full >> 15) & 0x1u;
        set => full = (full & ~(0x1u << 15)) | ((value & 0x1u) << 15);
    }

    internal uint ClampU
    {
        readonly get => (full >> 16) & 0x1u;
        set => full = (full & ~(0x1u << 16)) | ((value & 0x1u) << 16);
    }

    internal uint FlipV
    {
        readonly get => (full >> 17) & 0x1u;
        set => full = (full & ~(0x1u << 17)) | ((value & 0x1u) << 17);
    }

    internal uint FlipU
    {
        readonly get => (full >> 18) & 0x1u;
        set => full = (full & ~(0x1u << 18)) | ((value & 0x1u) << 18);
    }

    internal uint IgnoreTexA
    {
        readonly get => (full >> 19) & 0x1u;
        set => full = (full & ~(0x1u << 19)) | ((value & 0x1u) << 19);
    }

    internal uint UseAlpha
    {
        readonly get => (full >> 20) & 0x1u;
        set => full = (full & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint ColorClamp
    {
        readonly get => (full >> 21) & 0x1u;
        set => full = (full & ~(0x1u << 21)) | ((value & 0x1u) << 21);
    }

    internal uint FogCtrl
    {
        readonly get => (full >> 22) & 0x3u;
        set => full = (full & ~(0x3u << 22)) | ((value & 0x3u) << 22);
    }

    internal uint DstSelect
    {
        readonly get => (full >> 24) & 0x1u;
        set => full = (full & ~(0x1u << 24)) | ((value & 0x1u) << 24);
    }

    internal uint SrcSelect
    {
        readonly get => (full >> 25) & 0x1u;
        set => full = (full & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint DstInstr
    {
        readonly get => (full >> 26) & 0x7u;
        set => full = (full & ~(0x7u << 26)) | ((value & 0x7u) << 26);
    }

    internal uint SrcInstr
    {
        readonly get => (full >> 29) & 0x7u;
        set => full = (full & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct TCW
{
    [FieldOffset(0)] internal uint full;

    internal uint TexAddr
    {
        readonly get => (full >> 0) & 0x1fffffu;
        set => full = (full & ~(0x1fffffu << 0)) | ((value & 0x1fffffu) << 0);
    }

    internal uint StrideSel
    {
        readonly get => (full >> 25) & 0x1u;
        set => full = (full & ~(0x1u << 25)) | ((value & 0x1u) << 25);
    }

    internal uint ScanOrder
    {
        readonly get => (full >> 26) & 0x1u;
        set => full = (full & ~(0x1u << 26)) | ((value & 0x1u) << 26);
    }

    internal uint PixelFmt
    {
        readonly get => (full >> 27) & 0x7u;
        set => full = (full & ~(0x7u << 27)) | ((value & 0x7u) << 27);
    }

    internal uint VQ_Comp
    {
        readonly get => (full >> 30) & 0x1u;
        set => full = (full & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint MipMapped
    {
        readonly get => (full >> 31) & 0x1u;
        set => full = (full & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }

    internal uint PalSelect
    {
        readonly get => (full >> 21) & 0x3fu;
        set => full = (full & ~(0x3fu << 21)) | ((value & 0x3fu) << 21);
    }
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 32)]
internal unsafe struct Ta_Dma
{
    [FieldOffset(0)] internal PCW pcw;
    [FieldOffset(4)] internal fixed byte data_8[32 - 4];
    [FieldOffset(4)] internal fixed uint data_32[8 - 1];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam0
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal uint ign1;
    internal uint ign2;
    internal uint SDMA_SIZE;
    internal uint SDMA_ADDR;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam1
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal float FaceColorA;
    internal float FaceColorR;
    internal float FaceColorG;
    internal float FaceColorB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam2A
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal uint ign1;
    internal uint ign2;
    internal uint SDMA_SIZE;
    internal uint SDMA_ADDR;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam2B
{
    internal float FaceColorA;
    internal float FaceColorR;
    internal float FaceColorG;
    internal float FaceColorB;
    internal float FaceOffsetA;
    internal float FaceOffsetR;
    internal float FaceOffsetG;
    internal float FaceOffsetB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam3
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal TSP tsp1;
    internal TCW tcw1;
    internal uint SDMA_SIZE;
    internal uint SDMA_ADDR;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam4A
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal TSP tsp1;
    internal TCW tcw1;
    internal uint SDMA_SIZE;
    internal uint SDMA_ADDR;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_PolyParam4B
{
    internal float FaceColor0A;
    internal float FaceColor0R;
    internal float FaceColor0G;
    internal float FaceColor0B;
    internal float FaceColor1A;
    internal float FaceColor1R;
    internal float FaceColor1G;
    internal float FaceColor1B;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_ModVolParam
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal fixed uint ign[8 - 2];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_SpriteParam
{
    internal PCW pcw;
    internal ISP_TSP isp;
    internal TSP tsp;
    internal TCW tcw;
    internal uint BaseCol;
    internal uint OffsCol;
    internal uint SDMA_SIZE;
    internal uint SDMA_ADDR;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex0
{
    internal fixed float xyz[3];
    internal uint ignore_1;
    internal uint ignore_2;
    internal uint BaseCol;
    internal uint ignore_3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex1
{
    internal fixed float xyz[3];
    internal float BaseA;
    internal float BaseR;
    internal float BaseG;
    internal float BaseB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex2
{
    internal fixed float xyz[3];
    internal uint ignore_1;
    internal uint ignore_2;
    internal float BaseInt;
    internal uint ignore_3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex3
{
    internal fixed float xyz[3];
    internal float u;
    internal float v;
    internal uint BaseCol;
    internal uint OffsCol;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex4
{
    internal fixed float xyz[3];
    internal ushort v;
    internal ushort u;
    internal uint ignore_1;
    internal uint BaseCol;
    internal uint OffsCol;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex5A
{
    internal fixed float xyz[3];
    internal float u;
    internal float v;
    internal uint ignore_1;
    internal uint ignore_2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex5B
{
    internal float BaseA;
    internal float BaseR;
    internal float BaseG;
    internal float BaseB;
    internal float OffsA;
    internal float OffsR;
    internal float OffsG;
    internal float OffsB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex6A
{
    internal fixed float xyz[3];
    internal ushort v;
    internal ushort u;
    internal uint ignore_1;
    internal uint ignore_2;
    internal uint ignore_3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex6B
{
    internal float BaseA;
    internal float BaseR;
    internal float BaseG;
    internal float BaseB;
    internal float OffsA;
    internal float OffsR;
    internal float OffsG;
    internal float OffsB;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex7
{
    internal fixed float xyz[3];
    internal float u;
    internal float v;
    internal float BaseInt;
    internal float OffsInt;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex8
{
    internal fixed float xyz[3];
    internal ushort v;
    internal ushort u;
    internal uint ignore_1;
    internal float BaseInt;
    internal float OffsInt;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex9
{
    internal fixed float xyz[3];
    internal uint BaseCol0;
    internal uint BaseCol1;
    internal uint ignore_1;
    internal uint ignore_2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex10
{
    internal fixed float xyz[3];
    internal float BaseInt0;
    internal float BaseInt1;
    internal uint ignore_1;
    internal uint ignore_2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex11A
{
    internal fixed float xyz[3];
    internal float u0;
    internal float v0;
    internal uint BaseCol0;
    internal uint OffsCol0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex11B
{
    internal float u1;
    internal float v1;
    internal uint BaseCol1;
    internal uint OffsCol1;
    internal uint ignore_1;
    internal uint ignore_2;
    internal uint ignore_3;
    internal uint ignore_4;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex12A
{
    internal fixed float xyz[3];
    internal ushort v0;
    internal ushort u0;
    internal uint ignore_1;
    internal uint BaseCol0;
    internal uint OffsCol0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex12B
{
    internal ushort v1;
    internal ushort u1;
    internal uint ignore_2;
    internal uint BaseCol1;
    internal uint OffsCol1;
    internal uint ignore_3;
    internal uint ignore_4;
    internal uint ignore_5;
    internal uint ignore_6;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex13A
{
    internal fixed float xyz[3];
    internal float u0;
    internal float v0;
    internal float BaseInt0;
    internal float OffsInt0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex13B
{
    internal float u1;
    internal float v1;
    internal float BaseInt1;
    internal float OffsInt1;
    internal uint ignore_1;
    internal uint ignore_2;
    internal uint ignore_3;
    internal uint ignore_4;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_Vertex14A
{
    internal fixed float xyz[3];
    internal ushort v0;
    internal ushort u0;
    internal uint ignore_1;
    internal float BaseInt0;
    internal float OffsInt0;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Vertex14B
{
    internal ushort v1;
    internal ushort u1;
    internal uint ignore_2;
    internal float BaseInt1;
    internal float OffsInt1;
    internal uint ignore_3;
    internal uint ignore_4;
    internal uint ignore_5;
    internal uint ignore_6;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Sprite0A
{
    internal float x0;
    internal float y0;
    internal float z0;
    internal float x1;
    internal float y1;
    internal float z1;
    internal float x2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Sprite0B
{
    internal float y2;
    internal float z2;
    internal float x3;
    internal float y3;
    internal uint ignore_1;
    internal uint ignore_2;
    internal uint ignore_3;
    internal uint ignore_4;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Sprite1A
{
    internal float x0;
    internal float y0;
    internal float z0;
    internal float x1;
    internal float y1;
    internal float z1;
    internal float x2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_Sprite1B
{
    internal float y2;
    internal float z2;
    internal float x3;
    internal float y3;
    internal uint ignore_1;
    internal ushort v0;
    internal ushort u0;
    internal ushort v1;
    internal ushort u1;
    internal ushort v2;
    internal ushort u2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct TA_ModVolA
{
    internal PCW pcw;
    internal float x0;
    internal float y0;
    internal float z0;
    internal float x1;
    internal float y1;
    internal float z1;
    internal float x2;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal unsafe struct TA_ModVolB
{
    internal float y2;
    internal float z2;
    internal fixed uint ignore[6];
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 64)]
internal unsafe struct TA_VertexParam
{
    [FieldOffset(0)] internal PCW pcw;
    [FieldOffset(4)] internal fixed byte Raw[64 - 4];
    [FieldOffset(4)] internal TA_Vertex0 vtx0;
    [FieldOffset(4)] internal TA_Vertex1 vtx1;
    [FieldOffset(4)] internal TA_Vertex2 vtx2;
    [FieldOffset(4)] internal TA_Vertex3 vtx3;
    [FieldOffset(4)] internal TA_Vertex4 vtx4;
    [FieldOffset(4)] internal TA_Vertex5A vtx5A;
    [FieldOffset(32)] internal TA_Vertex5B vtx5B;
    [FieldOffset(4)] internal TA_Vertex6A vtx6A;
    [FieldOffset(32)] internal TA_Vertex6B vtx6B;
    [FieldOffset(4)] internal TA_Vertex7 vtx7;
    [FieldOffset(4)] internal TA_Vertex8 vtx8;
    [FieldOffset(4)] internal TA_Vertex9 vtx9;
    [FieldOffset(4)] internal TA_Vertex10 vtx10;
    [FieldOffset(4)] internal TA_Vertex11A vtx11A;
    [FieldOffset(32)] internal TA_Vertex11B vtx11B;
    [FieldOffset(4)] internal TA_Vertex12A vtx12A;
    [FieldOffset(32)] internal TA_Vertex12B vtx12B;
    [FieldOffset(4)] internal TA_Vertex13A vtx13A;
    [FieldOffset(32)] internal TA_Vertex13B vtx13B;
    [FieldOffset(4)] internal TA_Vertex14A vtx14A;
    [FieldOffset(32)] internal TA_Vertex14B vtx14B;
    [FieldOffset(4)] internal TA_Sprite0A spr0A;
    [FieldOffset(32)] internal TA_Sprite0B spr0B;
    [FieldOffset(4)] internal TA_Sprite1A spr1A;
    [FieldOffset(32)] internal TA_Sprite1B spr1B;
    [FieldOffset(0)] internal TA_ModVolA mvolA;
    [FieldOffset(32)] internal TA_ModVolB mvolB;
}

internal static unsafe partial class PowerVr2Ta
{
    internal const uint ListType_Opaque = 0;
    internal const uint ListType_Opaque_Modifier_Volume = 1;
    internal const uint ListType_Translucent = 2;
    internal const uint ListType_Translucent_Modifier_Volume = 3;
    internal const uint ListType_Punch_Through = 4;

    internal static bool IsModVolList(uint list) => (list & 1) != 0;

    internal const uint ParamType_End_Of_List = 0;
    internal const uint ParamType_User_Tile_Clip = 1;
    internal const uint ParamType_Object_List_Set = 2;
    internal const uint ParamType_Polygon_or_Modifier_Volume = 4;
    internal const uint ParamType_Sprite = 5;
    internal const uint ParamType_Vertex_Parameter = 7;
    internal const uint ParamType_Reserved_1 = 3;
    internal const uint ParamType_Reserved_2 = 6;
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
internal struct RegionArrayTile
{
    [FieldOffset(0)] internal uint full;

    internal uint X
    {
        readonly get => (full >> 2) & 0x3fu;
        set => full = (full & ~(0x3fu << 2)) | ((value & 0x3fu) << 2);
    }

    internal uint Y
    {
        readonly get => (full >> 8) & 0x3fu;
        set => full = (full & ~(0x3fu << 8)) | ((value & 0x3fu) << 8);
    }

    internal uint FlushAccumulate
    {
        readonly get => (full >> 28) & 0x1u;
        set => full = (full & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint PreSort
    {
        readonly get => (full >> 29) & 0x1u;
        set => full = (full & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint NoZClear
    {
        readonly get => (full >> 30) & 0x1u;
        set => full = (full & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint LastRegion
    {
        readonly get => (full >> 31) & 0x1u;
        set => full = (full & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }
}

internal enum PixelFormat
{
    Pixel1555 = 0,
    Pixel565 = 1,
    Pixel4444 = 2,
    PixelYUV = 3,
    PixelBumpMap = 4,
    PixelPal4 = 5,
    PixelPal8 = 6,
    PixelReserved = 7
}
