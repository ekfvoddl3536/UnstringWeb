namespace Unstring.ServerApp;

public static class Program
{
    private const int MAX_REQUEST_BODY_SIZE = 3 * 1024 * 1024; // 3 MiB

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        var serverConf = builder.Configuration.GetSection("Server");
        builder.WebHost.ConfigureKestrel(k =>
        {
            k.AddServerHeader = false;

            var port = serverConf.GetValue("ListenPort", 8080);
            var open = serverConf.GetValue("PublicHost", false);

            // public domain
            if (open)
                k.ListenAnyIP(port);
            else
                k.ListenLocalhost(port);

            k.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(30);
            k.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(15);

            k.Limits.MaxRequestBodySize = MAX_REQUEST_BODY_SIZE;
        });

        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        var localBuffer = new ThreadLocal<byte[]?>(static () => null, false);

        app.MapPost("/encode", async ctx =>
        {
            await Server.WorkAsync<Server.PEncode>(ctx, localBuffer).ConfigureAwait(false);
        });

        app.MapPost("/hash-encode", async ctx =>
        {
            await Server.WorkAsync<Server.PEncodeHashed>(ctx, localBuffer).ConfigureAwait(false);
        });

        app.MapPost("/decode", async ctx =>
        {
            await Server.WorkAsync<Server.PDecode>(ctx, localBuffer).ConfigureAwait(false);
        });

        app.Run();
    }
}
