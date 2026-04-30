using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using UniWebApp.Resources;
using System.Resources;

namespace UniWebApp.Services;

public class SharedResourceLocalizerFactory : IStringLocalizerFactory
{
    private readonly IResourceNamesCache _resourceNamesCache;
    private readonly ILoggerFactory _loggerFactory;

    public SharedResourceLocalizerFactory(IResourceNamesCache resourceNamesCache, ILoggerFactory loggerFactory)
    {
        _resourceNamesCache = resourceNamesCache ?? throw new ArgumentNullException(nameof(resourceNamesCache));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public IStringLocalizer Create(Type resourceSource)
    {
        if (resourceSource == typeof(SharedResource))
        {
            return new ResourceManagerStringLocalizer(
                SharedResource.ResourceManager,
                typeof(SharedResource).Assembly,
                "UniWebApp.Resources.SharedResource",
                _resourceNamesCache,
                _loggerFactory.CreateLogger<ResourceManagerStringLocalizer>());
        }
        
        throw new NotSupportedException($"Resource type {resourceSource.Name} isn't supported");
    }

    public IStringLocalizer Create(string baseName, string location)
    {
        if (baseName == "UniWebApp.Resources.SharedResource" || baseName == "SharedResource")
        {
            return new ResourceManagerStringLocalizer(
                SharedResource.ResourceManager,
                typeof(SharedResource).Assembly,
                "UniWebApp.Resources.SharedResource",
                _resourceNamesCache,
                _loggerFactory.CreateLogger<ResourceManagerStringLocalizer>());
        }
        
        throw new NotSupportedException($"Resource base name {baseName} isn't supported");
    }
}

