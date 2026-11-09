using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ElectronicPaymentApp.ViewModels;
using ElectronicPaymentApp.Views;
using System;
using System.Net.Http;
using ElectronicPaymentApp.Services;

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
            var settings = ApiSettings.Load();
            var httpClient = new HttpClient
            {
                BaseAddress = settings.BaseAddress,
                Timeout = TimeSpan.FromSeconds(10),
            };
            httpClient.DefaultRequestHeaders.Add(ApiSettings.ApiKeyHeaderName, settings.ApiKey);

            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(new AccountApiClient(httpClient)),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}