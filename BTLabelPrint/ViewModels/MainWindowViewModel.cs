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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;
using Settings = BTLabelPrint.Properties.Settings;

namespace BTLabelPrint.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private readonly IWebApiService webApiService;
        public MainWindowViewModel(IWebApiService webApiService)
        {
            this.webApiService = webApiService;

            CurrentPageCount = Settings.Default.CountPerPage <= 0 ? 100 : Settings.Default.CountPerPage;
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

        public int[] PageCounts => new[] { 10, 20, 30, 50, 100, 200 };

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
        #endregion

        #region Commands
        public ICommand PrintCommand => new DelegateCommand(
            async () =>
            {
                await PrintOrders(Orders
                    .Where(x => x.IsSelected)
                    .Select(x => x.Model));

                SelectDeselectAll(false);
            },
            () => !IsLoading)
            .ObservesProperty(() => IsLoading);

        public ICommand RefreshCommand => new DelegateCommand(
            LoadPage,
            () => !IsLoading)
            .ObservesProperty(() => IsLoading);

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
        #endregion

        private async void LoadPage()
        {
            IsLoading = true;
            try
            {
                var response = await GetOrders(CurrentPage, CurrentPageCount);
                if (response == null)
                {
                    Orders = null;
                    return;
                }

                LastPage = (int)Math.Ceiling((double)response.CountOrder / CurrentPageCount);

                if (response.Orders == null && LastPage < CurrentPage)
                {
                    CurrentPage = LastPage;
                }
                else
                {
                    Orders = response
                        .Orders?
                        .Select(x => new SelectableWrapper<Order>(x))
                        .ToList();
                }
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
                var response = await webApiService.GetOrders(AppSettings.Token, new OrderRequestParam(CurrentPage, CurrentPageCount));
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
            if (!orders.Any()) return;

            IsPrinting = true;

            try
            {
                await System.Threading.Tasks.Task.Run(
                () =>
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

                    using (Engine engine = new Engine())
                    {
                        engine.Start();
                        var doc = engine.Documents.Open(AppSettings.LabelPath);
                        try
                        {
                            //BarTender 2016
                            var tmpFilePath = Path.GetTempFileName();
                            File.WriteAllLines(tmpFilePath, rows.Select(x => $"{x.Name}{AppSettings.Delimiter}{x.Count}"), Encoding.UTF8);
                            if(!(doc.DatabaseConnections["DB"] is TextFile dbConn))
                            {
                                MessageBox.Show("В шаблоне не найдено подключение к базе данных!");
                                return;
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
                        }
                        finally
                        {
                            doc.Close(SaveOptions.DoNotSaveChanges);
                        }
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
}
