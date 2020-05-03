using BTLabelPrint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BTLabelPrint.Services
{
    class SearchSortService
    {
        private readonly IWebApiService webApiService;
        private readonly string token;

        public SearchSortService(IWebApiService webApiService, string token)
        {
            this.webApiService = webApiService;
            this.token = token;
        }

        public Task FindOrders(Action<Order> findCallback, Func<Order, bool> predicate, CancellationToken cancellationToken) =>
            ProcessAllOrders(
                orders =>
                {
                    foreach(var order in orders.Where(predicate))
                    {
                        findCallback(order);
                    }
                },
                cancellationToken);

        public async Task<IEnumerable<Order>> GetLastSortedOrders(int count, CancellationToken cancellationToken)
        {
            List<Order> orders = new List<Order>();

            await ProcessAllOrders(
                x => orders.AddRange(x),
                cancellationToken);

            return orders.OrderByDescending(x => x.Id).Take(count);
        }

        private async Task ProcessAllOrders(Action<IEnumerable<Order>> processAction, CancellationToken cancellationToken)
        {
            const int pageSize = 50;

            try
            {
                var response = await GetOrdersAsync(1, 1, cancellationToken);

                if (response == null || response.Count <= 0)
                {
                    return;
                }
                int lastPage = (int)Math.Ceiling((double)response.CountOrder / pageSize);
                if (lastPage <= 0)
                {
                    return;
                }

                List<Task<OrderResponse>> tasks = new List<Task<OrderResponse>>();
                while (lastPage > 0 && !cancellationToken.IsCancellationRequested)
                {
                    tasks.Add(GetOrdersAsync(lastPage, pageSize, cancellationToken));

                    //response = await GetOrdersAsync(lastPage, pageSize, cancellationToken);
                    //processAction(response.Orders);

                    lastPage--;
                }

                foreach (var r in await Task.WhenAll(tasks))
                {
                    processAction(r.Orders);
                }
            }
            catch
            {
                return;
            }
        }

        private async Task<OrderResponse> GetOrdersAsync(int page, int count, CancellationToken cancellationToken)
        {
            var response = await webApiService.GetOrders(token, new OrderRequestParam(page, count), cancellationToken);
            if (response.Error != 0)
            {
                throw new Exception($"Сервер вернул ошибку Error = {response.Error}");
            }

            return response.Response;
        }
    }
}
