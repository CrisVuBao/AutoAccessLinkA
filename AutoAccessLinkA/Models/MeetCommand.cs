using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAccessLinkA.Models
{
    public class MeetCommand
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string MeetLink { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public BrowserType Browser { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Executing, Done
    }
}
