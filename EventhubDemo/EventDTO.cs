using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventhubDemo
{
    public class EventDTO
    {
        public string Subject { get; set; }
        public string Message { get; set; }
        public string From { get; set; }
    }
}
