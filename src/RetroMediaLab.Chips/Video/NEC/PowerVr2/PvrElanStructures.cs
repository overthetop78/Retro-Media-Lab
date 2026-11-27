using System;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

[StructLayout(LayoutKind.Sequential)]
internal struct ElanPcw
{
    internal enum Command
    {
        Null = 0,
        Matrix2 = 1,
        Matrix1 = 2,
        ProjMatrix = 3,
        MatrixOrLight = 4,
        Gmp = 5,
        Ich = 7,
        Model = 8,
        RegisterWait = 0xe,
        Link = 0xf
    }

    internal uint Full;

    internal byte ObjectControl
    {
        readonly get => (byte)Full;
        set => Full = (Full & 0xffffff00u) | value;
    }

    internal uint Uv16Bit
    {
        readonly get => (Full >> 0) & 0x1u;
        set => Full = (Full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint Gouraud
    {
        readonly get => (Full >> 1) & 0x1u;
        set => Full = (Full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint Offset
    {
        readonly get => (Full >> 2) & 0x1u;
        set => Full = (Full & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint Texture
    {
        readonly get => (Full >> 3) & 0x1u;
        set => Full = (Full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint ColType
    {
        readonly get => (Full >> 4) & 0x3u;
        set => Full = (Full & ~(0x3u << 4)) | ((value & 0x3u) << 4);
    }

    internal uint Volume
    {
        readonly get => (Full >> 6) & 0x1u;
        set => Full = (Full & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint Shadow
    {
        readonly get => (Full >> 7) & 0x1u;
        set => Full = (Full & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint N2Command
    {
        readonly get => (Full >> 8) & 0xfu;
        set => Full = (Full & ~(0xfu << 8)) | ((value & 0xfu) << 8);
    }

    internal uint UserClip
    {
        readonly get => (Full >> 16) & 0x3u;
        set => Full = (Full & ~(0x3u << 16)) | ((value & 0x3u) << 16);
    }

    internal uint StripLen
    {
        readonly get => (Full >> 18) & 0x3u;
        set => Full = (Full & ~(0x3u << 18)) | ((value & 0x3u) << 18);
    }

    internal uint ParallelLight
    {
        readonly get => (Full >> 20) & 0x1u;
        set => Full = (Full & ~(0x1u << 20)) | ((value & 0x1u) << 20);
    }

    internal uint GroupEnable
    {
        readonly get => (Full >> 23) & 0x1u;
        set => Full = (Full & ~(0x1u << 23)) | ((value & 0x1u) << 23);
    }

    internal uint ListType
    {
        readonly get => (Full >> 24) & 0x7u;
        set => Full = (Full & ~(0x7u << 24)) | ((value & 0x7u) << 24);
    }

    internal uint Naomi2
    {
        readonly get => (Full >> 27) & 0x1u;
        set => Full = (Full & ~(0x1u << 27)) | ((value & 0x1u) << 27);
    }

    internal uint EndOfStrip
    {
        readonly get => (Full >> 28) & 0x1u;
        set => Full = (Full & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }

    internal uint ParaType
    {
        readonly get => (Full >> 29) & 0x7u;
        set => Full = (Full & ~(0x7u << 29)) | ((value & 0x7u) << 29);
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanBase
{
    internal ElanPcw Pcw;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanModelParameters
{
    private uint _value;

    internal uint CwCulling
    {
        readonly get => (_value >> 27) & 0x1u;
        set => _value = (_value & ~(0x1u << 27)) | ((value & 0x1u) << 27);
    }

    internal uint OpenVolume
    {
        readonly get => (_value >> 28) & 0x1u;
        set => _value = (_value & ~(0x1u << 28)) | ((value & 0x1u) << 28);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal struct ElanModel
{
    internal ElanBase Base;
    internal ElanModelParameters Param;
    internal TSP Tsp;
    internal uint Zero;
    internal uint Offset;
    internal uint One;
    internal uint Size;
    internal uint Reserved0;
}

[StructLayout(LayoutKind.Sequential, Size = 160)]
internal unsafe struct ElanInstanceMatrix
{
    internal ElanBase Base;
    internal uint Id1;
    internal uint Id2;
    internal fixed uint Reserved[5];

    internal uint Reserved1;
    internal float EnvMapU;
    internal float Lm00;
    internal float Lm10;
    internal float Lm20;
    internal float Lm01;
    internal float Lm11;
    internal float Lm21;
    internal float Lm02;
    internal float Lm12;
    internal float Lm22;
    internal float EnvMapV;
    internal fixed float Reserved2[4];

    internal uint Reserved3;
    internal float Near;
    internal float Tm00;
    internal float Tm01;
    internal float Tm02;
    internal float Tm10;
    internal float Tm11;
    internal float Tm12;
    internal float Tm20;
    internal float Tm21;
    internal float Tm22;
    internal float Tm30;
    internal float Tm31;
    internal float Tm32;
    internal float Far;
    internal float Mproj6;

    internal readonly bool IsInstanceMatrix() => Id1 == 0xf && Id2 == 0x7f;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal unsafe struct ElanProjMatrix
{
    internal ElanBase Base;
    internal uint Reserved;
    internal float Fx;
    internal float Tx;
    internal float Fy;
    internal float Ty;
    internal fixed uint Reserved0[2];
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanGloss
{
    private uint _value;

    internal uint Frac0
    {
        readonly get => (_value >> 0) & 0x1fu;
        set => _value = (_value & ~(0x1fu << 0)) | ((value & 0x1fu) << 0);
    }

    internal uint Exp0
    {
        readonly get => (_value >> 5) & 0x7u;
        set => _value = (_value & ~(0x7u << 5)) | ((value & 0x7u) << 5);
    }

    internal uint Frac1
    {
        readonly get => (_value >> 8) & 0x1fu;
        set => _value = (_value & ~(0x1fu << 8)) | ((value & 0x1fu) << 8);
    }

    internal uint Exp1
    {
        readonly get => (_value >> 13) & 0x7u;
        set => _value = (_value & ~(0x7u << 13)) | ((value & 0x7u) << 13);
    }

    internal readonly float GetCoef0() => MathF.Pow(2f, Exp0 - 1f) * (1 + (float)Frac0 / 32);
    internal readonly float GetCoef1() => MathF.Pow(2f, Exp1 - 1f) * (1 + (float)Frac1 / 32);
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanParameterSelect
{
    internal uint Full;

    internal uint D0
    {
        readonly get => (Full >> 0) & 0x1u;
        set => Full = (Full & ~(0x1u << 0)) | ((value & 0x1u) << 0);
    }

    internal uint S0
    {
        readonly get => (Full >> 1) & 0x1u;
        set => Full = (Full & ~(0x1u << 1)) | ((value & 0x1u) << 1);
    }

    internal uint A0
    {
        readonly get => (Full >> 2) & 0x1u;
        set => Full = (Full & ~(0x1u << 2)) | ((value & 0x1u) << 2);
    }

    internal uint F0
    {
        readonly get => (Full >> 3) & 0x1u;
        set => Full = (Full & ~(0x1u << 3)) | ((value & 0x1u) << 3);
    }

    internal uint D1
    {
        readonly get => (Full >> 4) & 0x1u;
        set => Full = (Full & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint S1
    {
        readonly get => (Full >> 5) & 0x1u;
        set => Full = (Full & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint A1
    {
        readonly get => (Full >> 6) & 0x1u;
        set => Full = (Full & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint F1
    {
        readonly get => (Full >> 7) & 0x1u;
        set => Full = (Full & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint Vol1UsesVol0Uv
    {
        readonly get => (Full >> 8) & 0x1u;
        set => Full = (Full & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint B0
    {
        readonly get => (Full >> 9) & 0x1u;
        set => Full = (Full & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint B1
    {
        readonly get => (Full >> 10) & 0x1u;
        set => Full = (Full & ~(0x1u << 10)) | ((value & 0x1u) << 10);
    }

    internal uint E0
    {
        readonly get => (Full >> 11) & 0x1u;
        set => Full = (Full & ~(0x1u << 11)) | ((value & 0x1u) << 11);
    }

    internal uint E1
    {
        readonly get => (Full >> 12) & 0x1u;
        set => Full = (Full & ~(0x1u << 12)) | ((value & 0x1u) << 12);
    }
}

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct ElanGmp
{
    internal ElanBase Base;
    internal ElanGloss Gloss;
    internal ElanParameterSelect ParamSelect;
    internal uint Diffuse0;
    internal uint Specular0;
    internal uint Diffuse1;
    internal uint Specular1;
    internal uint Reserved;
    internal uint Reserved0;
    internal uint Tex0;
    internal uint Reserved1;
    internal uint Pal0;
    internal uint Reserved2;
    internal uint Tex1;
    internal uint Reserved3;
    internal uint Pal1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanHeaderAndNormal
{
    internal uint Full;

    internal uint Nx
    {
        readonly get => (Full >> 0) & 0xffu;
        set => Full = (Full & ~(0xffu << 0)) | ((value & 0xffu) << 0);
    }

    internal uint Ny
    {
        readonly get => (Full >> 8) & 0xffu;
        set => Full = (Full & ~(0xffu << 8)) | ((value & 0xffu) << 8);
    }

    internal uint Nz
    {
        readonly get => (Full >> 16) & 0xffu;
        set => Full = (Full & ~(0xffu << 16)) | ((value & 0xffu) << 16);
    }

    internal uint Strip
    {
        readonly get => (Full >> 29) & 0x1u;
        set => Full = (Full & ~(0x1u << 29)) | ((value & 0x1u) << 29);
    }

    internal uint Fan
    {
        readonly get => (Full >> 30) & 0x1u;
        set => Full = (Full & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint EndOfStrip
    {
        readonly get => (Full >> 31) & 0x1u;
        set => Full = (Full & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }

    internal readonly bool IsFirstOrSecond() => Strip == 0 && Fan == 0;
    internal readonly bool IsThird() => Strip == 1 && Fan == 1;
    internal readonly bool IsFan() => Strip == 0 && Fan == 1;
    internal readonly bool IsStrip() => Strip == 1 && Fan == 0;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertex
{
    internal ElanHeaderAndNormal Header;
    internal float X;
    internal float Y;
    internal float Z;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanNormal
{
    internal float Nx;
    internal float Ny;
    internal float Nz;
    internal uint Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanUnpackedUv
{
    internal float U;
    internal float V;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanPackedUv
{
    internal uint Uv0;
    internal uint Uv1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanPackedRgb
{
    internal uint Argb0;
    internal uint Argb1;
}

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct ElanBumpScale
{
    internal byte BumpDegree;
    internal byte FixedOffset;
    internal fixed byte Reserved[2];
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanBumpVector
{
    internal sbyte X;
    internal sbyte Y;
    internal sbyte Z;
    internal byte Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanBumpMap
{
    internal ElanBumpScale ScaleFactor;
    internal ElanBumpVector Tangent;
    internal ElanBumpVector Bitangent;
    internal uint Reserved;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertexVu
{
    internal ElanVertex Vertex;
    internal ElanUnpackedUv Uv;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertexVnu
{
    internal ElanVertex Vertex;
    internal ElanNormal Normal;
    internal ElanUnpackedUv Uv;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertexVur
{
    internal ElanVertex Vertex;
    internal ElanUnpackedUv Uv;
    internal ElanPackedRgb Rgb;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertexVub
{
    internal ElanVertex Vertex;
    internal ElanUnpackedUv Uv;
    internal ElanBumpMap Bump;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanVertexVr
{
    internal ElanVertex Vertex;
    internal ElanPackedRgb Rgb;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal unsafe struct ElanIchList
{
    internal const uint VertexTypeV = 0x00000002;
    internal const uint VertexTypeVu = 0x0000000a;
    internal const uint VertexTypeVnu = 0x0000000e;
    internal const uint VertexTypeVr = 0x00000042;
    internal const uint VertexTypeVur = 0x0000004a;
    internal const uint VertexTypeVub = 0x0000010a;

    internal ElanBase Base;
    internal ISP_TSP Isp;
    internal TSP Tsp0;
    internal TCW Tcw0;
    internal TSP Tsp1;
    internal TCW Tcw1;
    internal uint Flags;
    internal uint VtxCount;

    internal readonly uint VertexSize() => Flags switch
    {
        VertexTypeV => (uint)sizeof(ElanVertex),
        VertexTypeVu => (uint)sizeof(ElanVertexVu),
        VertexTypeVnu => (uint)sizeof(ElanVertexVnu),
        VertexTypeVr => (uint)sizeof(ElanVertexVr),
        VertexTypeVur => (uint)sizeof(ElanVertexVur),
        VertexTypeVub => (uint)sizeof(ElanVertexVub),
        _ => 0
    };
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal unsafe struct ElanRegisterWait
{
    internal ElanBase Base;
    internal uint Offset;
    internal uint Reserved;
    internal uint Mask;
    internal fixed uint Reserved0[4];
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal unsafe struct ElanLink
{
    internal ElanBase Base;
    internal uint Offset;
    internal uint VramAddress;
    internal uint Size;
    internal fixed uint Reserved0[4];
}

internal static class ElanLimits
{
    internal const int MaxLights = 16;
}

[StructLayout(LayoutKind.Sequential)]
internal struct ElanLightModel
{
    internal ElanBase Base;
    private uint _flags;
    internal ushort DiffuseMask0;
    internal ushort SpecularMask0;
    internal uint AmbientBase0;
    internal uint AmbientOffset0;
    internal ushort DiffuseMask1;
    internal ushort SpecularMask1;
    internal uint AmbientBase1;
    internal uint AmbientOffset1;

    internal uint Zero
    {
        readonly get => (_flags >> 0) & 0xfu;
        set => _flags = (_flags & ~(0xfu << 0)) | ((value & 0xfu) << 0);
    }

    internal uint LightFlag
    {
        readonly get => (_flags >> 4) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 4)) | ((value & 0x1u) << 4);
    }

    internal uint UseAmbientBase0
    {
        readonly get => (_flags >> 5) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 5)) | ((value & 0x1u) << 5);
    }

    internal uint UseAmbientOffset0
    {
        readonly get => (_flags >> 6) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 6)) | ((value & 0x1u) << 6);
    }

    internal uint UseAmbientBase1
    {
        readonly get => (_flags >> 7) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 7)) | ((value & 0x1u) << 7);
    }

    internal uint UseAmbientOffset1
    {
        readonly get => (_flags >> 8) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 8)) | ((value & 0x1u) << 8);
    }

    internal uint UseBaseOver
    {
        readonly get => (_flags >> 9) & 0x1u;
        set => _flags = (_flags & ~(0x1u << 9)) | ((value & 0x1u) << 9);
    }

    internal uint BumpId1
    {
        readonly get => (_flags >> 12) & 0xfu;
        set => _flags = (_flags & ~(0xfu << 12)) | ((value & 0xfu) << 12);
    }

    internal uint BumpId2
    {
        readonly get => (_flags >> 16) & 0xfu;
        set => _flags = (_flags & ~(0xfu << 16)) | ((value & 0xfu) << 16);
    }

    internal readonly bool IsDiffuse(int lightId, int volume)
    {
        if (volume == 0)
            return (DiffuseMask0 & (1 << lightId)) != 0;
        return (DiffuseMask1 & (1 << lightId)) != 0;
    }

    internal readonly bool IsSpecular(int lightId, int volume)
    {
        if (volume == 0)
            return (SpecularMask0 & (1 << lightId)) != 0;
        return (SpecularMask1 & (1 << lightId)) != 0;
    }
}

internal enum ElanLightMethod
{
    SingleSided,
    DoubleSided,
    DoubleSidedWithTolerance,
    SpecialEffect,
    ThinSurface,
    BumpMap
}

internal enum ElanLightFunction
{
    BaseDiffBaseSpecAdd,
    BaseDiffOffsSpecAdd,
    OffsDiffBaseSpecAdd,
    OffsDiffOffsSpecAdd,
    AlphaDiffAdd,
    AlphaAttenAdd,
    FogDiffAdd,
    FogAttenuationAdd,
    BaseDiffBaseSpecSub,
    BaseDiffOffsSpecSub,
    OffsDiffBaseSpecSub,
    OffsDiffOffsSpecSub,
    AlphaDiffSub,
    AlphaAttenSub,
    FogDiffSub,
    FogAttenSub
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal unsafe struct ElanParallelLight
{
    internal ElanBase Base;
    private uint _color;
    private uint _direction;
    internal fixed uint Reserved2[5];

    internal uint LightId
    {
        readonly get => (_color >> 0) & 0xfu;
        set => _color = (_color & ~(0xfu << 0)) | ((value & 0xfu) << 0);
    }

    internal uint Blue
    {
        readonly get => (_color >> 8) & 0xffu;
        set => _color = (_color & ~(0xffu << 8)) | ((value & 0xffu) << 8);
    }

    internal uint Green
    {
        readonly get => (_color >> 16) & 0xffu;
        set => _color = (_color & ~(0xffu << 16)) | ((value & 0xffu) << 16);
    }

    internal uint Red
    {
        readonly get => (_color >> 24) & 0xffu;
        set => _color = (_color & ~(0xffu << 24)) | ((value & 0xffu) << 24);
    }

    internal uint DirZ
    {
        readonly get => (_direction >> 0) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 0)) | ((value & 0xffu) << 0);
    }

    internal uint DirY
    {
        readonly get => (_direction >> 8) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 8)) | ((value & 0xffu) << 8);
    }

    internal uint DirX
    {
        readonly get => (_direction >> 16) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 16)) | ((value & 0xffu) << 16);
    }

    internal uint Routing
    {
        readonly get => (_direction >> 24) & 0xfu;
        set => _direction = (_direction & ~(0xfu << 24)) | ((value & 0xfu) << 24);
    }

    internal uint Dmode
    {
        readonly get => (_direction >> 28) & 0x3u;
        set => _direction = (_direction & ~(0x3u << 28)) | ((value & 0x3u) << 28);
    }

    internal readonly float GetDirX() => ((unchecked((sbyte)DirX) << 4) | (int)((Base.Pcw.Full >> 16) & 0xf)) / 2047f;
    internal readonly float GetDirY() => ((unchecked((sbyte)DirY) << 4) | (int)((Base.Pcw.Full >> 4) & 0xf)) / 2047f;
    internal readonly float GetDirZ() => ((unchecked((sbyte)DirZ) << 4) | (int)(Base.Pcw.Full & 0xf)) / 2047f;
}

[StructLayout(LayoutKind.Sequential, Size = 32)]
internal struct ElanPointLight
{
    internal ElanBase Base;
    private uint _color;
    private uint _direction;
    internal float PosX;
    internal float PosY;
    internal float PosZ;
    private ushort _distA;
    private ushort _distB;
    private ushort _angleA;
    private ushort _angleB;

    internal uint LightId
    {
        readonly get => (_color >> 0) & 0xfu;
        set => _color = (_color & ~(0xfu << 0)) | ((value & 0xfu) << 0);
    }

    internal uint Dmode
    {
        readonly get => (_color >> 5) & 0x7u;
        set => _color = (_color & ~(0x7u << 5)) | ((value & 0x7u) << 5);
    }

    internal uint Blue
    {
        readonly get => (_color >> 8) & 0xffu;
        set => _color = (_color & ~(0xffu << 8)) | ((value & 0xffu) << 8);
    }

    internal uint Green
    {
        readonly get => (_color >> 16) & 0xffu;
        set => _color = (_color & ~(0xffu << 16)) | ((value & 0xffu) << 16);
    }

    internal uint Red
    {
        readonly get => (_color >> 24) & 0xffu;
        set => _color = (_color & ~(0xffu << 24)) | ((value & 0xffu) << 24);
    }

    internal uint DirZ
    {
        readonly get => (_direction >> 0) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 0)) | ((value & 0xffu) << 0);
    }

    internal uint DirY
    {
        readonly get => (_direction >> 8) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 8)) | ((value & 0xffu) << 8);
    }

    internal uint DirX
    {
        readonly get => (_direction >> 16) & 0xffu;
        set => _direction = (_direction & ~(0xffu << 16)) | ((value & 0xffu) << 16);
    }

    internal uint Routing
    {
        readonly get => (_direction >> 24) & 0xfu;
        set => _direction = (_direction & ~(0xfu << 24)) | ((value & 0xfu) << 24);
    }

    internal uint Smode
    {
        readonly get => (_direction >> 28) & 0x3u;
        set => _direction = (_direction & ~(0x3u << 28)) | ((value & 0x3u) << 28);
    }

    internal uint One
    {
        readonly get => (_direction >> 30) & 0x1u;
        set => _direction = (_direction & ~(0x1u << 30)) | ((value & 0x1u) << 30);
    }

    internal uint Dattenmode
    {
        readonly get => (_direction >> 31) & 0x1u;
        set => _direction = (_direction & ~(0x1u << 31)) | ((value & 0x1u) << 31);
    }

    internal readonly float GetDirX() => ((unchecked((sbyte)DirX) << 4) | (int)((Base.Pcw.Full >> 16) & 0xf)) / 2047f;
    internal readonly float GetDirY() => ((unchecked((sbyte)DirY) << 4) | (int)((Base.Pcw.Full >> 4) & 0xf)) / 2047f;
    internal readonly float GetDirZ() => ((unchecked((sbyte)DirZ) << 4) | (int)(Base.Pcw.Full & 0xf)) / 2047f;

    private static float F16ToF32(ushort value) => BitConverter.UInt32BitsToSingle((uint)value << 16);

    internal readonly float DistA() => F16ToF32(_distA);
    internal readonly float DistB() => F16ToF32(_distB);
    internal readonly float AngleA() => F16ToF32(_angleA);
    internal readonly float AngleB() => F16ToF32(_angleB);

    internal readonly float AttnMinDistance() => -DistB() / (DistA() - 1);
    internal readonly float AttnMaxDistance() => -DistB() / DistA();

    internal readonly float AttnDist(float dist)
    {
        float rv;
        if (Dattenmode != 0)
            rv = DistB() * dist + DistA();
        else
            rv = DistB() / dist + DistA();
        float upper = rv < 1f ? rv : 1f;
        return 0f < upper ? upper : 0f;
    }

    internal readonly bool IsAttnDist() => DistA() != 1 || DistB() != 0;
    internal readonly bool IsAttnAngle() => AngleA() != 1 || AngleB() != 0;
}
