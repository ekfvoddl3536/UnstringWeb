using System.Text;

namespace Unstring.ServerApp;

internal static partial class Unstring
{
    public const int TOKEN_SIZE_PER_WORD = 32;
    public const int MAX_TOKEN_WINDOW_SIZE = 512 * 1024;   // 512K
    public const int MAX_BUFFER_SIZE = TOKEN_SIZE_PER_WORD * MAX_TOKEN_WINDOW_SIZE;
    public const int MAX_DECODE_SIZE = MAX_BUFFER_SIZE * 8;

    public const int E_TOO_LARGE = -1;
    public const int E_CANT_DECODE = -2;
    public const int E_EMPTY = 0;

    private static ReadOnlySpan<byte> KMap => " ._?<>(){}\"\'!, ."u8;

    [StructLayout(LayoutKind.Sequential, Size = sizeof(byte) * 16)]
    private readonly ref struct StackBuffer { public readonly byte Element; }

    #region Encode
    [SkipLocalsInit]
    internal static unsafe int Encode(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text)
    {
        //  too long
        if (text.Length > MAX_BUFFER_SIZE)
            return E_TOO_LARGE;

        scoped ref byte vs = ref MemoryMarshal.GetReference(KMap);

        Unsafe.SkipInit(out StackBuffer __buffer);
        byte* buf = &__buffer.Element;

        scoped var sb = Utf8StringBuilder.Create(ref buffer, text.Length << 3);
        for (int i = 0; i < text.Length; ++i)
        {
            nuint sidx = 0;
            for (int c = text[i]; c != 0; c >>= 4)
            {
                int t = c & 0xF;

                // ' ' or '.'
                if (((t - 2) & 0xF) >= 0xC)
                {
                    // ' ' or '.'
                    buf[sidx++] = Unsafe.Add(ref vs, (uint)t);
                    // ' ' or '.'
                    buf[sidx++] = Unsafe.Add(ref vs, (uint)t >> 3);
                }
                else
                    buf[sidx++] = Unsafe.Add(ref vs, (uint)t);
            }

            WriteSize(ref sb, (int)sidx);
            sb.Append(buf, (int)sidx);
        }

        return sb.Count;
    }

    private static void WriteSize(scoped ref Utf8StringBuilder sb, int c)
    {
        sb.Reserve(c + 1);

        while (--c >= 0)
            sb.Append('.');

        sb.Append(' ');
    }
    #endregion

    #region Decode
    internal static unsafe int Decode(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text)
    {
        if (text.Length > MAX_DECODE_SIZE) return E_TOO_LARGE;

        scoped var sb = Utf8StringBuilder.Create(ref buffer, text.Length << 3);

        Unsafe.SkipInit(out StackBuffer __buffer);
        byte* buf = &__buffer.Element;

        for (int i = 0; ;)
        {
            int count = ReadCount(ref i, text);
            if (count == 0)
            {
                if (i == text.Length)
                    break;
                else
                    return E_CANT_DECODE;
            }

            int item = DecodeCore(ref i, text, count);
            if (item < 0)
                return E_CANT_DECODE;

            int encoded = Encoding.UTF8.GetBytes((char*)&item, 1, buf, sizeof(StackBuffer));
            sb.Append(buf, encoded);
        }
        
        return sb.Count;
    }

    private static int DecodeCore(scoped ref int idx, scoped ReadOnlySpan<char> text, int count)
    {
        scoped ref byte vs = ref MemoryMarshal.GetReference(KMap);

        int res = 0;

        int prev = -1;

        int si = idx;
        int di = si + count;

        int textLength = text.Length;
        scoped ref var ptext = ref MemoryMarshal.GetReference(text);

        int decode_idx = 0;
        for (; si < di && si < textLength; ++si)
        {
            int r = KMap.IndexOf((byte)Unsafe.Add(ref ptext, (uint)si));
            if (r < 0)
                return E_TOO_LARGE;

            // ' ' or '.'
            if (r < 2)
            {
                if (prev >= 0)
                {
                    r = prev + 14 * r;

                    prev = -1;
                }
                else
                {
                    prev = r;
                    continue;
                }
            }
            else if (prev >= 0)
            {
                res |= (prev & 0xF) << (decode_idx++ << 2);
                prev = -1;
            }

            res |= (r & 0xF) << (decode_idx++ << 2);
        }

        if (prev >= 0)
            res |= (prev & 0xF) << (decode_idx++ << 2);

        idx = di;

        return res;
    }

    private static int ReadCount(scoped ref int idx, scoped ReadOnlySpan<char> text)
    {
        int count = 0;

        int i = idx;
        while (i < text.Length)
            if (text[i++] == '.')
                ++count;
            else
                break;

        idx = i;

        return count;
    }
    #endregion
}
