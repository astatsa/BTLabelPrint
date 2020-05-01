using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class OrderContent
    {
        public string Code { get; set; }
        public int? Count { get; set; }
        public double? FulPrice { get; set; }
        public int? Id { get; set; }
        public string Name { get; set; }
        public double? Price { get; set; }
        public string Unit { get; set; }
        public string Url { get; set; }
        public double? Weight { get; set; }
    }
}
