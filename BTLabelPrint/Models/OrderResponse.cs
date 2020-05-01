using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class OrderResponse
    {
        public int Count { get; set; }
        public int CountOrder { get; set; }
        public int Page { get; set; }
        public List<Order> Orders { get; set; }
    }
}
