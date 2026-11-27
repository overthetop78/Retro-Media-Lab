using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Overlay
{
    internal void Init(IDirect3DDevice9 device)
    {
        device.AddRef();
        _device = new IDirect3DDevice9(device.NativePointer);
        Array.Clear(_vmuLastChanged);
    }

    internal void Term()
    {
        try
        {
            _device?.Dispose();
            _device = null;
        }
        finally
        {
            try
            {
                _xhairTexture?.Dispose();
                _xhairTexture = null;
            }
            finally
            {
                for (int i = 0; i < _vmuTextures.Length; i++)
                {
                    _vmuTextures[i]?.Dispose();
                    _vmuTextures[i] = null;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Vertex(float X, float Y, float Z, float U, float V);

    private IDirect3DDevice9? _device;
    private IDirect3DTexture9? _xhairTexture;
    private readonly IDirect3DTexture9?[] _vmuTextures = new IDirect3DTexture9?[8];
    private readonly ulong[] _vmuLastChanged = new ulong[8];
}
