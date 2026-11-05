namespace RetroMediaLab.Chips.Controllers.Adaptec.ACB4000.ACB4000.Constants;

/// <summary>Adresses RAM et échange de contextes du firmware ACB-4000 400108-00.</summary>
internal static class Acb4000MemoryConstants
{
    internal const ushort RamBase = Acb4000DiagnosticConstants.RamBase;
    internal const int DiagnosticCounterOffset = 0x84;
    internal const int DumpAddressOffset = 0x98;
    internal const int DumpFlagsOffset = 0x9c;
    internal const int SavedDumpFlagsOffset = 0x1c;
    internal const byte DumpReadyMask = 0x04;
    internal const int RamLength = 0x100;
    internal const int OtherUnitMask = 1;
    internal const int ContextPointerHighOffset = 0xb0;
    internal const int UnitContextStride = 0x40;
    internal const int WorkingOffset = 0x80;
    internal const int SwappedContextOffset = 0x0f;
    internal const int SwappedContextLength = 0x20;
    internal const int ReadPolicyOffset = 0x1a;
    internal const byte ReadPolicyMask = 0x18;
    internal const int ReadPolicyShift = 3;
    internal const int ContextPointerOffset = 0xaf;
    internal const int DriveControlOffset = 0x9f;
    internal const byte InitialDriveControl = 1;
    internal const int BlockSizeHighOffset = 0xa8;
    internal const byte InitialBlockSizeHigh = 1;
    internal const int HeadCountOffset = 0xa9;
    internal const byte InitialHeadCount = 2;
}
