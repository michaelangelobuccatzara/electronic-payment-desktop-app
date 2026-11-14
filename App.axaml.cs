using System;
using System.Net.Http;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ElectronicPaymentApp.Services;
using ElectronicPaymentApp.ViewModels;
using ElectronicPaymentApp.Views;

namespace ElectronicPaymentApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var apiSettings = ApiSettings.Load();
            var httpClient = new HttpClient
            {
                BaseAddress = apiSettings.BaseAddress,
                Timeout = TimeSpan.FromSeconds(10),
            };
            httpClient.DefaultRequestHeaders.Add(ApiSettings.ApiKeyHeaderName, apiSettings.ApiKey);

            var billAcceptor = new BillAcceptorClient(BillAcceptorSettings.Load().PortName);
            desktop.Exit += (_, _) => billAcceptor.Dispose(); // powers the acceptor off and closes the port

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(
                    new AccountApiClient(httpClient),
                    new PaymentApiClient(httpClient),
                    billAcceptor),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}