using System;
using System.Linq;
using Sitecore.Ship.Core.Domain;

namespace Sitecore.Ship.Core.Services
{
    public class ZipEntryDataParser
    {
        public static PackageManifestEntry GetManifestEntry(string dataKey)
        {
            if (dataKey.EndsWith("}", StringComparison.InvariantCultureIgnoreCase))
            {
                var elements = dataKey.Split(new[] { "_{" }, 2, StringSplitOptions.None);

                return new PackageManifestEntry
                {
                    ID = new Guid(elements[1].Trim(new[] { '{', '}' })),
                    Path = elements[0]
                };
            }

            if (dataKey.EndsWith("/xml", StringComparison.InvariantCultureIgnoreCase))
            {
                var elements = dataKey.Split(new[] { "/" }, StringSplitOptions.None).Reverse().ToArray();
                // fix - support XML folders
                if (elements.Length > 4)
                {
                    var guid = Guid.Empty;
                    if (Guid.TryParse(elements[3].Trim('{', '}'), out guid))
                    {
                        return new PackageManifestEntry
                        {
                            ID = guid,
                            Path = string.Join("/", elements.Skip(4).Reverse()),
                            Language = elements[2],
                            Version = ParseVersion(elements[1])
                        };
                    }
                }
            }

            const string filesPrefix = "addedfiles";
            if (dataKey.StartsWith(filesPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                return new FileManifestEntry(dataKey.Substring(filesPrefix.Length));
            }

            return new PackageManifestEntryNotFound();
        }

        private static int ParseVersion(string version)
        {
            int parsedVersion;
            if (int.TryParse(version, out parsedVersion))
            {
                return parsedVersion;
            }
            return 0;
        }
    }
}