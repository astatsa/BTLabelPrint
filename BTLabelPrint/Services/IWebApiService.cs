using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Services
{
    interface IWebApiService
    {
        [Get("/orders")]
        Task<object> GetOrders();
    }
}
