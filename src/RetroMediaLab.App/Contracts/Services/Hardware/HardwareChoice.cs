using RetroMediaLab.Infrastructure.Settings.Hardware;
namespace RetroMediaLab.App.Contracts.Services.Hardware;

public sealed record HardwareChoice(DriveSettings Drive, string Port, bool Available, string Label);
