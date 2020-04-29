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

            containerRegistry.RegisterInstance<IWebApiService>(Refit.RestService.For<IWebApiService>(httpSettings.GetValue<string>("Url")));

            ViewModelLocationProvider.Register<MainWindow, MainWindowViewModel>();
        }
    }
}
