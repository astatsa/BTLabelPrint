using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class OrderRequestParam
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
    }
}
