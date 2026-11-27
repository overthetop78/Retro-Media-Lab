using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using RetroMediaLab.Chips.CPU.Hitachi.SH4;
using RetroMediaLab.Chips.Dreamcast.Holly;
using static RetroMediaLab.Chips.Video.NEC.PowerVr2.PvrRegisters;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal static unsafe partial class PvrMemory
{
    internal static readonly RamRegion Vram = new();

    internal static byte* VramPointer
    {
        get
        {
            fixed (byte* memory = &Vram[0])
                return memory;
        }
    }

    internal static uint VramMask => Sh4DreamcastRuntime.Platform.VramMask;
    internal static uint VramSize => Sh4DreamcastRuntime.Platform.VramSize;

    private static readonly Sh4StoreQueueBuffer[] YuvTempData = new Sh4StoreQueueBuffer[512 / sizeof(Sh4StoreQueueBuffer)];
    private static uint _yuvDest;
    private static uint _yuvBlockCount;
    private static uint _yuvXCurrent;
    private static uint _yuvYCurrent;
    private static uint _yuvXSize;
    private static uint _yuvYSize;
    private static uint _yuvIndex;

    internal static partial void YuvInit()
    {
        _yuvXCurrent = 0;
        _yuvYCurrent = 0;
        _yuvDest = TA_YUV_TEX_BASE & VramMask;
        TA_YUV_TEX_CNT = 0;
        _yuvBlockCount = (TA_YUV_TEX_CTRL.yuv_u_size + 1) * (TA_YUV_TEX_CTRL.yuv_v_size + 1);
        if (TA_YUV_TEX_CTRL.yuv_tex == 1)
            Trace.TraceWarning("YUV: Not supported configuration yuv_tex=1");

        _yuvXSize = (TA_YUV_TEX_CTRL.yuv_u_size + 1) * 16;
        _yuvYSize = (TA_YUV_TEX_CTRL.yuv_v_size + 1) * 16;
        _yuvIndex = 0;
    }

    private static void YuvBlock8X8(byte* inputUv, byte* inputY, byte* output)
    {
        byte* line0 = output;
        byte* line1 = output + _yuvXSize * 2;
        for (int y = 0; y < 8; y += 2)
        {
            for (int x = 0; x < 8; x += 2)
            {
                byte u = inputUv[0];
                byte v = inputUv[64];

                line0[0] = u;
                line0[1] = inputY[0];
                line0[2] = v;
                line0[3] = inputY[1];
                line1[0] = u;
                line1[1] = inputY[8];
                line1[2] = v;
                line1[3] = inputY[9];

                inputUv += 1;
                inputY += 2;
                line0 += 4;
                line1 += 4;
            }

            inputY += 8;
            inputUv += 4;
            line0 += _yuvXSize * 4 - 8 * 2;
            line1 += _yuvXSize * 4 - 8 * 2;
        }
    }

    private static void YuvBlock384(byte* input, byte* output)
    {
        byte* inputUv = input;
        byte* inputY = input + 128;
        byte* destination = output;

        YuvBlock8X8(inputUv, inputY, destination);
        YuvBlock8X8(inputUv + 4, inputY + 64, destination + 8 * 2);
        YuvBlock8X8(inputUv + 32, inputY + 128, destination + _yuvXSize * 8 * 2);
        YuvBlock8X8(inputUv + 36, inputY + 192, destination + _yuvXSize * 8 * 2 + 8 * 2);
    }

    private static void YuvConvertMacroBlock(byte* data)
    {
        TA_YUV_TEX_CNT++;
        YuvBlock384(data, VramPointer + _yuvDest);
        _yuvDest += 32;

        _yuvXCurrent += 16;
        if (_yuvXCurrent == _yuvXSize)
        {
            _yuvDest += 15 * _yuvXSize * 2;
            _yuvXCurrent = 0;
            _yuvYCurrent += 16;
            if (_yuvYCurrent == _yuvYSize)
                _yuvYCurrent = 0;
        }

        if (_yuvBlockCount == TA_YUV_TEX_CNT)
            HollyInterruptController.RaiseInterrupt(HollyInterruptId.YuvDma);
    }

    private static void YuvData(Sh4StoreQueueBuffer* data, uint count)
    {
        if (_yuvBlockCount == 0)
        {
            Trace.TraceWarning("YUV_data: YUV decoder not inited");
            return;
        }

        if (_yuvBlockCount == TA_YUV_TEX_CNT)
            YuvInit();

        uint blockSize = TA_YUV_TEX_CTRL.yuv_form == 0 ? 384u : 512u;
        if (blockSize != 384)
        {
            Trace.TraceWarning("YUV_data: block size 512 not supported");
            return;
        }

        blockSize /= (uint)sizeof(Sh4StoreQueueBuffer);
        fixed (Sh4StoreQueueBuffer* temporary = YuvTempData)
        {
            while (count != 0)
            {
                if (_yuvIndex + count >= blockSize)
                {
                    uint remaining = blockSize - _yuvIndex;
                    if (_yuvIndex == 0)
                    {
                        YuvConvertMacroBlock((byte*)data);
                    }
                    else
                    {
                        NativeMemory.Copy(data, temporary + _yuvIndex, remaining * (uint)sizeof(Sh4StoreQueueBuffer));
                        YuvConvertMacroBlock((byte*)temporary);
                        _yuvIndex = 0;
                    }

                    data += remaining;
                    count -= remaining;
                }
                else
                {
                    NativeMemory.Copy(data, temporary + _yuvIndex, count * (uint)sizeof(Sh4StoreQueueBuffer));
                    _yuvIndex += count;
                    count = 0;
                }
            }
        }
    }

    internal static partial void YuvSerialize(BinaryWriter writer)
    {
        writer.Write(MemoryMarshal.AsBytes(YuvTempData.AsSpan()));
        writer.Write(_yuvDest);
        writer.Write(_yuvBlockCount);
        writer.Write(_yuvXCurrent);
        writer.Write(_yuvYCurrent);
        writer.Write(_yuvXSize);
        writer.Write(_yuvYSize);
        writer.Write(_yuvIndex);
    }

    internal static partial void YuvDeserialize(BinaryReader reader)
    {
        reader.BaseStream.ReadExactly(MemoryMarshal.AsBytes(YuvTempData.AsSpan()));
        _yuvDest = reader.ReadUInt32();
        _yuvBlockCount = reader.ReadUInt32();
        _yuvXCurrent = reader.ReadUInt32();
        _yuvYCurrent = reader.ReadUInt32();
        _yuvXSize = reader.ReadUInt32();
        _yuvYSize = reader.ReadUInt32();
        _yuvIndex = reader.ReadUInt32();
    }

    internal static partial void YuvReset()
    {
        Array.Clear(YuvTempData);
        _yuvDest = 0;
        _yuvBlockCount = 0;
        _yuvXCurrent = 0;
        _yuvYCurrent = 0;
        _yuvXSize = 0;
        _yuvYSize = 0;
        _yuvIndex = 0;
    }

    internal static partial T Read32<T>(uint address) where T : unmanaged =>
        *(T*)(VramPointer + (Map32(address) & ~((uint)sizeof(T) - 1)));

    internal static partial void Write32<T>(uint address, T data, bool internalWrite) where T : unmanaged
    {
        if (!internalWrite && sizeof(T) == 1)
        {
            Trace.TraceInformation($"{address:x8}: 8-bit VRAM writes are not possible");
            return;
        }

        address &= ~((uint)sizeof(T) - 1);
        uint vramAddress = address & VramMask;
        if (vramAddress >= PvrRenderer.FramebufferWatchStart && vramAddress < PvrRenderer.FramebufferWatchEnd)
            PvrRenderer.FramebufferDirty = true;

        *(T*)(VramPointer + Map32(address)) = data;
    }

    internal static partial void TaWrite(uint address, Sh4StoreQueueBuffer* data, uint count)
    {
        if ((address & 0x800000) == 0)
            PowerVr2Ta.VertexData(data, count);
        else
            YuvData(data, count);
    }

    internal static partial void TaWriteSq(uint address, Sh4StoreQueueBuffer* storeQueues)
    {
        uint writeAddress = address & 0x01FFFFE0;
        Sh4StoreQueueBuffer* queue = &storeQueues[(address >> 5) & 1];
        if (writeAddress < 0x800000)
        {
            PowerVr2Ta.VertexData32(queue);
        }
        else if (writeAddress < 0x1000000)
        {
            YuvData(queue, 1);
        }
        else
        {
            Debug.WriteLine($"Vram TAWriteSQ 0x{address:X} SB_LMMODE0 {HollySystemBus.SB_LMMODE0}");
            bool path64Bit = ((address & 0x02000000) != 0
                ? HollySystemBus.SB_LMMODE1 : HollySystemBus.SB_LMMODE0) == 0;
            if (path64Bit)
            {
                var destination = (Sh4StoreQueueBuffer*)(VramPointer + (writeAddress & VramMask));
                *destination = *queue;
            }
            else
            {
                for (uint i = 0; i < sizeof(Sh4StoreQueueBuffer); i += 4)
                    Write32(writeAddress + i, *(uint*)((byte*)queue + i));
            }
        }
    }

    private const uint VramBankBit = 0x400000;

    private static uint Map32(uint offset32)
    {
        uint staticBits = VramMask - (VramBankBit * 2 - 1) + 3;
        const uint offsetBits = (VramBankBit - 1) & ~3u;
        uint bank = (offset32 & VramBankBit) / VramBankBit;
        uint result = offset32 & staticBits;
        result |= (offset32 & offsetBits) * 2;
        result |= bank * 4;
        return result;
    }

    internal static partial T ReadArea4<T>(uint address, bool upper) where T : unmanaged
    {
        bool access32 = (upper ? HollySystemBus.SB_LMMODE1 : HollySystemBus.SB_LMMODE0) == 1;
        if (access32)
            return Read32<T>(address);
        else
            return *(T*)(VramPointer + (address & VramMask));
    }

    internal static partial void WriteArea4<T>(uint address, T data, bool upper) where T : unmanaged
    {
        bool access32 = (upper ? HollySystemBus.SB_LMMODE1 : HollySystemBus.SB_LMMODE0) == 1;
        if (access32)
            Write32(address, data);
        else
            *(T*)(VramPointer + (address & VramMask)) = data;
    }
}
