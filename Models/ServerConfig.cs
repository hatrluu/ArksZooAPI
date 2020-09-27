using System;

namespace ArksZooAPI.Models
{
    public class ServerConfig
    {
        public string MapName { get; set; }
        public string GamePath { get; set; }

        public string BackupPath { get; set; }

        public int BackupInterval { get; set; }

        public int HoursSave { get; set; }

    }
}
