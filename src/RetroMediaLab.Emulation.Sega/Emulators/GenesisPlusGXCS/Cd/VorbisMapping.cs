using System.IO;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal sealed class VorbisMapping
{
    private readonly VorbisSetup _setup;
    private readonly int[] _mux, _floor, _residue, _magnitude, _angle;
    private readonly bool[] _nonzero, _present, _bundleNonzero;
    private readonly int[][] _memo, _bundle;
    private readonly VorbisTransform _transform = new();
    private VorbisMapping(VorbisBits bits, VorbisSetup setup, int times)
    {
        _setup = setup;
        if (bits.Read(16) != 0) throw new InvalidDataException("Unsupported Vorbis mapping type.");
        int submapsFlag = bits.Read(1), submaps = submapsFlag == 0 ? 1 : bits.Read(4) + 1;
        int couplingFlag = bits.Read(1), steps = couplingFlag == 0 ? 0 : bits.Read(8) + 1;
        if (submapsFlag < 0 || couplingFlag < 0 || submaps < 1 || steps < 0) throw new InvalidDataException("Truncated Vorbis mapping.");
        _magnitude = new int[steps]; _angle = new int[steps];
        int width = VorbisCodebook.ILog((uint)(setup.Channels - 1));
        for (int i = 0; i < steps; i++)
        {
            int magnitude = _magnitude[i] = bits.Read(width), angle = _angle[i] = bits.Read(width);
            if ((uint)magnitude >= setup.Channels || (uint)angle >= setup.Channels || magnitude == angle) throw new InvalidDataException("Invalid Vorbis channel coupling.");
        }
        if (bits.Read(2) != 0) throw new InvalidDataException("Reserved Vorbis mapping bits.");
        _mux = new int[setup.Channels];
        if (submaps > 1)
        {
            for (int i = 0; i < _mux.Length; i++)
            {
                int mux = _mux[i] = bits.Read(4);
                if ((uint)mux >= submaps) throw new InvalidDataException("Invalid Vorbis channel submap.");
            }
        }
        _floor = new int[submaps]; _residue = new int[submaps];
        for (int i = 0; i < submaps; i++)
        {
            int time = bits.Read(8), floor = _floor[i] = bits.Read(8), residue = _residue[i] = bits.Read(8);
            if ((uint)time >= times || (uint)floor >= setup.Floors.Length || (uint)residue >= setup.Residues.Length) throw new InvalidDataException("Invalid Vorbis mapping backend.");
        }
        _nonzero = new bool[setup.Channels]; _present = new bool[setup.Channels]; _bundleNonzero = new bool[setup.Channels];
        _memo = new int[setup.Channels][]; _bundle = new int[setup.Channels][];
        for (int i = 0; i < _memo.Length; i++) _memo[i] = new int[setup.Floors[_floor[_mux[i]]].MemoLength];
    }
    public static VorbisMapping Unpack(VorbisBits bits, VorbisSetup setup, int times) => new(bits, setup, times);
    public void Decode(VorbisBits bits, int[][] pcm, int blockFlag, int previousFlag, int nextFlag)
    {
        int count = _setup.BlockSizes[blockFlag], channels = _setup.Channels;
        for (int channel = 0; channel < channels; channel++)
        {
            VorbisFloor floor = _setup.Floors[_floor[_mux[channel]]];
            _present[channel] = _nonzero[channel] = floor.Decode(bits, _setup.Books, _memo[channel]);
            pcm[channel].AsSpan(0, count / 2).Clear();
        }
        for (int i = 0; i < _magnitude.Length; i++)
            if (_nonzero[_magnitude[i]] || _nonzero[_angle[i]]) _nonzero[_magnitude[i]] = _nonzero[_angle[i]] = true;
        for (int submap = 0; submap < _floor.Length; submap++)
        {
            int bundled = 0;
            for (int channel = 0; channel < channels; channel++)
            {
                if (_mux[channel] != submap) continue;
                _bundleNonzero[bundled] = _nonzero[channel]; _bundle[bundled++] = pcm[channel];
            }
            _setup.Residues[_residue[submap]].Decode(bits, _bundle, _bundleNonzero, bundled, count);
        }
        unchecked
        {
            for (int step = _magnitude.Length - 1; step >= 0; step--)
            {
                int[] magnitude = pcm[_magnitude[step]], angle = pcm[_angle[step]];
                for (int i = 0; i < count / 2; i++)
                {
                    int m = magnitude[i], a = angle[i];
                    if (m > 0)
                    {
                        if (a > 0) { magnitude[i] = m; angle[i] = m - a; }
                        else { angle[i] = m; magnitude[i] = m + a; }
                    }
                    else
                    {
                        if (a > 0) { magnitude[i] = m; angle[i] = m + a; }
                        else { angle[i] = m; magnitude[i] = m - a; }
                    }
                }
            }
        }
        for (int channel = 0; channel < channels; channel++)
        {
            _setup.Floors[_floor[_mux[channel]]].Apply(pcm[channel], count / 2, _memo[channel], _present[channel]);
            _transform.Inverse(count, pcm[channel]);
            if (_nonzero[channel]) VorbisTransform.ApplyWindow(pcm[channel], count, _setup.BlockSizes[previousFlag], _setup.BlockSizes[nextFlag]);
            else pcm[channel].AsSpan(0, count).Clear();
        }
    }
}
