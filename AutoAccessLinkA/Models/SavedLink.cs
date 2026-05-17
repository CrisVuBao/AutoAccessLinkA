using System;

namespace AutoAccessLinkA.Models
{
    public class SavedLink
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public BrowserType Browser { get; set; }
    }
}
