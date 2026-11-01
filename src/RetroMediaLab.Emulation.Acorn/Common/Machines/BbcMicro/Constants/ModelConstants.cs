namespace RetroMediaLab.Emulation.Acorn.Common.Machines.BbcMicro.Constants;

internal static class BbcMicroModelConstants
{
    internal const string ModelAId = nameof(MachineModel.BbcMicroModelA);
    internal const string ModelBId = nameof(MachineModel.BbcMicroModelB);
    internal const string ModelBPlusId = nameof(MachineModel.BbcMicroModelBPlus);
    internal const string ModelBPlus128Id = nameof(MachineModel.BbcMicroModelBPlus128);
    internal const string Master128Id = nameof(MachineModel.BbcMaster128);
    internal const string ModelADisplayNameResource = "Emulation.Acorn.Model.BbcMicroModelA";
    internal const string ModelBDisplayNameResource = "Emulation.Acorn.Model.BbcMicroModelB";
    internal const string ModelBPlusDisplayNameResource = "Emulation.Acorn.Model.BbcMicroModelBPlus";
    internal const string ModelBPlus128DisplayNameResource = "Emulation.Acorn.Model.BbcMicroModelBPlus128";
    internal const string Master128DisplayNameResource = "Emulation.Acorn.Model.BbcMaster128";
    internal const int CyclesPerLine = 128;
    internal const int LinesPerFrame = 312;
    internal const int CyclesPerFrame = CyclesPerLine * LinesPerFrame;
    internal const string SixteenKibibytesValue = "16";
    internal const string ThirtyTwoKibibytesValue = "32";
    internal const string SixteenKibibytesDisplay = "16 KiB";
    internal const string ThirtyTwoKibibytesDisplay = "32 KiB";
    internal const long CpuFrequencyHz = 2_000_000;
    internal const long SixteenKibibytes = 16 * 1024;
    internal const long ThirtyTwoKibibytes = 32 * 1024;
    internal const long SixtyFourKibibytes = 64 * 1024;
    internal const long OneHundredTwentyEightKibibytes = 128 * 1024;
    internal const int OnePort = 1;
    internal const int TwoPorts = 2;
}
