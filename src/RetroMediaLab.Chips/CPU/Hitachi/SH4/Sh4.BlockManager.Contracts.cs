using System.Threading;

namespace RetroMediaLab.Chips.CPU.Hitachi.SH4;

#if SH4_DYNAREC_NONE
internal class Sh4RuntimeBlockInfo
#else
internal class Sh4RuntimeBlockInfo(Sh4BlockManager blockManager) : IDisposable
#endif
{
#if !SH4_DYNAREC_NONE
    private readonly byte[] _identity = GC.AllocateUninitializedArray<byte>(1, pinned: true);
    private bool _disposed;
    private int _referenceCount;

    internal unsafe nuint NativeIdentity
    {
        get
        {
            fixed (byte* address = _identity)
                return (nuint)address;
        }
    }
#endif

    public uint Address { get; set; }
    public uint VirtualAddress { get; set; }
    public nuint Code { get; set; }
    public uint HostCodeSize { get; set; }
    public uint GuestCodeSize { get; set; }
    public Sh4Fpscr FpuConfiguration { get; set; }
    public uint GuestCycles { get; set; }
    public uint GuestOpcodes { get; set; }
    public uint HostOpcodes { get; set; }
    public bool HasFpuOperation { get; set; }
    public bool Temporary { get; set; }
    public uint BlockCheckFailures { get; set; }
    public uint BranchAddress { get; set; }
    public uint NextAddress { get; set; }
    public Sh4RuntimeBlockInfo? Branch { get; set; }
    public Sh4RuntimeBlockInfo? Next { get; set; }
    public uint RelinkOffset { get; set; }
    public uint RelinkData { get; set; }
    public Sh4BlockEndType BlockType { get; set; }
    public bool HasConditionalJump { get; set; }
    public bool ReadOnly { get; set; }
    public List<Sh4ShilOpcode> Opcodes { get; } = [];
    public List<Sh4RuntimeBlockInfo> Predecessors { get; } = [];

#if !SH4_DYNAREC_NONE
    public bool Setup(Sh4RecompilerDriver driver, uint pc, Sh4Fpscr fpuConfiguration) =>
        driver.SetupBlock(this, pc, fpuConfiguration);
#endif

    public bool ContainsCode(nuint pointer) => unchecked((uint)(pointer - Code)) < HostCodeSize;

    public virtual uint Relink() => 0;

#if !SH4_DYNAREC_NONE
    public void AddRef(Sh4RuntimeBlockInfo predecessor) =>
        Sh4BlockManager.AddReference(this, predecessor);

    public void RemoveRef(Sh4RuntimeBlockInfo predecessor) =>
        Sh4BlockManager.RemoveReference(this, predecessor);

    public void Discard() => blockManager.DiscardReferences(this);

    public void SetProtectedFlags() => blockManager.SetProtectedFlags(this);

    internal void RetainReference() => Interlocked.Increment(ref _referenceCount);

    internal void ReleaseReference()
    {
        if (Interlocked.Decrement(ref _referenceCount) == 0)
            Dispose();
    }

    internal void ReleasePredecessors()
    {
        foreach (var predecessor in Predecessors)
            predecessor.ReleaseReference();
        Predecessors.Clear();
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        try
        {
            blockManager.ReleaseBlock(this);
        }
        finally
        {
            ReleasePredecessors();
        }
    }
#endif
}

internal sealed partial class Sh4BlockManager
{
    internal static bool HandleRamWriteAccess(nuint address)
    {
#if SH4_DYNAREC_NONE
        return false;
#else
        return Sh4RecompilerDriver.Current.BlockManager.RamWriteAccess(address);
#endif
    }

#if SH4_DYNAREC_NONE
    public void RamWriteAccess(uint address)
    {
    }

    public void LockPage(uint address) => LockPage(address, (uint)Environment.SystemPageSize);

    public void LockPage(uint address, uint size)
    {
    }

    public void UnlockPage(uint address) => UnlockPage(address, (uint)Environment.SystemPageSize);

    public void UnlockPage(uint address, uint size)
    {
    }

    public uint GetRamOffset(nuint pointer) => 0;
#else
    public bool IsRamPageProtected(uint address) => !_unprotectedPages[GetRamPage(address)];

    public void LockPage(uint address) => LockPage(address, PageSize);

    public void UnlockPage(uint address) => UnlockPage(address, PageSize);
#endif
}
