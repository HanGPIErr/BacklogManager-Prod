using System;

namespace BacklogManager.Domain
{
    public class VersionInfo
    {
        public string Version { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string DownloadUrl { get; set; }
        public bool Mandatory { get; set; }
        public string Changelog { get; set; }
        public string MinimumVersion { get; set; }
    }
}
