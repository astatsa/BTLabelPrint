using BTLabelPrint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
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

        public async Task<IEnumerable<Order>> GetLastSortedDescOrders(int count, CancellationToken cancellationToken)
        {
            List<Order> orders = new List<Order>();

            await ProcessAllOrders(
                x => orders.AddRange(x),
                cancellationToken);

            return orders.OrderByDescending(x => x.Id).Take(count);
        }

        /// <summary>
        /// Return last count orders, if count < 0 return all orders
        /// </summary>
        /// <param name="count"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Order>> GetLastSortedOrdersAsync(int count, CancellationToken cancellationToken)
        {
            int pageSize = Math.Min(50, count);

            int ordersCount = await GetOrdersCountAsync(cancellationToken);
            int lastPage = (int)Math.Ceiling((double)ordersCount / pageSize);
            if (lastPage <= 0)
            {
                return Enumerable.Empty<Order>();
            }

            List<Order> result = new List<Order>();
            while (!cancellationToken.IsCancellationRequested && count > 0)
            {
                var response = await GetOrdersAsync(lastPage, pageSize, cancellationToken);
                var orders = response.Orders;
                if(response.Orders.Count > count)
                {
                    orders.RemoveRange(count, orders.Count - count);
                }
                orders.Reverse();
                result.AddRange(orders);

                count -= orders.Count;
                lastPage--;
            }

            return result;
        }

        private async Task ProcessAllOrders(Action<IEnumerable<Order>> processAction, CancellationToken cancellationToken)
        {
            const int pageSize = 50;

            try
            {
                int ordersCount = await GetOrdersCountAsync(cancellationToken);
                int lastPage = (int)Math.Ceiling((double)ordersCount / pageSize);
                if (lastPage <= 0)
                {
                    return;
                }

                List<Task<OrderResponse>> tasks = new List<Task<OrderResponse>>();
                while (lastPage > 0 && !cancellationToken.IsCancellationRequested)
                {
                    tasks.Add(GetOrdersAsync(lastPage, pageSize, cancellationToken));
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

        private async Task<int> GetOrdersCountAsync(CancellationToken cancellationToken)
        {
            var response = await GetOrdersAsync(1, 1, cancellationToken);
            if (response == null || response.CountOrder <= 0)
            {
                return 0;
            }
            return response.CountOrder;
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
