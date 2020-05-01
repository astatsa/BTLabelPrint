using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class ApiResponse<TResponse>
    {
        public int Error { get; set; }
        public string Status { get; set; }
        public TResponse Response { get; set; }
    }
}
