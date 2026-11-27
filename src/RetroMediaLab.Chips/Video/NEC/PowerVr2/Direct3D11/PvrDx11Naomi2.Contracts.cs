using System;
using Vortice.Direct3D11;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal unsafe sealed partial class PvrDx11Naomi2Helper
{
    internal void Term()
    {
        try
        {
            _polyConstantsBuffer?.Dispose();
        }
        finally
        {
            try
            {
                _lightConstantsBuffer?.Dispose();
            }
            finally
            {
                _polyConstantsBuffer = null;
                _lightConstantsBuffer = null;
                _deviceContext = null;
            }
        }
    }

    internal void ResetCache()
    {
        _lastModel = -1;
    }

    private void SetConstBuffer<T>(ID3D11Buffer buffer, in T data) where T : unmanaged
    {
        ID3D11DeviceContext context = _deviceContext!;
        MappedSubresource mapped = context.Map(buffer, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            fixed (T* source = &data)
                Buffer.MemoryCopy(source, (void*)mapped.DataPointer, sizeof(T), sizeof(T));
        }
        finally
        {
            context.Unmap(buffer, 0);
        }
    }

    private ID3D11DeviceContext? _deviceContext;
    private ID3D11Buffer? _polyConstantsBuffer;
    private ID3D11Buffer? _lightConstantsBuffer;
    private int _lastModel;
}
