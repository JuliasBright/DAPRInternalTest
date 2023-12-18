using Common.Interfaces.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models.Responses
{
    public class AlertResponse : IAlertResponse
    {
        public string Response { get ; set ; }
    }
}
