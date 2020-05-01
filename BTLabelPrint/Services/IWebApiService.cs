using BTLabelPrint.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BTLabelPrint.Services
{
    interface IWebApiService
    {
        [Get("/api?token={token}&method=getOrder&param={param}")]
        Task<Models.ApiResponse<OrderResponse>> GetOrders(string token, string param);
    }
}
