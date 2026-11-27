using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D11;

internal sealed unsafe class PvrDx11Quad : IDisposable
{
    private struct Vertex(float x, float y, float u, float v)
    {
        internal float X = x;
        internal float Y = y;
        internal float U = u;
        internal float V = v;
    }

    private PvrDx11Shaders _shaders = null!;
    private ID3D11Device _device = null!;
    private ID3D11DeviceContext _deviceContext = null!;
    private ID3D11InputLayout? _inputLayout;
    private ID3D11Buffer? _vertexBuffer;
    private ID3D11Buffer? _constantBuffer;
    private ID3D11RasterizerState? _rasterizerState;
    private ID3D11DepthStencilState? _depthStencilState;
    private ID3D11VertexShader _vertexShader = null!;
    private ID3D11VertexShader _rotateVertexShader = null!;
    private ID3D11PixelShader _pixelShader = null!;

    internal void Init(ID3D11Device device, ID3D11DeviceContext deviceContext, PvrDx11Shaders shaders)
    {
        _device = device;
        _deviceContext = deviceContext;
        _shaders = shaders;
        _vertexShader = shaders.GetQuadVertexShader(false);
        _rotateVertexShader = shaders.GetQuadVertexShader(true);
        _pixelShader = shaders.GetQuadPixelShader();

        InputElementDescription[] layout =
        [
            new("POSITION", 0, Format.R32G32_Float, 0, 0, InputClassification.PerVertexData, 0),
            new("TEXCOORD", 0, Format.R32G32_Float, 2 * sizeof(float), 0, InputClassification.PerVertexData, 0)
        ];
        using var blob = shaders.GetQuadVertexShaderBlob();
        if (device.CreateInputLayout(layout, new ReadOnlySpan<byte>((void*)blob.BufferPointer, (int)blob.BufferSize), out _inputLayout).Failure)
            Trace.TraceWarning("Input layout creation failed");

        RasterizerDescription rasterizer = default;
        rasterizer.FillMode = FillMode.Solid;
        rasterizer.CullMode = CullMode.None;
        rasterizer.ScissorEnable = false;
        rasterizer.DepthClipEnable = true;
        _rasterizerState = device.CreateRasterizerState(rasterizer);

        DepthStencilDescription depth = default;
        depth.DepthEnable = false;
        depth.DepthWriteMask = DepthWriteMask.All;
        depth.DepthFunc = ComparisonFunction.Always;
        depth.StencilEnable = false;
        depth.FrontFace.StencilFailOp = depth.FrontFace.StencilDepthFailOp =
            depth.FrontFace.StencilPassOp = StencilOperation.Keep;
        depth.FrontFace.StencilFunc = ComparisonFunction.Always;
        depth.BackFace = depth.FrontFace;
        _depthStencilState = device.CreateDepthStencilState(depth);

        BufferDescription vertices = default;
        vertices.ByteWidth = sizeof(float) * 4 * 4;
        vertices.Usage = ResourceUsage.Dynamic;
        vertices.BindFlags = BindFlags.VertexBuffer;
        vertices.CPUAccessFlags = CpuAccessFlags.Write;
        _vertexBuffer = device.CreateBuffer(vertices);

        BufferDescription constants = default;
        constants.ByteWidth = sizeof(float) * 4;
        constants.Usage = ResourceUsage.Dynamic;
        constants.BindFlags = BindFlags.ConstantBuffer;
        constants.CPUAccessFlags = CpuAccessFlags.Write;
        _constantBuffer = device.CreateBuffer(constants);
    }

    internal void Draw(ID3D11ShaderResourceView view, ID3D11SamplerState sampler, float* color = null,
        float x = -1f, float y = -1f, float width = 2f, float height = 2f, bool rotate = false)
    {
        Vertex* vertices = stackalloc Vertex[4]
        {
            new(x, y, 0f, 1f),
            new(x, y + height, 0f, 0f),
            new(x + width, y, 1f, 1f),
            new(x + width, y + height, 1f, 0f)
        };
        MappedSubresource mapped = _deviceContext.Map(_vertexBuffer!, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            Buffer.MemoryCopy(vertices, (void*)mapped.DataPointer, sizeof(Vertex) * 4, sizeof(Vertex) * 4);
        }
        finally
        {
            _deviceContext.Unmap(_vertexBuffer!, 0);
        }

        _deviceContext.IASetInputLayout(_inputLayout);
        _deviceContext.IASetVertexBuffer(0, _vertexBuffer, (uint)sizeof(Vertex), 0);

        _deviceContext.OMSetDepthStencilState(_depthStencilState, 0);
        _deviceContext.RSSetState(_rasterizerState);
        _deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        _deviceContext.VSSetShader(rotate ? _rotateVertexShader : _vertexShader);
        _deviceContext.PSSetShader(_pixelShader);
        _deviceContext.GSSetShader(null);
        _deviceContext.HSSetShader(null);
        _deviceContext.DSSetShader(null);
        _deviceContext.CSSetShader(null);

        float* white = stackalloc float[4] { 1f, 1f, 1f, 1f };
        if (color == null)
            color = white;
        mapped = _deviceContext.Map(_constantBuffer!, 0, MapMode.WriteDiscard, MapFlags.None);
        try
        {
            Buffer.MemoryCopy(color, (void*)mapped.DataPointer, sizeof(float) * 4, sizeof(float) * 4);
        }
        finally
        {
            _deviceContext.Unmap(_constantBuffer!, 0);
        }

        _deviceContext.PSSetConstantBuffer(0, _constantBuffer);
        _deviceContext.PSSetShaderResource(0, view);
        _deviceContext.PSSetSampler(0, sampler);
        _deviceContext.Draw(4, 0);
    }

    public void Dispose()
    {
        try
        {
            _depthStencilState?.Dispose();
            _rasterizerState?.Dispose();
            _constantBuffer?.Dispose();
            _vertexBuffer?.Dispose();
            _inputLayout?.Dispose();
        }
        finally
        {
            _depthStencilState = null;
            _rasterizerState = null;
            _constantBuffer = null;
            _vertexBuffer = null;
            _inputLayout = null;
        }
    }
}
