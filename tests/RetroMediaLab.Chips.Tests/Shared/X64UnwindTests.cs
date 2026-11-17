using System.Runtime.InteropServices;
using RetroMediaLab.Chips.Common.Compilation.X64;
using RetroMediaLab.Chips.CPU.Hitachi.SH2.Compilation;

namespace RetroMediaLab.Chips.Tests.Shared;

[CollectionDefinition("Windows native function tables", DisableParallelization = true)]
public sealed class NativeFunctionTableCollection;

[Collection("Windows native function tables")]
public sealed unsafe class X64UnwindTests
{
    private static readonly byte[] Code = [
        0x53, 0x56, 0x57, 0x48, 0x83, 0xec, 0x30,
        0x48, 0x89, 0xce, 0x48, 0x89, 0xd7, 0x4c, 0x89, 0xc3,
        0x90, 0x90, 0x90, 0x90,
        0x48, 0x83, 0xc4, 0x30, 0x5f, 0x5e, 0x5b, 0xc3];
    private static readonly byte[] Unwind = [1, 16, 4, 0, 7, 0x52, 3, 0x70, 2, 0x60, 1, 0x30];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    [InlineData(10)]
    [InlineData(13)]
    [InlineData(16)]
    [InlineData(21)]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(25)]
    [InlineData(26)]
    [InlineData(27)]
    public void WindowsRecoversCallerFromPrologueBodyAndEpilogue(int offset)
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        Sh2X64Block? block = null; byte* context = null; byte* stack = null;
        try
        {
            block = Sh2X64Block.TryCompile([0xe123])!;
            context = (byte*)NativeMemory.AlignedAlloc(1232, 16);
            stack = (byte*)NativeMemory.AlignedAlloc(1024, 16);
            if (context == null || stack == null) throw new OutOfMemoryException();
            new Span<byte>(context, 1232).Clear(); new Span<byte>(stack, 1024).Clear();
            ulong entrySp = (ulong)(stack + 520);
            const ulong caller = 0x12345678, rbx = 0x1111222233334444, rsi = 0x5555666677778888, rdi = 0x9999aaaabbbbcccc;
            *(ulong*)entrySp = caller;
            *(ulong*)(entrySp - 8) = rbx; *(ulong*)(entrySp - 16) = rsi; *(ulong*)(entrySp - 24) = rdi;
            ulong sp = offset < 1 ? entrySp : offset < 2 ? entrySp - 8 : offset < 3 ? entrySp - 16
                : offset < 7 ? entrySp - 24 : offset < 24 ? entrySp - 72 : offset < 25 ? entrySp - 24
                : offset < 26 ? entrySp - 16 : offset < 27 ? entrySp - 8 : entrySp;
            // Windows AMD64 CONTEXT layout: flags at 48, integer registers at 120..248.
            *(uint*)(context + 48) = 0x100003;
            *(ulong*)(context + 144) = offset >= 16 && offset < 27 ? 1UL : rbx;
            *(ulong*)(context + 168) = offset >= 10 && offset < 26 ? 2UL : rsi;
            *(ulong*)(context + 176) = offset >= 13 && offset < 25 ? 3UL : rdi;
            *(ulong*)(context + 152) = sp;
            int actualOffset = offset switch
            {
                20 => block.Code.Length - 8, 24 => block.Code.Length - 4,
                25 => block.Code.Length - 3, 26 => block.Code.Length - 2,
                27 => block.Code.Length - 1, _ => offset
            };
            ulong pc = unchecked((ulong)block.EntryPointer) + (uint)actualOffset;
            *(ulong*)(context + 248) = pc;
            nint function = RtlLookupFunctionEntry(pc, out ulong imageBase, 0);
            Assert.NotEqual(0, function); Assert.Equal(unchecked((ulong)block.EntryPointer), imageBase);
            Assert.Equal((uint)block.Code.Length, ((uint*)function)[1]);
            RtlVirtualUnwind(0, imageBase, pc, function, (nint)context, out _, out _, 0);
            Assert.Equal(caller, *(ulong*)(context + 248)); Assert.Equal(entrySp + 8, *(ulong*)(context + 152));
            Assert.Equal(rbx, *(ulong*)(context + 144)); Assert.Equal(rsi, *(ulong*)(context + 168)); Assert.Equal(rdi, *(ulong*)(context + 176));
        }
        finally
        {
            try { block?.Dispose(); }
            finally { if (context != null) NativeMemory.AlignedFree(context); if (stack != null) NativeMemory.AlignedFree(stack); }
        }
    }

    [Fact]
    public void DisposingCodeRemovesItsFunctionTableAndOrdinaryBuffersStayUnregistered()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        X64CodeBuffer? block = null;
        try
        {
            block = X64CodeBuffer.Create(Code, Unwind);
            ulong pc = unchecked((ulong)block.DangerousGetHandle()) + 16;
            Assert.NotEqual(0, RtlLookupFunctionEntry(pc, out _, 0));
            block.Dispose(); block = null;
            Assert.Equal(0, RtlLookupFunctionEntry(pc, out _, 0));
            block = X64CodeBuffer.Create([0xc3]);
            Assert.Equal(0, RtlLookupFunctionEntry(unchecked((ulong)block.DangerousGetHandle()), out _, 0));
        }
        finally { block?.Dispose(); }
    }

    [Fact]
    public void InvalidUnwindHeadersAreRejectedBeforePublishingCode()
    {
        if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64) return;
        Assert.Throws<ArgumentException>(() => Reject([], Unwind));
        Assert.Throws<ArgumentException>(() => Reject([0xc3], Unwind));
        Assert.Throws<ArgumentException>(() => Reject(Code, [2, 0, 0, 0]));
        Assert.Throws<ArgumentException>(() => Reject(Code, [1, 16, 4, 0]));
        static void Reject(byte[] code, byte[] unwind)
        {
            X64CodeBuffer? block = null;
            try { block = X64CodeBuffer.Create(code, unwind); }
            finally { block?.Dispose(); }
        }
    }

    [DllImport("kernel32.dll")] private static extern nint RtlLookupFunctionEntry(ulong pc, out ulong imageBase, nint historyTable);
    [DllImport("kernel32.dll")] private static extern nint RtlVirtualUnwind(uint handlerType, ulong imageBase, ulong pc,
        nint functionEntry, nint context, out nint handlerData, out ulong establisherFrame, nint contextPointers);
}
