using System.Globalization;
using System.Text;

namespace RetroMediaLab.Chips.Video.NEC.PowerVr2;

internal sealed class PvrShaderSource(string header = "")
{
    private sealed class Constant(string name, string value = "0")
    {
        internal readonly string Name = name;
        internal string Value = value;
    }

    private string _header = header;
    private readonly List<string> _segments = [];
    private readonly List<Constant> _constants = [];

    internal PvrShaderSource SetHeader(string source)
    {
        _header = source;
        return this;
    }

    internal PvrShaderSource AddSource(string source)
    {
        _segments.Add(source);
        return this;
    }

    internal PvrShaderSource AddConstant(string name, string value = "0")
    {
        _constants.Add(new Constant(name, value));
        return this;
    }

    internal PvrShaderSource AddConstant(string name, int value)
    {
        AddConstant(name, value.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    internal PvrShaderSource SetConstant(string name, string value)
    {
        foreach (Constant constant in _constants)
        {
            if (constant.Name == name)
            {
                constant.Value = value;
                return this;
            }
        }
        return AddConstant(name, value);
    }

    internal PvrShaderSource SetConstant(string name, int value)
    {
        SetConstant(name, value.ToString(CultureInfo.InvariantCulture));
        return this;
    }

    internal string Generate()
    {
        var result = new StringBuilder(_header + "\n");
        foreach (Constant constant in _constants)
            result.Append("#define ").Append(constant.Name).Append(' ').Append(constant.Value).Append('\n');
        foreach (string segment in _segments)
            result.Append(segment).Append('\n');
        return result.ToString();
    }
}
