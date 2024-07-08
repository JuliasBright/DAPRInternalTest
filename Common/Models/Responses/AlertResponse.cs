using Common.Interfaces.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Responses
{
    public class AlertResponse
    {
        public string Message { get; set; }
        public DateTime ResponseTime { get; set; }
    }
}

