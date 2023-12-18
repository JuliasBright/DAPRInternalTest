using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Interfaces.Requests
{
    public interface IAlertRequest
    {
        public string ClientName { get; set; }
        public IList<string> AlertTypes { get; set; }

    }
}
