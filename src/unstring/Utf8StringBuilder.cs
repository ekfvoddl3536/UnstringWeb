using System.Diagnostics;

namespace Unstring.ServerApp;

internal unsafe ref struct Utf8StringBuilder
{
    private readonly ref byte[] _buffer;
    private byte* _last;
    private byte* _ptr;
    private byte* _end;

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public Utf8StringBuilder(ref byte[] buffer)
    {
        _buffer = ref buffer;

        _ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(buffer));
        _last = _ptr;
        _end = _ptr + buffer.Length;
    }

    public readonly int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => (int)(_end - _ptr);
    }

    public readonly int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        get => (int)(_last - _ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(char item)
    {
        Debug.Assert(item < byte.MaxValue, "only accept ASCII");
        Append((byte)item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(byte item)
    {
        Reserve(1);
        
        *_last++ = item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Reserve(int count) => EnsureCapacity(Count + count);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void EnsureCapacity(int min_capacity)
    {
        //  align 4
        if ((uint)min_capacity > (uint)_buffer.Length)
        {
            min_capacity = (min_capacity + 3) & ~3;
            var next_capacity = int.Max(min_capacity, min_capacity << 1);
            if (next_capacity < 0)
                throw new OverflowException();

            int size = Count;
            _buffer = GC.AllocateUninitializedArray<byte>(next_capacity, true);
            
            _ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(_buffer));
            _last = _ptr + size;
            _end = _ptr + next_capacity;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static Utf8StringBuilder Create(ref byte[]? buffer, int min_capacity)
    {
        buffer ??= GC.AllocateUninitializedArray<byte>(4096, true);
        var __retVal = new Utf8StringBuilder(ref buffer);
        __retVal.EnsureCapacity(min_capacity);
        return __retVal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public void Append(byte* src, int count)
    {
        var off = (nuint)(uint)count;
        
        Reserve((int)off);

        var last = _last;
        _last += off;

        NativeMemory.Copy(src, last, off);
    }
}
