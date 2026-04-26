using AmeCapture.Application.Interfaces;
using AmeCapture.Infrastructure.Database;
using AmeCapture.Infrastructure.Repositories;
using AmeCapture.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace AmeCapture.App
{
    public static class MauiProgram
    {
        private const string LogOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

        public static MauiApp CreateMauiApp()
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AmeCapture",
                "logs",
                "amecapture-.log");

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: LogOutputTemplate);

#if DEBUG
            loggerConfiguration = loggerConfiguration.WriteTo.Console(outputTemplate: LogOutputTemplate);
#endif

            Log.Logger = loggerConfiguration.CreateLogger();

            Log.Information("AmeCapture starting up");
            Log.Debug("Log file path: {LogPath}", logPath);
            Log.Debug("Base data path: {BasePath}", Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AmeCapture", "data"));

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Fatal(e.ExceptionObject as Exception, "Unhandled AppDomain exception");
                Log.CloseAndFlush();
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Log.Error(e.Exception, "Unobserved task exception");
                e.SetObserved();
            };

            MauiAppBuilder builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AmeCapture",
                "data");

            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AmeCapture",
                "amecapture.db");

            builder.Services.AddSingleton<IDbConnectionFactory>(_ =>
            {
                string dir = Path.GetDirectoryName(dbPath)!;
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                return new SqliteConnectionFactory(dbPath);
            });

            builder.Services.AddSingleton<IStorageService>(_ =>
                new StorageService(basePath));

            builder.Services.AddSingleton<IWorkspaceRepository, WorkspaceRepository>();
            builder.Services.AddSingleton<ITagRepository, TagRepository>();
            builder.Services.AddSingleton<ISettingsRepository, SettingsRepository>();

            builder.Services.AddSingleton<ICaptureService, CaptureService>();
            builder.Services.AddSingleton<IThumbnailService, ThumbnailService>();
            builder.Services.AddSingleton<IWindowEnumerationService, WindowEnumerationService>();
            builder.Services.AddSingleton<ICaptureOrchestrator, CaptureOrchestrator>();
            builder.Services.AddSingleton<IEditorService, SkiaSharpEditorService>();
            builder.Services.AddSingleton<IGlobalShortcutService, GlobalShortcutService>();
            builder.Services.AddSingleton<IClipboardService, ClipboardService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();
            builder.Services.AddSingleton<ITrayService, TrayService>();
            builder.Services.AddSingleton<CommunityToolkit.Mvvm.Messaging.IMessenger>(CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default);

            builder.Services.AddTransient<ViewModels.WorkspaceViewModel>();
            builder.Services.AddTransient<ViewModels.EditorViewModel>();
            builder.Services.AddTransient<Views.WorkspacePage>();
            builder.Services.AddTransient<Views.EditorPage>();

            builder.Logging.AddSerilog(dispose: true);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
