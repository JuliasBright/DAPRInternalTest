using Common.Interfaces.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Requests
{
    public class AlertRequest : IAlertRequest
    {
        public string? ClientName { get ; set ; }
        public IList<string>? AlertTypes { get ; set; }
    }
}
