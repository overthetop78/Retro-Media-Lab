using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe class PvrDx11OitBuffers
{
    internal void Init(ID3D11Device device, ID3D11DeviceContext deviceContext)
    {
        _device = device;
        _deviceContext = deviceContext;
        _pixelsBufferView?.Dispose();
        _pixelsBufferView = null;
        _pixelsBuffer?.Dispose();
        _pixelsBuffer = null;
        BufferDescription description = new()
        {
            ByteWidth = (uint)Math.Min(PvrConfiguration.PixelBufferSize, uint.MaxValue),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.UnorderedAccess,
            MiscFlags = ResourceOptionFlags.BufferStructured,
            StructureByteStride = 16
        };

        var result = device.CreateBuffer(description, null, out _pixelsBuffer);
        if (result.Failure)
        {
            const uint resourceSizeLimit = 128u * 1024u * 1024u;
            if (description.ByteWidth > resourceSizeLimit)
            {
                description.ByteWidth = resourceSizeLimit;
                result = device.CreateBuffer(description, null, out _pixelsBuffer);
            }
            if (result.Failure)
            {
                PvrRuntime.Warn($"Pixels buffer creation failed: {result.Code:x}");
                return;
            }
        }

        UnorderedAccessViewDescription viewDescription = new()
        {
            Format = Format.Unknown,
            ViewDimension = UnorderedAccessViewDimension.Buffer,
            Buffer = new BufferUnorderedAccessView
            {
                FirstElement = 0,
                Flags = BufferUnorderedAccessViewFlags.Counter,
                NumElements = description.ByteWidth / description.StructureByteStride
            }
        };

        result = device.CreateUnorderedAccessView(_pixelsBuffer!, viewDescription, out _pixelsBufferView);
        if (result.Failure)
            PvrRuntime.Warn($"Pixels buffer UAV creation failed: {result.Code:x}");
    }

    internal void Resize(int width, int height)
    {
        if (_width >= width && _height >= height)
            return;

        _width = Math.Max(_width, width);
        _height = Math.Max(_height, height);
        _abufferPointersView?.Dispose();
        _abufferPointersView = null;
        _abufferPointersTexture?.Dispose();
        _abufferPointersTexture = null;
        Texture2DDescription textureDescription = new()
        {
            Width = (uint)_width,
            Height = (uint)_height,
            ArraySize = 1,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
            Format = Format.R32_UInt,
            MipLevels = 1
        };

        uint pitch = (uint)(_width * sizeof(int));
        nuint length = (nuint)pitch * (uint)_height;
        void* initialData = NativeMemory.Alloc(length);
        try
        {
            NativeMemory.Fill(initialData, length, 0xff);
            SubresourceData data = new((nint)initialData, pitch, 0);
            var result = _device!.CreateTexture2D(textureDescription, new ReadOnlySpan<SubresourceData>(&data, 1), out _abufferPointersTexture);
            if (result.Failure)
            {
                PvrRuntime.Warn($"A-buffer texture creation failed: {result.Code:x}");
                return;
            }
        }
        finally
        {
            NativeMemory.Free(initialData);
        }

        UnorderedAccessViewDescription viewDescription = new()
        {
            Format = Format.Unknown,
            ViewDimension = UnorderedAccessViewDimension.Texture2D
        };
        var viewResult = _device.CreateUnorderedAccessView(_abufferPointersTexture!, viewDescription, out _abufferPointersView);
        if (viewResult.Failure)
            PvrRuntime.Warn($"A-buffer texture UAV creation failed: {viewResult.Code:x}");
    }

    internal void Bind()
    {
        ID3D11UnorderedAccessView?[] views = [_pixelsBufferView, _abufferPointersView];
        uint[] initialCounts = [0, uint.MaxValue];
        _deviceContext!.OMSetRenderTargetsAndUnorderedAccessViews(
            uint.MaxValue, null, null, 1, (uint)views.Length, views, initialCounts);
    }

    internal void Term()
    {
        _width = 0;
        _height = 0;
        try
        {
            _abufferPointersView?.Dispose();
        }
        finally
        {
            try
            {
                _abufferPointersTexture?.Dispose();
            }
            finally
            {
                try
                {
                    _pixelsBufferView?.Dispose();
                }
                finally
                {
                    try
                    {
                        _pixelsBuffer?.Dispose();
                    }
                    finally
                    {
                        _abufferPointersView = null;
                        _abufferPointersTexture = null;
                        _pixelsBufferView = null;
                        _pixelsBuffer = null;
                        _deviceContext = null;
                        _device = null;
                    }
                }
            }
        }
    }

    private int _width;
    private int _height;
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _deviceContext;
    private ID3D11Texture2D? _abufferPointersTexture;
    private ID3D11UnorderedAccessView? _abufferPointersView;
    private ID3D11Buffer? _pixelsBuffer;
    private ID3D11UnorderedAccessView? _pixelsBufferView;
}
