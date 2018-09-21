namespace Sitecore.Ship.Core.Domain
{
    public class PublishParameters
    {
        public bool Related { get; set; }
        public bool Deep { get; set; }
        public string Mode { get; set; }
        public string Source { get; set; }
        public string[] Targets { get; set; }
        public string[] Languages { get; set; }
    }
}