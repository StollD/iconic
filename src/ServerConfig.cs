using System;
using System.Collections.Generic;

namespace Iconic
{
    /// <summary>
    /// The configuration for a server
    /// </summary>
    public class ServerConfig
    {
        public Int32 Id { get; set; }
        public UInt64 Server { get; set; }
        public Single IntervalMin { get; set; }
        public Single IntervalMax { get; set; }
        public List<String> Thumbnails { get; set; }
        public DateTime NextSet { get; set; }

        public ServerConfig()
        {
            IntervalMax = 30;
            IntervalMin = 30;
        }
    }
}