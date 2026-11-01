using EnabledOption = RetroMediaLab.MediaEngine.Contracts.Options.EnabledOption;
namespace RetroMediaLab.App.ViewModels.Operations.Options;

public sealed class FlagOptionViewModel(string argument) : OperationOptionViewModelBase(argument)
{
    public override EnabledOption ToEnabledOption() => new(Argument);
}
