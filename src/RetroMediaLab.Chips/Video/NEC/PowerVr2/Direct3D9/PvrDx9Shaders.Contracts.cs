using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Direct3D9;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2.Direct3D9;

internal sealed unsafe partial class PvrDx9Shaders
{
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeMacro
    {
        internal nint Name;
        internal nint Definition;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate int CompileShaderEntry(nint source, uint sourceLength, NativeMacro* defines, nint include,
        nint function, nint profile, uint flags, out nint shader, out nint errors, out nint constants);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint ShaderProfileEntry(nint device);

    private IDirect3DDevice9? _device;
    private readonly Dictionary<uint, IDirect3DPixelShader9?> _shaders = [];
    private readonly IDirect3DVertexShader9?[] _vertexShaders = new IDirect3DVertexShader9?[4];
    private readonly IDirect3DPixelShader9?[] _modVolShaders = new IDirect3DPixelShader9?[2];
    private readonly PvrWindowsLibrary _d3dx9Library = new();
    private CompileShaderEntry? _compileShader;
    private ShaderProfileEntry? _getVertexShaderProfile;
    private ShaderProfileEntry? _getPixelShaderProfile;
}
