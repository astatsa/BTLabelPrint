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
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;

namespace BTLabelPrint.ViewModels
{
    class MainWindowViewModel : BindableBase
    {
        private readonly IWebApiService webApiService;
        public MainWindowViewModel(IWebApiService webApiService)
        {
            this.webApiService = webApiService;
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
                    foreach (var order in Orders)
                    {
                        order.IsSelected = allSelected;
                    }
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
            set => SetProperty(ref currentPrinter, value);
        }

        public int[] PageCounts => new[] { 10, 20, 30, 50, 100 };

        private int currentPageCount;
        public int CurrentPageCount
        {
            get => currentPageCount;
            set => SetProperty(ref currentPageCount, value);
        }

        private bool isPrinting;
        public bool IsPrinting
        {
            get => isPrinting;
            set => SetProperty(ref isPrinting, value);
        }
        #endregion

        #region Commands
        public ICommand PrintCommand => new DelegateCommand(
            async () =>
            {
                await PrintOrders(Orders
                    .Where(x => x.IsSelected)
                    .Select(x => x.Model));

                AllSelected = false;
            },
            () => !IsLoading)
            .ObservesProperty(() => IsLoading);

        public ICommand RefreshCommand => new DelegateCommand(
            async () =>
            {
                IsLoading = true;

                try
                {
                    var response = await webApiService.GetOrders(AppSettings.Token, new OrderRequestParam(1).ToString());

                    if (response.Error != 0)
                    {
                        return;
                    }

                    Orders = response
                        .Response
                        .Orders
                        .Select(x => new SelectableWrapper<Order>(x))
                        .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка");
                }
                finally
                {
                    IsLoading = false;
                }
            },
            () => !IsLoading)
            .ObservesProperty(() => IsLoading);

        public ICommand SinglePrintCommand => new DelegateCommand<Order>(
            x =>
            {
                
            });
        #endregion

        private async System.Threading.Tasks.Task PrintOrders(IEnumerable<Order> orders)
        {
            IsPrinting = true;

            try
            {
                await System.Threading.Tasks.Task.Run(
                () =>
                {
                    var rows = orders.SelectMany(x => x.OrderContent)
                        .Where(x => x.Count.HasValue && x.Count.Value > 0)
                        .Select(x => new OrderRow
                        {
                            Name = x.Name,
                            Count = x.Count ?? 0
                        })
                        .ToArray();

                    using (Engine engine = new Engine())
                    {
                        engine.Start();
                        var doc = engine.Documents.Open(AppSettings.LabelPath);
                        var xs = new XmlSerializer(rows.GetType(), new XmlRootAttribute("DB"));
                        using (StringWriter sw = new StringWriter())
                        {
                            xs.Serialize(sw, rows);
                            ((XMLDatabase)doc.DatabaseConnections["XML"]).XmlData = sw.ToString();
                        }
                        doc.PrintSetup.PrinterName = CurrentPrinter.PrinterName;
                        doc.Print();
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
