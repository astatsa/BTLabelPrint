using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Models
{
    class ApiParam
    {
        public ApiParam()
        {

        }

        public ApiParam(object param)
        {
            Param = JsonConvert.SerializeObject(param);
        }

        public string Token => AppSettings.Token;
        public string Method { get; set; }
        public string Param { get; set; }
    }
}
