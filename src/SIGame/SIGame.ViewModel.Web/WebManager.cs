using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using SICore.Clients;
using SICore.Contracts;
using SIPackages;

namespace SIGame.ViewModel.Web;

public sealed class WebManager : IFileShare
{
    private readonly int _port;
    private readonly WebApplication _webApplication;

    public WebManager(int port, Dictionary<ResourceKind, string> resourceLocations)
    {
        if (port < 1 || port > 65535)
        {
            throw new ArgumentException($"Invalid multimedia port value {port}. Port must be between 1 and 65535", nameof(port));
        }

        _port = port;

        var builder = WebApplication.CreateBuilder();

        _webApplication = builder.Build();

        _webApplication.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(resourceLocations[ResourceKind.DefaultAvatar]),
            RequestPath = "/defaultAvatars"
        });

        _webApplication.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(resourceLocations[ResourceKind.Avatar]),
            RequestPath = "/avatars"
        });

        var packageFolder = resourceLocations[ResourceKind.Package];

        var imagesFolder = Path.Combine(packageFolder, CollectionNames.ImagesStorageName);

        if (Directory.Exists(imagesFolder))
        {
            _webApplication.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(imagesFolder),
                RequestPath = "/package/Images",
            });
        }
        var audioFolder = Path.Combine(packageFolder, CollectionNames.AudioStorageName);

        if (Directory.Exists(audioFolder))
        {
            _webApplication.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(audioFolder),
                RequestPath = "/package/Audio",
            });
        }

        var videoFolder = Path.Combine(packageFolder, CollectionNames.VideoStorageName);

        if (Directory.Exists(videoFolder))
        {
            _webApplication.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(videoFolder),
                RequestPath = "/package/Video",
            });
        }

        _webApplication.RunAsync($"http://+:{port}");
    }

    public Uri CreateResourceUri(ResourceKind resourceKind, Uri relativePath) =>
        new(resourceKind switch
        {
            ResourceKind.DefaultAvatar => $"http://localhost:{_port}/defaultAvatars/{relativePath}",
            ResourceKind.Avatar => $"http://localhost:{_port}/avatars/{relativePath}",
            _ => $"http://localhost:{_port}/package/{relativePath}"
        });

    public ValueTask DisposeAsync() => _webApplication.DisposeAsync();
}
