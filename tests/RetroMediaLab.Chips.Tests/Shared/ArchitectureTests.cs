using System.Reflection;
using RetroMediaLab.Chips.CPU.MOS.MCS6500.Chip6502.Services;

namespace RetroMediaLab.Chips.Tests.Shared;

public sealed class ArchitectureTests
{
    [Fact]
    public void HardwareLibraryDoesNotReferenceAnyRmlProject()
    {
        var references = typeof(Cpu6502).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name!.StartsWith("retromedialab.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EveryPublicComponentHasItsOwnHardwareContract()
    {
        var components = typeof(Cpu6502).Assembly.GetExportedTypes().Where(type => type.IsClass && !type.IsAbstract
            && type.Namespace!.EndsWith(".Services", StringComparison.Ordinal));
        Assert.NotEmpty(components);
        foreach (var component in components)
            Assert.Contains(component.GetInterfaces(), contract => contract.Name == "I" + component.Name
                && contract.Assembly == component.Assembly);
    }
    [Fact]
    public void HardwareContractsExposePublicInstanceSignals()
    {
        var components = typeof(Cpu6502).Assembly.GetExportedTypes().Where(type => type.IsClass && !type.IsAbstract
            && type.Namespace!.EndsWith(".Services", StringComparison.Ordinal));
        foreach (var component in components)
        {
            var contract = Assert.Single(component.GetInterfaces().Where(type => type.Name == "I" + component.Name));
            foreach (var signal in component.GetFields(BindingFlags.Instance | BindingFlags.Public))
                Assert.NotNull(contract.GetProperty(signal.Name));
        }
    }
}
