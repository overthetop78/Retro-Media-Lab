using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RetroMediaLab.Chips.Common.Memory;

public ref struct ArrayPointer<T>
{
    private Span<byte> _bytes;
    private int _position;
    private Memory<byte> _memory;
    private bool _memoryBacked;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayPointer(Span<T> memory, int position = 0)
    {
        ref byte first = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(memory));
        _bytes = MemoryMarshal.CreateSpan(ref first, memory.Length * Unsafe.SizeOf<T>());
        _position = position * Unsafe.SizeOf<T>();
        _memory = default;
        _memoryBacked = false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayPointer(T[] memory, int position = 0) : this(memory.AsSpan(), position)
    {
        if (typeof(T) == typeof(byte))
        {
            _memory = (byte[])(object)memory;
            _memoryBacked = true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ArrayPointer(Memory<T> memory, int position = 0) : this(memory.Span, position)
    {
        if (typeof(T) == typeof(byte))
        {
            _memory = Unsafe.As<Memory<T>, Memory<byte>>(ref memory);
            _memoryBacked = true;
        }
    }

    public readonly Memory<byte> Memory => typeof(T) == typeof(byte) && _memoryBacked
        ? _memory.Slice(_position)
        : throw new InvalidOperationException("This pointer does not refer to a managed byte memory view.");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ArrayPointer(Span<byte> bytes, int bytePosition, bool raw)
    {
        _bytes = bytes;
        _position = bytePosition;
        _memory = default;
        _memoryBacked = false;
    }

    public readonly bool IsNull => _bytes.IsEmpty;

    public readonly ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_bytes.Slice(_position + index * Unsafe.SizeOf<T>(), Unsafe.SizeOf<T>())));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ArrayPointer<U> Cast<U>()
    {
        if (typeof(T) != typeof(U) && (RuntimeHelpers.IsReferenceOrContainsReferences<T>() || RuntimeHelpers.IsReferenceOrContainsReferences<U>()))
            throw new InvalidOperationException("Cannot reinterpret managed references.");
        return new(_bytes, _position, true) { _memory = _memory, _memoryBacked = _memoryBacked };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Same(ArrayPointer<T> other) =>
        Unsafe.ByteOffset(ref MemoryMarshal.GetReference(other._bytes), ref MemoryMarshal.GetReference(_bytes)) + _position - other._position == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> Bytes(int length) => _bytes.Slice(_position, length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPointer<T> operator +(ArrayPointer<T> pointer, int offset) { pointer._position += offset * Unsafe.SizeOf<T>(); return pointer; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPointer<T> operator -(ArrayPointer<T> pointer, int offset) { pointer._position -= offset * Unsafe.SizeOf<T>(); return pointer; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int operator -(ArrayPointer<T> left, ArrayPointer<T> right) => unchecked((int)(Unsafe.ByteOffset(ref MemoryMarshal.GetReference(right._bytes), ref MemoryMarshal.GetReference(left._bytes)) + left._position - right._position)) / Unsafe.SizeOf<T>();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(ArrayPointer<T> left, ArrayPointer<T> right) =>
        Unsafe.ByteOffset(ref MemoryMarshal.GetReference(right._bytes), ref MemoryMarshal.GetReference(left._bytes)) + left._position - right._position < 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(ArrayPointer<T> left, ArrayPointer<T> right) => right < left;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(ArrayPointer<T> left, ArrayPointer<T> right) => !(right < left);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(ArrayPointer<T> left, ArrayPointer<T> right) => !(left < right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPointer<T> operator ++(ArrayPointer<T> pointer) => pointer + 1;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayPointer<T> operator --(ArrayPointer<T> pointer) => pointer - 1;

    public static implicit operator ArrayPointer<T>(T[] memory) => new(memory);
    public static implicit operator ArrayPointer<T>(Span<T> memory) => new(memory);
}
