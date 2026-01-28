using Microsoft.Extensions.Localization;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BusTicketing.Services
{
    public class JsonStringLocalizer : IStringLocalizer
    {
        private readonly string _resourcePath;

        public JsonStringLocalizer(string resourcePath)
        {
            _resourcePath = resourcePath;
        }

        private Dictionary<string, string> LoadJson(string culture)
        {
            var filePath = Path.Combine(_resourcePath, $"{culture}.json");

            if (!File.Exists(filePath))
                return new Dictionary<string, string>();

            var jsonData = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(jsonData);
        }

        public LocalizedString this[string name]
        {
            get
            {
                var culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
                var values = LoadJson(culture);

                return values.TryGetValue(name, out string value)
                    ? new LocalizedString(name, value)
                    : new LocalizedString(name, name, true);
            }
        }

        public LocalizedString this[string name, params object[] arguments]
            => this[name];

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var culture = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            var values = LoadJson(culture);

            foreach (var item in values)
                yield return new LocalizedString(item.Key, item.Value);
        }
    }
}
