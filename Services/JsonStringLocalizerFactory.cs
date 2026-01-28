using Microsoft.Extensions.Localization;

namespace BusTicketing.Services
{
    public class JsonStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly string _resourcesPath;

        public JsonStringLocalizerFactory(IWebHostEnvironment env)
        {
            _resourcesPath = Path.Combine(env.ContentRootPath, "Resources");
        }

        public IStringLocalizer Create(Type resourceSource)
            => new JsonStringLocalizer(_resourcesPath);

        public IStringLocalizer Create(string baseName, string location)
            => new JsonStringLocalizer(_resourcesPath);
    }
}
