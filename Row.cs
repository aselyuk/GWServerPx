using System;

namespace GWServerPx
{
    public class Row
    {
        public string Name { get; set; }
        public DateTime EndDate { get; set; }
        public bool SendAlert { get; set; }
        public bool IsApp { get; set; }
        public TimeSpan DiffDays { get; set; }
    }
}
