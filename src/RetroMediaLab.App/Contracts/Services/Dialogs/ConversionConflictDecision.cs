using ConversionOutput = RetroMediaLab.MediaEngine.Images.Conversion.ConversionOutput;
using RetroMediaLab.App.Views.Dialogs.Conversion;

namespace RetroMediaLab.App.Contracts.Services.Dialogs;

public sealed record ConversionConflictDecision(ConversionOutput Output, ConversionConflictChoice Choice);
