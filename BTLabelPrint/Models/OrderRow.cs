using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace BTLabelPrint.Models
{
    [XmlType("Row")]
    public class OrderRow
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }
}
