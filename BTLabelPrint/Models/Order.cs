using BTLabelPrint.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class Order
    {
        public int? Id { get; set; }
        [JsonProperty("name_buyer")]
        public string BuyerName { get; set; }
        public string Comment { get; set; }
        [JsonProperty("add_date")]
        [JsonConverter(typeof(DateTimeJsonConverter))]
        public DateTime? AddDate { get; set; }
        public string Address { get; set; }
        [JsonProperty("close_date")]
        [JsonConverter(typeof(DateTimeJsonConverter))]
        public DateTime? CloseDate { get; set; }
        [JsonProperty("updata_date")]
        [JsonConverter(typeof(DateTimeJsonConverter))]
        public DateTime? UpdateDate { get; set; }
        public string Number { get; set; }
        public string Phone { get; set; }
        [JsonProperty("status_id")]
        public int? StatusId { get; set; }
        public double? Summ { get; set; }
        [JsonProperty("order_content")]
        public List<OrderContent> OrderContent { get; set; }
        [JsonProperty("delivery_cost")]
        public double? DeliveryCost { get; set; }
        public double? TotalCost => (Summ ?? 0) + (DeliveryCost ?? 0);
    }
}
