using BTLabelPrint.Models;
using BTLabelPrint.Services;
using Prism.Commands;
using Prism.Common;
using Prism.Mvvm;
using Seagull.BarTender.Print;
using Seagull.BarTender.Print.Database;
using Seagull.BarTender.PrintServer.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Serialization;
using Settings = BTLabelPrint.Properties.Settings;

namespace BTLabelPrint.ViewModels
{
    class MainWindowViewModel : BindableBase, IDisposable
    {
        private readonly IWebApiService webApiService;
        private readonly SearchSortService searchService;
        private Engine barEngine;
        private CancellationTokenSource searchCts;

        public MainWindowViewModel(IWebApiService webApiService, SearchSortService searchService)
        {
            this.webApiService = webApiService;
            this.searchService = searchService;

            CurrentPageCount = Settings.Default.CountPerPage <= 0 ? 100 : Settings.Default.CountPerPage;
            CurrentSearchField = SearchFields.FirstOrDefault(x => x.FieldName == Settings.Default.SearchFieldName) ?? SearchFields[0];
            LastPage = 1;
            CurrentPage = 1;

            CurrentPrinter = Printers.FirstOrDefault(x => x.PrinterName == Settings.Default.DefaultPrinter);
        }

        #region Properties
        private ICollection<SelectableWrapper<Order>> orders;
        public ICollection<SelectableWrapper<Order>> Orders
        {
            get => orders;
            set => SetProperty(ref orders, value);
        }

        private bool allSelected;
        public bool AllSelected
        {
            get => allSelected;
            set => SetProperty(ref allSelected, value,
                () =>
                {
                    SelectDeselectAll(allSelected);
                });
        }

        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            set => SetProperty(ref isLoading, value);
        }

        public Printers Printers => new Printers();

        private Printer currentPrinter;
        public Printer CurrentPrinter
        {
            get => currentPrinter ?? Printers.Default;
            set => SetProperty(ref currentPrinter, value, () => Settings.Default.DefaultPrinter = currentPrinter?.PrinterName);
        }

        public int[] PageCounts => new[] { 10, 20, 30, 50, 100, 250 };
        public SearchField<Order>[] SearchFields => new[]
        {
            new SearchField<Order>("BuyerName", "Покупатель", x => x.BuyerName),
            new SearchField<Order>("Number", "Номер заказа", x => x.Number),
            new SearchField<Order>("Phone", "Номер телефона", x => x.Phone)
        };

        private SearchField<Order> currentSearchField;
        public SearchField<Order> CurrentSearchField
        {
            get => currentSearchField;
            set => SetProperty(ref currentSearchField, value, () => Settings.Default.SearchFieldName = currentSearchField.FieldName);
        }

        private int currentPageCount;
        public int CurrentPageCount
        {
            get => currentPageCount;
            set => SetProperty(ref currentPageCount, value, () => Settings.Default.CountPerPage = currentPageCount);
        }

        private bool isPrinting;
        public bool IsPrinting
        {
            get => isPrinting;
            set => SetProperty(ref isPrinting, value);
        }

        private int lastPage;
        public int LastPage
        {
            get => lastPage;
            set => SetProperty(ref lastPage, value);
        }

        private int currentPage;
        public int CurrentPage
        {
            get => currentPage;
            set
            {
                if(value > LastPage)
                {
                    value = LastPage;
                }
                else if(value < 1)
                {
                    value = 1;
                }
                SetProperty(ref currentPage, value, LoadPage);
            }
        }

        private string searchString;
        public string SearchString
        {
            get => searchString;
            set => SetProperty(ref searchString, value);
        }

        private bool isSearching;
        public bool IsSearching
        {
            get => isSearching;
            set => SetProperty(ref isSearching, value);
        }

        #endregion

        #region Commands
        public ICommand PrintCommand => new DelegateCommand(
            async () =>
            {
                await PrintOrders(Orders
                    .Where(x => x.IsSelected)
                    .Select(x => x.Model));

                SelectDeselectAll(false);
            });

        public ICommand RefreshCommand => new DelegateCommand(
            LoadPage,
            () => !IsLoading && !IsSearching)
            .ObservesProperty(() => IsLoading)
            .ObservesProperty(() => IsSearching);

        public ICommand SinglePrintCommand => new DelegateCommand<Order>(
            async x =>
            {
                if (x != null)
                {
                    await PrintOrders(new Order[] { x });
                }
            });

        public ICommand ChangePageCommand => new DelegateCommand<int?>(
            x =>
            {
                if(x.HasValue)
                {
                    CurrentPage += x.Value;
                }
            });

        public ICommand SetLastOrFirstPageCommand => new DelegateCommand<bool?>(
            x =>
            {
                if(x.HasValue)
                {
                    CurrentPage = x.Value ? LastPage : 1;
                }
            });

        public ICommand SearchCommand => new DelegateCommand(
            async () =>
            {
                if(String.IsNullOrWhiteSpace(SearchString) || CurrentSearchField == null)
                {
                    return;
                }

                if(IsSearching)
                {
                    if (searchCts != null)
                    {
                        searchCts.Cancel();
                        searchCts = null;
                    }
                    return;
                }
                else
                {
                    searchCts = new CancellationTokenSource();
                }

                var newOrders = new List<SelectableWrapper<Order>>();
                IsSearching = true;
                try
                {
                    await searchService.FindOrders(x => newOrders.Add(new SelectableWrapper<Order>(x)),
                        x => CurrentSearchField.GetFieldAction(x).Contains(SearchString),
                        searchCts.Token);
                    if (searchCts != null)
                    {
                        newOrders.Sort((x, y) => (x.Model.Id ?? 0) > (y.Model.Id ?? 0) ? -1 : 1);
                        Orders = newOrders;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка поиска");
                }
                finally
                {
                    IsSearching = false;
                }
            },
            () => !IsLoading)
            .ObservesProperty(() => IsLoading);
        #endregion

        private async void LoadPage()
        {
            IsLoading = true;
            try
            {
                Orders = (await searchService.GetLastSortedOrders(CurrentPageCount, CancellationToken.None))
                    .Select(x => new SelectableWrapper<Order>(x))
                    .ToList();
                //var response = await GetOrders(CurrentPage, CurrentPageCount);
                //if (response == null)
                //{
                //    Orders = null;
                //    return;
                //}

                //LastPage = (int)Math.Ceiling((double)response.CountOrder / CurrentPageCount);

                //if (response.Orders == null && LastPage < CurrentPage)
                //{
                //    CurrentPage = LastPage;
                //}
                //else
                //{
                //    Orders = response
                //        .Orders?
                //        .Select(x => new SelectableWrapper<Order>(x))
                //        .ToList();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<OrderResponse> GetOrders(int page, int count)
        {
            try
            {
                var response = await webApiService.GetOrders(AppSettings.Token, new OrderRequestParam(CurrentPage, CurrentPageCount),
                    CancellationToken.None);
                if (response.Error != 0)
                {
                    MessageBox.Show($"Запрос списка заказов вернул ошибку Error = {response.Error}");
                    return null;
                }
                return response.Response;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
                return null;
            }
        }

        private async System.Threading.Tasks.Task PrintOrders(IEnumerable<Order> orders)
        {
            IsPrinting = true;

            try
            {
                var rows = orders.SelectMany(x => x.OrderContent)
                    .Where(x => x.Count.HasValue && x.Count.Value > 0);
                //    .Select(x => new OrderRow
                //    {
                //        Name = x.Name,
                //        Count = x.Count ?? 0
                //    }).ToArray();

                if (!rows.Any())
                {
                    MessageBox.Show("Нет данных для печати!");
                    return;
                }

                await System.Threading.Tasks.Task.Run(
                () =>
                {
                    if (barEngine == null)
                    {
                        barEngine = new Engine();
                        barEngine.Start();
                    }
                    var doc = barEngine.Documents.Open(AppSettings.LabelPath);
                    try
                    {
                        //BarTender 2016
                        var tmpFilePath = Path.GetTempFileName();
                        File.WriteAllLines(tmpFilePath, rows.Select(x => $"{x.Name}Demo{AppSettings.Delimiter}{x.Count}"), Encoding.UTF8);
                        if(!(doc.DatabaseConnections["DB"] is TextFile dbConn))
                        {
                            throw new Exception("В шаблоне не найдено подключение к базе данных!");
                        }
                        dbConn.FileName = tmpFilePath;
                        dbConn.FieldDelimiter = AppSettings.Delimiter;

                        //BarTender 2019
                        //var xs = new XmlSerializer(rows.GetType(), new XmlRootAttribute("DB"));
                        //using (StringWriter sw = new StringWriter())
                        //{
                        //    xs.Serialize(sw, rows);
                        //    ((XMLDatabase)doc.DatabaseConnections["XML"]).XMLData = sw.ToString();
                        //}

                        doc.PrintSetup.PrinterName = CurrentPrinter.PrinterName;
                        doc.Print();

                        File.Delete(tmpFilePath);
                    }
                    finally
                    {
                        doc.Close(SaveOptions.DoNotSaveChanges);
                    }
                });
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка");
            }
            finally
            {
                IsPrinting = false;
            }
        }

        private void SelectDeselectAll(bool select)
        {
            foreach(var order in Orders)
            {
                order.IsSelected = select;
            }
            AllSelected = select;
        }

        public void Dispose()
        {
            if(barEngine != null)
            {
                barEngine.Stop(SaveOptions.DoNotSaveChanges);
                barEngine.Dispose();
            }
        }
    }

    class SelectableWrapper<T> : BindableBase
    {
        private bool isSelected;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private T model;
        public T Model
        {
            get => model;
            set => SetProperty(ref model, value);
        }

        public SelectableWrapper(T model)
        {
            this.Model = model;
        }
    }

    class SearchField<T>
    {
        public SearchField(string fieldName, string alias, Func<T, string> getFieldAction)
        {
            this.FieldName = fieldName;
            this.Alias = alias;
            this.GetFieldAction = getFieldAction;
        }
        public string FieldName { get; set; }
        public Func<T, string> GetFieldAction { get; set; }
        public string Alias { get; set; }

        public override bool Equals(object obj)
        {
            if(obj == null || !(obj is SearchField<T> other))
                return false;

            return this.FieldName == other.FieldName;
        }

        public override int GetHashCode()
        {
            return 956599492 + EqualityComparer<string>.Default.GetHashCode(FieldName);
        }
    }
}
