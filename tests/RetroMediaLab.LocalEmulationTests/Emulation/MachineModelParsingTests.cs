using System.Globalization;
using System.Reflection;

namespace RetroMediaLab.Tests.Emulation;

public sealed class MachineModelParsingTests
{
    public static IEnumerable<object[]> Catalogs()
    {
        yield return [typeof(RetroMediaLab.Emulation.Acorn.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Apple.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Atari.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Bandai.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Coleco.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Epoch.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Fairchild.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Mattel.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Panasonic.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Philips.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Sharp.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Sinclair.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Snk.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Thomson.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Watara.Common.Machines.Common.Dictionaries.ModelCatalog)];
        yield return [typeof(RetroMediaLab.Emulation.Wenquxing.Common.Machines.Common.Dictionaries.ModelCatalog)];
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void DefinedNamesNumbersAndSurroundingWhitespaceKeepTheirMeaning(Type catalog)
    {
        var parse = catalog.GetMethod("Parse")!;
        foreach (var name in Enum.GetNames(parse.ReturnType))
        {
            var expected = Enum.Parse(parse.ReturnType, name);
            Assert.Equal(expected, parse.Invoke(null, [name]));
            Assert.Equal(expected, parse.Invoke(null, ["  " + name + "  "]));
            var number = Convert.ToInt64(expected, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            Assert.Equal(expected, parse.Invoke(null, [number]));
        }
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void InvalidIdentifiersKeepParameterNameAndActualValue(Type catalog)
    {
        var parse = catalog.GetMethod("Parse")!;
        foreach (var id in new string?[] { null, "", "absent-model", "2147483647", "-2147483648" })
            Reject(parse, id);
    }

    [Theory]
    [MemberData(nameof(Catalogs))]
    public void ModelNamesRemainCaseSensitive(Type catalog)
    {
        var parse = catalog.GetMethod("Parse")!;
        var names = Enum.GetNames(parse.ReturnType);
        foreach (var name in names)
        {
            var changed = name.ToLowerInvariant();
            if (changed == name)
                changed = name.ToUpperInvariant();
            if (changed == name || names.Contains(changed, StringComparer.Ordinal))
                continue;
            Reject(parse, changed);
        }
    }

    private static void Reject(MethodInfo parse, string? id)
    {
        var wrapper = Assert.Throws<TargetInvocationException>(() => parse.Invoke(null, [id]));
        var error = Assert.IsType<ArgumentOutOfRangeException>(wrapper.InnerException);
        Assert.Equal("id", error.ParamName);
        Assert.Equal(id, error.ActualValue);
    }
}
