using BTLabelPrint.Views;
using BTLabelPrint.Services;
using BTLabelPrint.ViewModels;
using Microsoft.Extensions.Configuration;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BTLabelPrint.Helpers;

namespace BTLabelPrint
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Local.json", true)
                .Build();
            var httpSettings = configuration.GetSection("WebApiSettings");
            var printSettings = configuration.GetSection("PrintSettings");

            AppSettings.Token = httpSettings.GetValue<string>("Token");
            if(String.IsNullOrWhiteSpace(AppSettings.Token))
            {
                MessageBox.Show("Укажите Token в файле настроек!");
                Shutdown();
            }
            AppSettings.LabelPath = printSettings.GetValue<string>("LabelPath");
            if (String.IsNullOrWhiteSpace(AppSettings.LabelPath))
            {
                MessageBox.Show("Укажите путь к шаблону бирки в файле настроек!");
                Shutdown();
            }

            AppSettings.Delimiter = printSettings.GetValue<string>("Delimiter");
            if(String.IsNullOrEmpty(AppSettings.Delimiter))
            {
                AppSettings.Delimiter = "#";
            }

            containerRegistry.RegisterInstance<IWebApiService>(
                Refit.RestService.For<IWebApiService>(
                    new System.Net.Http.HttpClient(new WebApiHttpClientHandler())
                    {
                        BaseAddress = new Uri(httpSettings.GetValue<string>("Url"))
                    }));

            containerRegistry.RegisterInstance<SearchSortService>(
                new SearchSortService(
                    Container.Resolve<IWebApiService>(),
                    AppSettings.Token));

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            BTLabelPrint.Properties.Settings.Default.Save();
            base.OnExit(e);
        }
    }
}
