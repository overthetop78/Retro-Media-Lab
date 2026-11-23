using RetroMediaLab.App.Constants.Rendering.Emulation;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace RetroMediaLab.App.Rendering.Emulation.Processing.OpenGl;

internal sealed partial class OpenGlNativeApi
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void ProgramParameteri(uint program, uint name, int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void GetProgramBinary(uint program, int capacity, out int length, out uint format, [Out] byte[] binary);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void LoadProgramBinary(uint program, uint format, byte[] binary, int length);

    private sealed class ProgramBinaryCache(string path, ProgramParameteri setParameter,
        GetProgramBinary getBinary, LoadProgramBinary loadBinary)
    {
        private const uint NumberOfBinaryFormats = 0x87FE;
        private const uint RetrievableHint = 0x8257;
        private const uint BinaryLength = 0x8741;

        internal static ProgramBinaryCache? ForCurrentDriver(string vertexSource, string fragmentSource)
        {
            var setParameter = Find<ProgramParameteri>("glProgramParameteri");
            var getBinary = Find<GetProgramBinary>("glGetProgramBinary");
            var loadBinary = Find<LoadProgramBinary>("glProgramBinary");
            if (setParameter is null || getBinary is null || loadBinary is null) return null;
            GetInteger(NumberOfBinaryFormats, out var formatCount);
            if (formatCount <= 0) return null;

            var vendor = Marshal.PtrToStringAnsi(GetString(0x1F00)) ?? "";
            var renderer = Marshal.PtrToStringAnsi(GetString(0x1F01)) ?? "";
            var version = Marshal.PtrToStringAnsi(GetString(0x1F02)) ?? "";
            var path = VideoShaderDiskCache.Entry("OpenGL", "program-v1", vendor, renderer, version, vertexSource, fragmentSource);
            return new ProgramBinaryCache(path, setParameter, getBinary, loadBinary);
        }

        internal bool Restore(uint program, GetProgramivDelegate getProgram)
        {
            var payload = VideoShaderDiskCache.Read(path);
            if (payload is not { Length: > sizeof(uint) }) return false;
            var format = BinaryPrimitives.ReadUInt32LittleEndian(payload);
            loadBinary(program, format, payload[sizeof(uint)..], payload.Length - sizeof(uint));
            getProgram(program, OpenGlVideoConstants.LinkStatus, out var linked);
            if (linked == 0) _ = GetError();
            return linked != 0;
        }

        internal void MarkRetrievable(uint program) => setParameter(program, RetrievableHint, 1);

        internal void Save(uint program, GetProgramivDelegate getProgram)
        {
            getProgram(program, BinaryLength, out var capacity);
            if (capacity is <= 0 or > VideoShaderDiskCache.MaximumBinarySize - sizeof(uint)) return;
            var binary = new byte[capacity];
            getBinary(program, capacity, out var length, out var format, binary);
            if (length <= 0 || length > capacity) return;
            var payload = new byte[sizeof(uint) + length];
            BinaryPrimitives.WriteUInt32LittleEndian(payload, format);
            binary.AsSpan(0, length).CopyTo(payload.AsSpan(sizeof(uint)));
            VideoShaderDiskCache.Write(path, payload);
        }

        [DllImport(OpenGlNativeFunctionNames.Library, EntryPoint = "glGetIntegerv")]
        private static extern void GetInteger(uint name, out int value);
        [DllImport(OpenGlNativeFunctionNames.Library, EntryPoint = "glGetString")]
        private static extern IntPtr GetString(uint name);
        [DllImport(OpenGlNativeFunctionNames.Library, EntryPoint = "glGetError")]
        private static extern uint GetError();
    }
}
