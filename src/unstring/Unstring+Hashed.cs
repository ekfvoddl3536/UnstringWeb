namespace Unstring.ServerApp;

partial class Unstring
{
    internal static int EncodeHashed(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text)
    {
        //  too long
        if (text.Length > MAX_BUFFER_SIZE)
            return E_TOO_LARGE;

        scoped ref byte vs = ref MemoryMarshal.GetReference(KMap);

        scoped var sb = Utf8StringBuilder.Create(ref buffer, text.Length << 3);

        int bnum = text.Length + Next(text.Length);
        for (int i = 0; i < text.Length; ++i)
        {
            for (int c = text[i]; c != 0; c >>= 4)
            {
                int padding = Next(c + bnum) & 0x1F;
                for (; padding > 0; --padding)
                {
                    bnum = Next(bnum);

                    int temp = bnum & 0xF;
                    if (temp < 0x6)
                        sb.Append('.');
                    else if (temp < 0xE)
                        sb.Append(Unsafe.Add(ref vs, temp - 2));
                    else if (temp < 0xE)
                        sb.Append('\"');
                    else
                        sb.Append(' ');
                }

                sb.Append(Unsafe.Add(ref vs, c & 0xF));
            }

            bnum = Next(bnum);
        }

        return sb.Count;
    }

    private static int Next(int num)
    {
        int res = num * 1160377727;
        for (int i = res; i > 1;)
            if ((i & 1) != 0)
            {
                res = (res >> 2) + (res >> 5) + (res >> 12) + res * 1524124499;
                ++i;
            }
            else
            {
                res -= res >> 12;
                i >>= 1;
            }

        return res;
    }
}
