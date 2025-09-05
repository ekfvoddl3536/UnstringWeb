using System.Net;
using System.Text;

namespace Unstring.ServerApp;

internal static class Server
{
    public interface IUnstringProcessing<TSelf>
        where TSelf : unmanaged, IUnstringProcessing<TSelf>, allows ref struct
    {
        static abstract int Invoke(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text);
    }

    public readonly ref struct PEncode : IUnstringProcessing<PEncode>
    {
        public static int Invoke(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text) =>
            Unstring.Encode(ref buffer, text);
    }

    public readonly ref struct PDecode : IUnstringProcessing<PDecode>
    {
        public static int Invoke(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text) =>
            Unstring.Decode(ref buffer, text);
    }

    public readonly ref struct PEncodeHashed : IUnstringProcessing<PEncodeHashed>
    {
        public static int Invoke(scoped ref byte[]? buffer, scoped ReadOnlySpan<char> text) =>
            Unstring.EncodeHashed(ref buffer, text);
    }

    internal static async ValueTask<bool> WorkAsync<TMethod>(HttpContext ctx, ThreadLocal<byte[]?> localBuffer)
        where TMethod : unmanaged, IUnstringProcessing<TMethod>, allows ref struct
    {
        byte[]? buffer =
            Thread.CurrentThread.IsThreadPoolThread
            ? localBuffer.Value
            : null;

        var body = await ctx.Request.BodyReader.ReadAsync(ctx.RequestAborted).ConfigureAwait(false);
        if (body.IsCanceled)
            return false;

        var bodyBuffer = body.Buffer;
        ctx.Request.BodyReader.AdvanceTo(bodyBuffer.Start, bodyBuffer.End);

        var bodyContent = Encoding.UTF8.GetString(in bodyBuffer).AsSpan();

        var offset = TMethod.Invoke(ref buffer, bodyContent);

        if (Thread.CurrentThread.IsThreadPoolThread)
            localBuffer.Value = buffer;

        if (offset <= 0)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            ctx.Response.Headers.TryAdd("X-Handled-ErrorCode", offset.ToString());

            await ctx.Response.CompleteAsync().ConfigureAwait(false);

            return false;
        }

        ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        var result = await ctx.Response.BodyWriter.WriteAsync(buffer.AsMemory(0, offset), ctx.RequestAborted).ConfigureAwait(false);
        if (result.IsCanceled)
            return false;

        await ctx.Response.CompleteAsync().ConfigureAwait(false);

        return true;
    }
}
