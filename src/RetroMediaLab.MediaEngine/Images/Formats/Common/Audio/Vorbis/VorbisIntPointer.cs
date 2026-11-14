namespace RetroMediaLab.MediaEngine.Images.Formats.Common.Audio.Vorbis;

/// <summary>Cursor over the integer buffers used by the Vorbis transforms.</summary>
internal ref struct VorbisIntPointer(Span<int> data,int position = 0)
{
    private readonly Span<int> _data = data;
    private readonly int _position = position;
    public ref int this[int index] => ref _data[_position+index];
    public Span<int> AsSpan(int length) => _data.Slice(_position,length);
    public static VorbisIntPointer operator +(VorbisIntPointer value,int offset)=>new(value._data,value._position+offset);
    public static VorbisIntPointer operator -(VorbisIntPointer value,int offset)=>value+(-offset);
    public static int operator -(VorbisIntPointer left,VorbisIntPointer right)=>left._position-right._position;
    public static VorbisIntPointer operator ++(VorbisIntPointer value)=>value+1;
    public static VorbisIntPointer operator --(VorbisIntPointer value)=>value-1;
    public static bool operator <(VorbisIntPointer left,VorbisIntPointer right)=>left._position<right._position;
    public static bool operator >(VorbisIntPointer left,VorbisIntPointer right)=>left._position>right._position;
    public static bool operator <=(VorbisIntPointer left,VorbisIntPointer right)=>left._position<=right._position;
    public static bool operator >=(VorbisIntPointer left,VorbisIntPointer right)=>left._position>=right._position;
}
