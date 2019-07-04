namespace Sitecore.Ship.Core.Domain
{
    public class InstallPackage : PackageCommandsBase
    {
        public string Path { get; set; }
        public bool ReturnManifest { get; set; }
    }
}