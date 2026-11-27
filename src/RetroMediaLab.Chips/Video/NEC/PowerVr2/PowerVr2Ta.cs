using System.Diagnostics;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PowerVr2Ta
{
    private enum TaState : byte
    {
        NoList,
        PolygonListVertex32,
        PolygonListVertex64,
        ModifierVolumeListVertex64,
        PolygonListHeader64Vertex32,
        PolygonListHeader64Vertex64,
        PolygonListVertex64SecondHalf,
        ModifierVolumeListVertex64SecondHalf
    }

    private static ref byte TaCurrentState => ref TaFsm[2048];

    internal static readonly byte[] TaFsm = FillFsm();
    internal static uint TaFsmCurrentList = 7;
    internal static uint TaRenderPass;

    private static void FillFsm(byte[] stateMachine, TaState state, int parameterType, int objectType,
        TaState next, uint process = 0, uint size64 = 0)
    {
        for (int i = 0; i < 8; i++)
        {
            if (parameterType != -1)
                i = parameterType;

            for (int j = 0; j < 32; j++)
            {
                if (objectType != -1)
                    j = objectType;
                int index = ((int)state << 8) + (i << 5) + j;
                Sh4HostRuntime.Verify(stateMachine[index] == 0x80 + (int)state);
                stateMachine[index] = (byte)((uint)next | process * 16);
                if (objectType != -1)
                    break;
            }

            if (parameterType != -1)
                break;
        }
    }

    private static byte[] FillFsm()
    {
        var stateMachine = new byte[2049];
        for (int i = 0; i < 2048; i++)
            stateMachine[i] = (byte)((i >> 8) | 0x80);

        for (uint i = 0; i < 8; i++)
        {
            switch (i)
            {
                case ParamType_End_Of_List:
                    FillFsm(stateMachine, TaState.NoList, (int)i, -1, TaState.NoList, 1);
                    FillFsm(stateMachine, TaState.PolygonListVertex32, (int)i, -1, TaState.NoList, 1);
                    FillFsm(stateMachine, TaState.PolygonListVertex64, (int)i, -1, TaState.NoList, 1);
                    FillFsm(stateMachine, TaState.ModifierVolumeListVertex64, (int)i, -1, TaState.NoList, 1);
                    break;

                case ParamType_User_Tile_Clip:
                case ParamType_Object_List_Set:
                    FillFsm(stateMachine, TaState.NoList, (int)i, -1, TaState.NoList);
                    FillFsm(stateMachine, TaState.PolygonListVertex32, (int)i, -1, TaState.PolygonListVertex32);
                    FillFsm(stateMachine, TaState.PolygonListVertex64, (int)i, -1, TaState.PolygonListVertex64);
                    FillFsm(stateMachine, TaState.ModifierVolumeListVertex64, (int)i, -1, TaState.ModifierVolumeListVertex64);
                    break;

                case 3:
                case 6:
                    break;

                case ParamType_Polygon_or_Modifier_Volume:
                    for (int k = 0; k < 32; k++)
                    {
                        uint uid = TaTypeLut.Instance.Table[k * 4];
                        uint vertexType = uid & 0x7f;
                        bool vertex64 = vertexType is 5 or 6 or 11 or 12 or 13 or 14;
                        bool parameter64 = (uid >> 31) != 0;

                        TaState next = parameter64
                            ? (vertex64 ? TaState.PolygonListHeader64Vertex64 : TaState.PolygonListHeader64Vertex32)
                            : (vertex64 ? TaState.PolygonListVertex64 : TaState.PolygonListVertex32);

                        FillFsm(stateMachine, TaState.PolygonListVertex32, (int)i, k, next, 0, parameter64 ? 1u : 0u);
                        FillFsm(stateMachine, TaState.PolygonListVertex64, (int)i, k, next, 0, parameter64 ? 1u : 0u);
                    }
                    FillFsm(stateMachine, TaState.ModifierVolumeListVertex64, (int)i, -1, TaState.ModifierVolumeListVertex64);
                    FillFsm(stateMachine, TaState.NoList, (int)i, -1, TaState.NoList, 1);
                    break;

                case ParamType_Sprite:
                    FillFsm(stateMachine, TaState.PolygonListVertex32, (int)i, -1, TaState.PolygonListVertex64);
                    FillFsm(stateMachine, TaState.PolygonListVertex64, (int)i, -1, TaState.PolygonListVertex64);
                    FillFsm(stateMachine, TaState.NoList, (int)i, -1, TaState.NoList, 1);
                    break;

                case ParamType_Vertex_Parameter:
                    FillFsm(stateMachine, TaState.PolygonListVertex32, (int)i, -1, TaState.PolygonListVertex32, 0, 0);
                    FillFsm(stateMachine, TaState.PolygonListVertex64, (int)i, -1, TaState.PolygonListVertex64SecondHalf, 0, 1);
                    FillFsm(stateMachine, TaState.ModifierVolumeListVertex64, (int)i, -1, TaState.ModifierVolumeListVertex64SecondHalf, 0, 1);
                    break;
            }
        }

        FillFsm(stateMachine, TaState.PolygonListHeader64Vertex32, -1, -1, TaState.PolygonListVertex32);
        FillFsm(stateMachine, TaState.PolygonListHeader64Vertex64, -1, -1, TaState.PolygonListVertex64);
        FillFsm(stateMachine, TaState.PolygonListVertex64SecondHalf, -1, -1, TaState.PolygonListVertex64);
        FillFsm(stateMachine, TaState.ModifierVolumeListVertex64SecondHalf, -1, -1, TaState.ModifierVolumeListVertex64);
        return stateMachine;
    }

    private static readonly HollyInterruptId[] ListEndInterrupt =
    [
        HollyInterruptId.Opaque,
        HollyInterruptId.OpaqueMod,
        HollyInterruptId.Trans,
        HollyInterruptId.TransMod,
        HollyInterruptId.PunchThru
    ];

    private static void HandleCommand(uint transition)
    {
        var data = (Ta_Dma*)(CurrentData.ThdData - 32);
        uint command = transition >> 4;
        transition &= 7;
        if (command != 8)
        {
            if (data->pcw.ParaType == ParamType_End_Of_List)
            {
                if (TaFsmCurrentList == 7)
                    TaFsmCurrentList = data->pcw.ListType;

                if (Sh4DreamcastRuntime.Platform.IsNaomi2)
                    HollyInterruptController.RaiseInterruptBothClx(ListEndInterrupt[TaFsmCurrentList]);
                else
                    HollyInterruptController.RaiseInterrupt(ListEndInterrupt[TaFsmCurrentList]);
                TaFsmCurrentList = 7;
                transition = (uint)TaState.NoList;
            }
            else if (data->pcw.ParaType == ParamType_Polygon_or_Modifier_Volume)
            {
                if (TaFsmCurrentList == 7)
                    TaFsmCurrentList = data->pcw.ListType;

                if (!IsModVolList(TaFsmCurrentList))
                    transition = (uint)TaState.PolygonListVertex32;
                else
                    transition = (uint)TaState.ModifierVolumeListVertex64;
            }
            else if (data->pcw.ParaType == ParamType_Sprite)
            {
                if (TaFsmCurrentList == 7)
                    TaFsmCurrentList = data->pcw.ListType;

                transition = (uint)TaState.PolygonListVertex32;
            }
            else
            {
                Sh4HostRuntime.Die("WTF ?\n");
            }
        }

        uint input = (transition << 8) | (data->pcw.ParaType << 5) | ((uint)data->pcw.obj_ctrl >> 2) % 32;
        TaCurrentState = (byte)(TaFsm[input] & 0xf);
        Sh4HostRuntime.Verify(TaCurrentState <= 7);
    }

    private static uint OpbSize(uint value) => value == 0 ? 0 : 16u << (int)value;

    private static void MarkObjectListBlocks(uint renderPass)
    {
        GetRegionTileAddressAndSize(out uint address, out uint tileSize);
        address += tileSize * renderPass;
        uint opbAddress = PvrMemory.Read32<uint>(address + 4);
        bool emptyOpaqueList = (opbAddress & 0x80000000) != 0;

        address = PvrRegisters.TA_OL_BASE;
        uint tileCount = (PvrRegisters.TA_GLOB_TILE_CLIP.tile_y_num + 1) * (PvrRegisters.TA_GLOB_TILE_CLIP.tile_x_num + 1);
        uint opBlockSize = OpbSize(PvrRegisters.TA_ALLOC_CTRL & 3);
        if (emptyOpaqueList)
            address += opBlockSize * tileCount;
        if (opBlockSize == 0 || emptyOpaqueList)
        {
            address += OpbSize((PvrRegisters.TA_ALLOC_CTRL >> 4) & 3) * tileCount;
            opBlockSize = OpbSize((PvrRegisters.TA_ALLOC_CTRL >> 8) & 3);
            if (opBlockSize == 0)
            {
                address += OpbSize((PvrRegisters.TA_ALLOC_CTRL >> 12) & 3) * tileCount;
                opBlockSize = OpbSize((PvrRegisters.TA_ALLOC_CTRL >> 16) & 3);
                if (opBlockSize == 0)
                {
                    Trace.TraceInformation("markObjectListBlocks: all lists are empty");
                    return;
                }
            }
        }

        for (uint y = 0; y <= PvrRegisters.TA_GLOB_TILE_CLIP.tile_y_num; y++)
        {
            for (uint x = 0; x <= PvrRegisters.TA_GLOB_TILE_CLIP.tile_x_num; x++)
            {
                PvrMemory.Write32(address, PvrRegisters.TA_OL_BASE);
                address += opBlockSize;
            }
        }
    }

    internal static void VertexListInit(bool continuation)
    {
        if (!continuation)
            TaRenderPass = 0;
        else
            TaRenderPass++;
        SetCurrentTarc(PvrRegisters.TA_OL_BASE);
        CurrentData.ClearPartial();
        MarkObjectListBlocks(TaRenderPass);

        TaCurrentState = (byte)TaState.NoList;
        TaFsmCurrentList = 7;
        if (Sh4DreamcastRuntime.Platform.IsNaomi2)
            ParseReset();
    }

    internal static void VertexSoftReset() => TaCurrentState = (byte)TaState.NoList;

    private static void ThreadData32(Sh4StoreQueueBuffer* data)
    {
        if (CurrentContext is null)
        {
            Trace.TraceInformation("Warning: data sent to TA prior to ListInit. Ignored");
            return;
        }
        if (CurrentData.End() - CurrentData.ThdRoot >= TaDataSize)
        {
            Trace.TraceInformation("Warning: TA data buffer overflow");
            HollyInterruptController.RaiseInterrupt(HollyInterruptId.MatrNoMem);
            return;
        }

        var destination = (Sh4StoreQueueBuffer*)CurrentData.ThdData;
        PCW control = *(PCW*)data;
        *destination = *data;
        CurrentData.ThdData += 32;

        uint input = ((uint)TaCurrentState << 8) | (control.ParaType << 5) | (((uint)control.obj_ctrl >> 2) & 31);
        uint transition = TaFsm[input];
        TaCurrentState = (byte)transition;
        bool mustHandle = (transition & 0xf0) != 0;
        if (!mustHandle)
            return;
        else
            HandleCommand(transition);
    }

    internal static void VertexData32(Sh4StoreQueueBuffer* data) => ThreadData32(data);

    internal static void VertexData(Sh4StoreQueueBuffer* data, uint count)
    {
        while (count >= 4)
        {
            ThreadData32(data);
            data++;
            ThreadData32(data);
            data++;
            ThreadData32(data);
            data++;
            ThreadData32(data);
            data++;
            count -= 4;
        }
        while (count > 0)
        {
            ThreadData32(data);
            data++;
            count--;
        }
    }
}

internal sealed partial class TaTypeLut
{
    private static uint PolygonDataTypeId(PCW control)
    {
        if (control.Texture != 0)
        {
            if (control.Volume == 0)
            {
                if (control.Col_Type == 0)
                {
                    if (control.UV_16bit == 0)
                        return 3;
                    else
                        return 4;
                }
                else if (control.Col_Type == 1)
                {
                    if (control.UV_16bit == 0)
                        return 5;
                    else
                        return 6;
                }
                else
                {
                    if (control.UV_16bit == 0)
                        return 7;
                    else
                        return 8;
                }
            }
            else
            {
                if (control.Col_Type == 0)
                {
                    if (control.UV_16bit == 0)
                        return 11;
                    else
                        return 12;
                }
                else if (control.Col_Type == 1)
                {
                    return InvalidType;
                }
                else
                {
                    if (control.UV_16bit == 0)
                        return 13;
                    else
                        return 14;
                }
            }
        }
        else
        {
            if (control.Volume == 0)
            {
                if (control.Col_Type == 0)
                    return 0;
                else if (control.Col_Type == 1)
                    return 1;
                else
                    return 2;
            }
            else
            {
                if (control.Col_Type == 0)
                    return 9;
                else if (control.Col_Type == 1)
                    return InvalidType;
                else
                    return 10;
            }
        }
    }

    private static uint PolygonHeaderTypeSize(PCW control)
    {
        if (control.Volume == 0)
        {
            if (control.Col_Type < 2)
            {
                return 0 | 0;
            }
            else if (control.Col_Type == 2)
            {
                if (control.Texture != 0)
                {
                    if (control.Offset != 0)
                        return 2 | 0x80;
                    else
                        return 1 | 0;
                }
                else
                {
                    return 1 | 0;
                }
            }
            else
            {
                return 0 | 0;
            }
        }
        else
        {
            if (control.Col_Type == 0)
                return 3 | 0;
            else if (control.Col_Type == 2)
                return 4 | 0x80;
            else if (control.Col_Type == 3)
                return 3 | 0;
            else
                return InvalidType;
        }
    }

    private TaTypeLut()
    {
        for (int i = 0; i < 256; i++)
        {
            var control = new PCW { obj_ctrl = (byte)i };
            uint value = PolygonDataTypeId(control);
            uint type = PolygonHeaderTypeSize(control);

            if ((type & 0x80) != 0)
                value |= PowerVr2Ta.SZ64 << 30;
            else
                value |= PowerVr2Ta.SZ32 << 30;

            value |= (type & 0x7f) << 8;
            Table[i] = value;
        }
    }
}
