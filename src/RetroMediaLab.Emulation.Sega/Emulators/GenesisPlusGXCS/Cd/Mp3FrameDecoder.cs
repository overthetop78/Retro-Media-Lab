using RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Video;

namespace RetroMediaLab.Emulation.Sega.Emulators.GenesisPlusGXCS.Cd;

internal readonly record struct Mp3FrameInfo(int FrameBytes, int FrameOffset, int Channels, int SampleRate, int Layer, int BitrateKbps);

internal sealed partial class Mp3FrameDecoder
{
    public const int MaximumSamplesPerFrame = 1152 * 2;
    private readonly mp3dec_t[] _decoder = [new()];
    private readonly mp3dec_frame_info_t[] _info = [new()];
    private readonly mp3dec_scratch_t[] _scratch = [new()];
    private readonly bs_t[] _frameBits = [new()];
    private readonly L12_scale_info[] _scaleInfo = [new()];
    private mp3dec_scratch_t scratch => _scratch[0];
    private CPointer<mp3dec_scratch_t> scratch_ptr => new(_scratch);
    private CPointer<bs_t> frame_bits => new(_frameBits);
    private CPointer<L12_scale_info> scale_info => new(_scaleInfo);
    private int found_frame_size;

    public void Reset() => mp3dec_init(new(_decoder));
    public int Decode(Memory<byte> input, int byteCount, Span<short> samples, out Mp3FrameInfo info)
    {
        if ((uint)byteCount > input.Length) throw new ArgumentOutOfRangeException(nameof(byteCount));
        if (!samples.IsEmpty && samples.Length < MaximumSamplesPerFrame) throw new ArgumentException("MP3 frame output buffer is too short.", nameof(samples));
        int frames = mp3dec_decode_frame(new(_decoder), new(input), byteCount, samples.IsEmpty ? default : new CPointer<short>(samples), new(_info));
        mp3dec_frame_info_t result = _info[0];
        info = new(result.frame_bytes, result.frame_offset, result.channels, result.hz, result.layer, result.bitrate_kbps);
        return frames;
    }
    private int FindFrame(CPointer<byte> input, int bytes, CPointer<mp3dec_t> decoder)
    {
        Span<int> fields = stackalloc int[2]; fields[0] = decoder[0].free_format_bytes; fields[1] = 0;
        int result = mp3d_find_frame(input, bytes, new(fields), new(fields, 1));
        decoder[0].free_format_bytes = fields[0]; found_frame_size = fields[1];
        return result;
    }
    private static CPointer<bs_t> ScratchBits(CPointer<mp3dec_scratch_t> state) => new(state[0].BitsState);
    private static void CopyBytes(CPointer<byte> destination, CPointer<byte> source, int count) => source.Bytes(count).CopyTo(destination.Bytes(count));
    private static void CopyFloats(CPointer<float> destination, CPointer<float> source, int count) => source.Bytes(count * sizeof(float)).CopyTo(destination.Bytes(count * sizeof(float)));
    private static void FillBytes(CPointer<byte> destination, int value, int count) => destination.Bytes(count).Fill((byte)value);
    private static void ClearFloats(CPointer<float> destination, int count) => destination.Bytes(count * sizeof(float)).Clear();
    private static void ClearDecoder(CPointer<mp3dec_t> state)
    {
        mp3dec_t decoder = state[0];
        decoder.mdct_overlap.AsSpan().Clear(); decoder.qmf_state.AsSpan().Clear();
        decoder.reserv = decoder.free_format_bytes = 0;
        decoder.header.AsSpan().Clear(); decoder.reserv_buf.AsSpan().Clear();
    }
}
