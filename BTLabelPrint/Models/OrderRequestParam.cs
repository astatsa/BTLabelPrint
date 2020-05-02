using Newtonsoft.Json;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class OrderRequestParam : IFormattable
    {
        [JsonProperty("page")]
        public int Page { get; set; }
        [JsonProperty("count")]
        public int Count { get; set; }

        public OrderRequestParam(int page, int count = 250)
        {
            this.Page = page;
            this.Count = count;
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            return ToString();
        }
    }
}
